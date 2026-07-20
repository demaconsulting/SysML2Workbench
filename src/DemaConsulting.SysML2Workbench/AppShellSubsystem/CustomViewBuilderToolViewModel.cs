using CommunityToolkit.Mvvm.ComponentModel;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Dock tool view model backing the "Custom View Builder" panel. Holds the long-lived
///     <see cref="ViewDefinitionModel" /> builder state (mutated directly by
///     <see cref="CustomViewBuilderToolView" />'s per-row controls) and the available expose-target picker list
///     derived from the currently loaded workspace. This view model deliberately tracks no diagram tab itself:
///     100% of the "which tab, create-or-reuse, repaint, focus" decision is made centrally by
///     <see cref="MainWindowShell" /> and its <see cref="MainWindowShell.TabsChanged" /> notification, which the
///     Avalonia-aware composition root reconciles against Dock.
/// </summary>
public partial class CustomViewBuilderToolViewModel : Dock.Model.Mvvm.Controls.Tool
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
    ///     Creates the custom-view builder tool view model.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public CustomViewBuilderToolViewModel(MainWindowShell shell)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        Shell.SourcesChanged += (_, _) => RefreshFromWorkspace();
        RefreshFromWorkspace();
    }

    /// <summary>
    ///     Shared application shell.
    /// </summary>
    public MainWindowShell Shell { get; }

    /// <summary>
    ///     Long-lived custom-view builder state, mutated directly by the expose-target row controls' event
    ///     handlers in <see cref="CustomViewBuilderToolView" /> rather than rebuilt fresh from control values on
    ///     every Preview/Export click. This is the natural fit for the per-row mutable expose-target UI: each row
    ///     needs individually addressable recursion-kind and bracket-filter state that a flat "read all controls"
    ///     rebuild cannot reconstruct.
    /// </summary>
    public ViewDefinitionModel BuilderDefinition { get; private set; } = new();

    /// <summary>
    ///     Raised after <see cref="RefreshFromWorkspace" /> replaces <see cref="BuilderDefinition" /> with a fresh
    ///     instance, so the view can clear its manually constructed selected-target rows.
    /// </summary>
    public event EventHandler? BuilderReset;

    /// <summary>
    ///     Refreshes the available expose-target picker list from current shell state after a workspace open or
    ///     reload, and resets the builder state, since a fresh workspace invalidates any previously selected
    ///     expose targets that may no longer resolve.
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

        BuilderDefinition = new ViewDefinitionModel();
        StatusMessage = null;
        IsWorkspaceEmpty = Shell.CurrentWorkspace.Sources.Count == 0;
        BuilderReset?.Invoke(this, EventArgs.Empty);
    }
}
