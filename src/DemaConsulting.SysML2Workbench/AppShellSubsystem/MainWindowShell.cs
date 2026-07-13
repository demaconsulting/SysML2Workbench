using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Distinguishes the kind of content a <see cref="WorkbenchTab" /> presents.
/// </summary>
public enum WorkbenchTabKind
{
    /// <summary>A rendered predefined view selected from the catalog.</summary>
    PredefinedView,

    /// <summary>A live preview of a GUI-authored custom view.</summary>
    CustomViewPreview,
}

/// <summary>
///     One open tab in the shell's tabbed presentation area.
/// </summary>
/// <param name="Id">Stable identifier used to detect an already-open tab.</param>
/// <param name="Title">User-facing tab label.</param>
/// <param name="Kind">Content category shown by the tab.</param>
public sealed record WorkbenchTab(string Id, string Title, WorkbenchTabKind Kind);

/// <summary>
///     MainWindowShell is the desktop composition root that coordinates workspace lifecycle, view selection,
///     diagram display, diagnostics presentation, and snippet export within a single windowed user experience.
/// </summary>
/// <remarks>
///     This class intentionally has no direct dependency on any Avalonia control type, so its orchestration
///     logic is fully unit-testable without booting the Avalonia UI thread. The real window
///     (<c>MainWindowView.axaml</c>/<c>.axaml.cs</c>) is a thin binding layer over an instance of this class.
/// </remarks>
public sealed class MainWindowShell : IDisposable
{
    private readonly WorkspaceModel _workspaceModel;
    private readonly FileWatcher _fileWatcher;
    private readonly DiagnosticsAggregator _diagnosticsAggregator;
    private readonly ViewCatalogPresenter _viewCatalogPresenter;
    private readonly LayoutInvoker _layoutInvoker;
    private readonly SvgCanvasHost _canvasHost;
    private readonly DiagnosticsListView _diagnosticsListView;
    private readonly SysmlSnippetGenerator _snippetGenerator;
    private readonly RollingFileLogger _logger;
    private readonly List<WorkbenchTab> _openTabs = [];

    /// <summary>
    ///     Currently loaded workspace and its revision metadata, or <see langword="null" /> before the first
    ///     workspace is opened.
    /// </summary>
    public WorkspaceSnapshot? CurrentWorkspace { get; private set; }

    /// <summary>
    ///     Selected catalog view, if the user is in predefined-view mode.
    /// </summary>
    public ViewDescriptor? ActivePredefinedView { get; private set; }

    /// <summary>
    ///     Current custom-view state, if the user is composing or previewing a custom view.
    /// </summary>
    public ViewDefinitionModel? ActiveCustomView { get; private set; }

    /// <summary>
    ///     Tabs representing rendered diagrams, builder surfaces, or related shell content.
    /// </summary>
    public IReadOnlyList<WorkbenchTab> OpenTabs => _openTabs;

    /// <summary>
    ///     Read-only access to the view catalog presenter, exposed so the UI can list predefined views.
    /// </summary>
    public ViewCatalogPresenter ViewCatalog => _viewCatalogPresenter;

    /// <summary>
    ///     Read-only access to the diagnostics list view, exposed so the UI can bind the diagnostics panel.
    /// </summary>
    public DiagnosticsListView Diagnostics => _diagnosticsListView;

    /// <summary>
    ///     Read-only access to the diagram canvas host, exposed so the UI can bind the diagram surface.
    /// </summary>
    public SvgCanvasHost Canvas => _canvasHost;

    /// <summary>
    ///     Creates the shell from its constituent subsystem units.
    /// </summary>
    /// <param name="workspaceModel">Owns discovery, load, and reload of the workspace.</param>
    /// <param name="fileWatcher">Detects external workspace changes.</param>
    /// <param name="diagnosticsAggregator">Aggregates per-file diagnostics into a workspace-wide view.</param>
    /// <param name="viewCatalogPresenter">Supplies predefined view choices.</param>
    /// <param name="layoutInvoker">Renders predefined and custom views to SVG.</param>
    /// <param name="canvasHost">Displays the active diagram.</param>
    /// <param name="diagnosticsListView">Displays workspace diagnostics.</param>
    /// <param name="snippetGenerator">Exports custom-view definitions as SysML text.</param>
    /// <param name="logger">Records shell-level operational events and failures.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public MainWindowShell(
        WorkspaceModel workspaceModel,
        FileWatcher fileWatcher,
        DiagnosticsAggregator diagnosticsAggregator,
        ViewCatalogPresenter viewCatalogPresenter,
        LayoutInvoker layoutInvoker,
        SvgCanvasHost canvasHost,
        DiagnosticsListView diagnosticsListView,
        SysmlSnippetGenerator snippetGenerator,
        RollingFileLogger logger)
    {
        ArgumentNullException.ThrowIfNull(workspaceModel);
        ArgumentNullException.ThrowIfNull(fileWatcher);
        ArgumentNullException.ThrowIfNull(diagnosticsAggregator);
        ArgumentNullException.ThrowIfNull(viewCatalogPresenter);
        ArgumentNullException.ThrowIfNull(layoutInvoker);
        ArgumentNullException.ThrowIfNull(canvasHost);
        ArgumentNullException.ThrowIfNull(diagnosticsListView);
        ArgumentNullException.ThrowIfNull(snippetGenerator);
        ArgumentNullException.ThrowIfNull(logger);

        _workspaceModel = workspaceModel;
        _fileWatcher = fileWatcher;
        _diagnosticsAggregator = diagnosticsAggregator;
        _viewCatalogPresenter = viewCatalogPresenter;
        _layoutInvoker = layoutInvoker;
        _canvasHost = canvasHost;
        _diagnosticsListView = diagnosticsListView;
        _snippetGenerator = snippetGenerator;
        _logger = logger;
    }

    /// <summary>
    ///     Loads a new workspace into the shell, refreshes the view catalog and diagnostics, resets active
    ///     selections, and begins live-watching the folder for external changes.
    /// </summary>
    /// <param name="rootPath">User-selected folder.</param>
    /// <returns>The freshly loaded workspace snapshot.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rootPath" /> is null or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="rootPath" /> does not exist.</exception>
    public async Task<WorkspaceSnapshot> OpenWorkspaceAsync(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        try
        {
            var snapshot = await _workspaceModel.LoadWorkspaceAsync(rootPath).ConfigureAwait(false);
            ApplyWorkspaceSnapshot(snapshot);

            if (_fileWatcher.WatchedRootPath is null)
            {
                _fileWatcher.StartWatching(snapshot.RootPath);
            }

            _logger.Log(LogLevel.Info, $"Workspace opened: {snapshot.RootPath}");
            return snapshot;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.Log(LogLevel.Error, $"Failed to open workspace '{rootPath}'", ex);
            throw;
        }
    }

    /// <summary>
    ///     Re-applies pending external file changes by reloading the workspace and refreshing all
    ///     workspace-derived shell state.
    /// </summary>
    /// <returns>The refreshed workspace snapshot.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no workspace has been opened yet.</exception>
    public async Task<WorkspaceSnapshot> RefreshFromExternalChangesAsync()
    {
        if (CurrentWorkspace is null)
        {
            throw new InvalidOperationException("A workspace must be opened before it can be refreshed.");
        }

        var changedPaths = _fileWatcher.FlushPendingChanges();

        try
        {
            var snapshot = await _workspaceModel.ReloadFilesAsync(changedPaths).ConfigureAwait(false);
            ApplyWorkspaceSnapshot(snapshot);
            _logger.Log(LogLevel.Info, $"Workspace refreshed after external change ({changedPaths.Count} file(s)).");
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Failed to refresh workspace after an external change.", ex);
            throw;
        }
    }

    /// <summary>
    ///     Renders a predefined view selected by the user.
    /// </summary>
    /// <param name="viewId">Identifier from <see cref="ViewCatalogPresenter" />.</param>
    /// <returns>Rendered SVG markup now loaded into the canvas.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no workspace is loaded.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewId" /> is not present in the catalog.</exception>
    public string SelectPredefinedView(string viewId)
    {
        if (CurrentWorkspace is null)
        {
            throw new InvalidOperationException("A workspace must be opened before a view can be selected.");
        }

        var descriptor = _viewCatalogPresenter.SelectView(viewId);

        try
        {
            var svg = _layoutInvoker.RenderPredefinedView(CurrentWorkspace.Workspace, descriptor);
            _canvasHost.LoadSvg(svg);
            ActivePredefinedView = descriptor;
            ActiveCustomView = null;
            EnsureTabOpen(descriptor.QualifiedName, descriptor.DisplayName, WorkbenchTabKind.PredefinedView);
            return svg;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"Failed to render predefined view '{viewId}'.", ex);
            throw;
        }
    }

    /// <summary>
    ///     Renders the current GUI-authored custom view as a live preview.
    /// </summary>
    /// <param name="definition">Normalized custom-view state.</param>
    /// <returns>Rendered SVG markup now loaded into the canvas.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition" /> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no workspace is loaded, or when <paramref name="definition" /> does not validate against
    ///     the current workspace.
    /// </exception>
    public string PreviewCustomView(ViewDefinitionModel definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (CurrentWorkspace is null)
        {
            throw new InvalidOperationException("A workspace must be opened before a custom view can be previewed.");
        }

        var validation = definition.ValidateAgainstWorkspace(CurrentWorkspace.Workspace);
        if (validation.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException(
                "The custom view definition does not validate against the current workspace: "
                + string.Join("; ", validation.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message)));
        }

        try
        {
            var svg = _layoutInvoker.RenderCustomView(CurrentWorkspace.Workspace, definition);
            _canvasHost.LoadSvg(svg);
            ActiveCustomView = definition;
            ActivePredefinedView = null;
            EnsureTabOpen("$custom-preview", definition.DisplayName ?? "Custom View", WorkbenchTabKind.CustomViewPreview);
            return svg;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Failed to render custom view preview.", ex);
            throw;
        }
    }

    /// <summary>
    ///     Generates copy-pasteable SysML text for the current custom-view definition.
    /// </summary>
    /// <param name="definition">Normalized custom-view state, ready to export.</param>
    /// <returns>Complete SysML view snippet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition" /> is null.</exception>
    public string ExportCustomViewSnippet(ViewDefinitionModel definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var snippet = _snippetGenerator.GenerateSnippet(definition);
        _logger.Log(LogLevel.Info, "Custom view exported as a SysML snippet.");
        return snippet;
    }

    /// <summary>
    ///     Applies a freshly loaded or reloaded workspace snapshot to all workspace-derived shell state.
    /// </summary>
    /// <param name="snapshot">Newly published workspace snapshot.</param>
    private void ApplyWorkspaceSnapshot(WorkspaceSnapshot snapshot)
    {
        CurrentWorkspace = snapshot;
        _diagnosticsAggregator.ReplaceWorkspaceDiagnostics(snapshot.Diagnostics);
        var ordered = _diagnosticsAggregator.RebuildAggregate();
        _diagnosticsListView.BindDiagnostics(ordered);
        _viewCatalogPresenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);

        ActivePredefinedView = null;
        ActiveCustomView = null;
        _openTabs.Clear();
    }

    /// <summary>
    ///     Opens a tab for the given identifier if one is not already open.
    /// </summary>
    /// <param name="id">Stable tab identifier.</param>
    /// <param name="title">User-facing tab label.</param>
    /// <param name="kind">Content category shown by the tab.</param>
    private void EnsureTabOpen(string id, string title, WorkbenchTabKind kind)
    {
        if (_openTabs.Any(tab => tab.Id == id))
        {
            return;
        }

        _openTabs.Add(new WorkbenchTab(id, title, kind));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _fileWatcher.Dispose();
    }
}
