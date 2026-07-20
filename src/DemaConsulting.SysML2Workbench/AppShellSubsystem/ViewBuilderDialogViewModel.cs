using CommunityToolkit.Mvvm.ComponentModel;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     View model backing the modal <see cref="ViewBuilderDialogView" />. Constructed fresh every time the
///     dialog opens (unlike the tool panel view models it supersedes), so it holds no state carried over from a
///     prior dialog session. Wraps a single <see cref="ViewDefinitionModel" />, mutated the same way the
///     deleted <c>CustomViewBuilderToolViewModel.BuilderDefinition</c> was, and owns its own
///     <see cref="PreviewCanvas" /> so every in-progress edit can be rendered live into the dialog's left-hand
///     preview pane without touching any real, shell-tracked diagram tab (see <see cref="RenderPreview" />).
///     Only <see cref="TryCommit" /> - the dialog's OK button - ever creates a real tab on <see cref="Shell" />;
///     Cancel performs zero calls into <see cref="Shell" /> at all.
/// </summary>
public sealed partial class ViewBuilderDialogViewModel : ObservableObject
{
    /// <summary>
    ///     Node kinds excluded from the expose-target picker, mirroring <see cref="ViewDefinitionModel" />'s own
    ///     validation rules so the user cannot select a target that would fail validation.
    /// </summary>
    private static readonly Type[] DisallowedExposeNodeTypes =
    [
        typeof(SysmlViewNode),
        typeof(SysmlViewpointNode),
        typeof(SysmlImportNode),
        typeof(SysmlMetadataNode),
        typeof(SysmlTransitionNode),
        typeof(SysmlConnectionNode),
    ];

    [ObservableProperty]
    private IReadOnlyList<string> _availableExposeTargets = [];

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isWorkspaceEmpty;

    /// <summary>
    ///     Creates the dialog view model, refreshing the available expose-target picker list from the shell's
    ///     current workspace and rendering an initial (empty) preview.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public ViewBuilderDialogViewModel(MainWindowShell shell)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        PreviewCanvas = new SvgCanvasHost();

        RefreshFromWorkspace();
    }

    /// <summary>
    ///     Shared application shell.
    /// </summary>
    public MainWindowShell Shell { get; }

    /// <summary>
    ///     The custom-view definition being composed by this dialog session, mutated directly by the expose-target
    ///     row controls' event handlers in <see cref="ViewBuilderDialogView" /> rather than rebuilt fresh from
    ///     control values on every edit, matching the deleted docked panel's approach to the same per-row mutable
    ///     expose-target state.
    /// </summary>
    public ViewDefinitionModel Definition { get; } = new();

    /// <summary>
    ///     The dialog's own diagram surface, holding whatever SVG <see cref="RenderPreview" /> most recently
    ///     produced. Never shared with, nor derived from, any tab tracked by <see cref="Shell" />.
    /// </summary>
    public SvgCanvasHost PreviewCanvas { get; }

    /// <summary>
    ///     Raised after <see cref="RenderPreview" /> updates <see cref="PreviewCanvas" />, so the view can reload
    ///     its on-screen preview <c>Image</c> from the new SVG.
    /// </summary>
    public event EventHandler? PreviewChanged;

    /// <summary>
    ///     Refreshes the available expose-target picker list from current shell state. Called once at
    ///     construction; the dialog is short-lived (opened, edited, closed) so it does not itself subscribe to
    ///     <see cref="MainWindowShell.SourcesChanged" /> the way the old long-lived panel view model did.
    /// </summary>
    public void RefreshFromWorkspace()
    {
        AvailableExposeTargets = Shell.CurrentWorkspace.Sources.Count == 0
            ? []
            : Shell.CurrentWorkspace.Workspace.Declarations
                .Where(kvp => !Shell.CurrentWorkspace.Workspace.StdlibNames.Contains(kvp.Key))
                .Where(kvp => !DisallowedExposeNodeTypes.Contains(kvp.Value.GetType()))
                .Select(kvp => kvp.Key)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

        IsWorkspaceEmpty = Shell.CurrentWorkspace.Sources.Count == 0;
    }

    /// <summary>
    ///     Adds an expose target to <see cref="Definition" /> and re-renders the live preview.
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the target to expose.</param>
    public void AddExposeTarget(string qualifiedName)
    {
        Definition.AddExposeTarget(qualifiedName);
        RenderPreview();
    }

    /// <summary>
    ///     Removes an expose target from <see cref="Definition" /> and re-renders the live preview.
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the previously-added target.</param>
    /// <param name="recursionKind">Recursion kind of the previously-added target.</param>
    public void RemoveExposeTarget(string qualifiedName, ExposeRecursionKind recursionKind)
    {
        Definition.RemoveExposeTarget(qualifiedName, recursionKind);
        RenderPreview();
    }

    /// <summary>
    ///     Changes an expose target's recursion kind on <see cref="Definition" /> and re-renders the live preview.
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the previously-added target.</param>
    /// <param name="currentRecursionKind">Current recursion kind of the target to change.</param>
    /// <param name="newRecursionKind">New recursion kind for the target.</param>
    public void SetExposeRecursionKind(string qualifiedName, ExposeRecursionKind currentRecursionKind, ExposeRecursionKind newRecursionKind)
    {
        Definition.SetExposeRecursionKind(qualifiedName, currentRecursionKind, newRecursionKind);
        RenderPreview();
    }

    /// <summary>
    ///     Sets or clears an expose target's optional bracket-filter expression on <see cref="Definition" /> and
    ///     re-renders the live preview.
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the previously-added target.</param>
    /// <param name="recursionKind">Recursion kind of the previously-added target.</param>
    /// <param name="expression">Bracket-filter expression text, or <see langword="null" />/whitespace to clear it.</param>
    public void SetExposeBracketFilter(string qualifiedName, ExposeRecursionKind recursionKind, string? expression)
    {
        Definition.SetExposeBracketFilter(qualifiedName, recursionKind, expression);
        RenderPreview();
    }

    /// <summary>
    ///     Changes the target rendering style on <see cref="Definition" /> and re-renders the live preview.
    /// </summary>
    /// <param name="viewKind">Selected view kind.</param>
    public void SetViewKind(ViewKind viewKind)
    {
        Definition.SetViewKind(viewKind);
        RenderPreview();
    }

    /// <summary>
    ///     Sets the optional filter expression on <see cref="Definition" /> and re-renders the live preview.
    /// </summary>
    /// <param name="filterExpression">Filter text, or <see langword="null" />/whitespace to clear it.</param>
    public void SetFilterExpression(string? filterExpression)
    {
        Definition.SetFilterExpression(filterExpression);
        RenderPreview();
    }

    /// <summary>
    ///     Sets the optional user-facing view name on <see cref="Definition" />. Does not itself affect the
    ///     rendered SVG shape, but is still followed by a preview refresh so the dialog behaves consistently: any
    ///     edit re-renders (the name change does update the tab title a subsequent <see cref="TryCommit" /> would
    ///     produce).
    /// </summary>
    /// <param name="displayName">Display name, or <see langword="null" />/whitespace to clear it.</param>
    public void SetDisplayName(string? displayName)
    {
        Definition.SetDisplayName(displayName);
        RenderPreview();
    }

    /// <summary>
    ///     Renders <see cref="Definition" /> via <see cref="MainWindowShell.RenderCustomViewPreview" /> and loads
    ///     the result into <see cref="PreviewCanvas" />, then raises <see cref="PreviewChanged" />. Never throws:
    ///     an incomplete or invalid definition (for example no view kind selected yet) is reported through
    ///     <see cref="StatusMessage" /> instead, since this method runs after every single control edit and a
    ///     mid-edit definition is routinely incomplete. On failure, <see cref="PreviewCanvas" /> is cleared via
    ///     <see cref="SvgCanvasHost.Clear" /> so a previously-rendered SVG never lingers on screen once the
    ///     current configuration no longer corresponds to it.
    /// </summary>
    public void RenderPreview()
    {
        try
        {
            var svg = Shell.RenderCustomViewPreview(Definition);
            PreviewCanvas.LoadSvg(svg, resetViewport: false);
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            PreviewCanvas.Clear();
            StatusMessage = $"Preview unavailable: {ex.Message}";
        }
        finally
        {
            PreviewChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Commits the current <see cref="Definition" /> as a brand-new diagram tab: opens a fresh, empty
    ///     custom-view-preview tab via <see cref="MainWindowShell.OpenNewCustomPreviewTab" /> and renders
    ///     <see cref="Definition" /> into it via <see cref="MainWindowShell.PreviewCustomView" />, in one
    ///     try/catch (per this unit's documented "open, try-render, close-tab-and-report-error on failure"
    ///     sequence) so the two methods' validation paths can never disagree: if the render fails, the just-opened
    ///     empty tab is rolled back via <see cref="MainWindowShell.CloseDiagramTab" /> so no partial/empty tab is
    ///     ever left behind.
    /// </summary>
    /// <param name="error">
    ///     When this method returns <see langword="false" />, the reason the commit failed; otherwise
    ///     <see langword="null" />.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> when a new tab was opened and successfully rendered; <see langword="false" />
    ///     when the definition failed to render, in which case no tab is left open and <paramref name="error" />
    ///     describes the failure.
    /// </returns>
    public bool TryCommit(out string? error)
    {
        var tab = Shell.OpenNewCustomPreviewTab();

        try
        {
            Shell.PreviewCustomView(Definition);
            error = null;
            StatusMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            Shell.CloseDiagramTab(tab.Id);
            error = $"Commit failed: {ex.Message}";
            StatusMessage = error;
            return false;
        }
    }
}
