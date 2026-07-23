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

    /// <summary>A read-only view of a workspace file's raw source text.</summary>
    SourceText,
}

/// <summary>
///     One open tab in the shell's tabbed presentation area. Each tab owns its own <see cref="SvgCanvasHost" />
///     so multiple diagram tabs can be open at once with fully independent zoom/pan/content state.
/// </summary>
/// <param name="Id">Stable identifier used to detect an already-open tab.</param>
/// <param name="Title">User-facing tab label.</param>
/// <param name="Kind">Content category shown by the tab.</param>
/// <param name="Canvas">
///     This tab's own diagram surface state. Allocated for every tab kind for shape simplicity, but unused
///     (never loaded/rendered into) for a <see cref="WorkbenchTabKind.SourceText" /> tab.
/// </param>
/// <param name="SourceDefinition">
///     The <see cref="ViewDefinitionModel" /> that produced this tab's currently rendered diagram, or
///     <see langword="null" /> when no concrete definition could be derived for it (an unscoped predefined view
///     with zero expose members, or a brand-new custom-preview tab that has not rendered anything yet). Used by
///     <see cref="MainWindowShell.CanExportTabAsSysml" />/<see cref="MainWindowShell.ExportTabAsSysmlSnippet" /> to
///     back the diagram tab's "Copy as SysML" context-menu action. Always <see langword="null" /> for a
///     <see cref="WorkbenchTabKind.SourceText" /> tab.
/// </param>
/// <param name="FilePath">
///     The absolute path of the workspace file this tab presents the raw source text of, or
///     <see langword="null" /> for every tab kind other than <see cref="WorkbenchTabKind.SourceText" />. Resolved
///     by <see cref="MainWindowShell.GetTabFilePath" /> for <see cref="SourceTextDocumentViewModel" />.
/// </param>
/// <remarks>
///     Because <see cref="Canvas" /> is a mutable reference type, two <see cref="WorkbenchTab" /> instances are
///     no longer meaningfully value-equal to each other once its state diverges. Nothing in
///     <see cref="MainWindowShell" /> compares <see cref="WorkbenchTab" /> instances by equality; all lookups are
///     by <see cref="Id" />.
/// </remarks>
public sealed record WorkbenchTab(string Id, string Title, WorkbenchTabKind Kind, SvgCanvasHost Canvas, ViewDefinitionModel? SourceDefinition = null, string? FilePath = null);

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
    private readonly DiagnosticsListView _diagnosticsListView;
    private readonly SysmlSnippetGenerator _snippetGenerator;
    private readonly RollingFileLogger _logger;
    private readonly List<WorkbenchTab> _openTabs = [];

    /// <summary>
    ///     Owns the ordered set of file/folder sources the user has added to the workspace. The shell is the
    ///     single owner of this instance; other consumers (for example <see cref="WorkspacePanelToolViewModel" />)
    ///     read source/attribution state only through the shell's own read-only surface.
    /// </summary>
    private readonly WorkspaceSourceSet _sourceSet = new();

    /// <summary>
    ///     Identifiers of the sources currently registered with <see cref="_fileWatcher" />, tracked so mutators
    ///     can diff the previous and new source sets and call <see cref="FileWatcher.WatchSource" />/
    ///     <see cref="FileWatcher.UnwatchSource" /> only for the sources that actually changed.
    /// </summary>
    private readonly HashSet<string> _watchedSourceIds = [];

    /// <summary>
    ///     Canvas shown when no diagram tab is open, so <see cref="Canvas" /> always has a usable instance to
    ///     return even before the first tab exists (or after the last tab closes).
    /// </summary>
    private readonly SvgCanvasHost _idleCanvasHost = new();

    /// <summary>
    ///     Monotonically increasing counter used to generate unique custom-view-preview tab identifiers across
    ///     the shell's lifetime. Deliberately not reset when <see cref="ApplyWorkspaceSnapshot" /> clears
    ///     <see cref="_openTabs" />, so ids remain collision-free for the shell instance's entire lifetime.
    /// </summary>
    private int _customPreviewTabSequence;

    /// <summary>
    ///     Currently loaded workspace and its revision metadata. Never <see langword="null" />: the shell
    ///     eagerly computes and applies an empty (0-source) snapshot at construction, so a freshly constructed
    ///     shell always has a valid, if empty, workspace rather than a null placeholder.
    /// </summary>
    public WorkspaceSnapshot CurrentWorkspace { get; private set; }

    /// <summary>
    ///     Maps each currently loaded source's id to the files it contributed, mirroring the most recent
    ///     <see cref="WorkspaceSourceResolution.SourceIdToFiles" />. Exposed read-only so
    ///     <see cref="WorkspacePanelToolViewModel" /> can build its source/file tree without needing its own
    ///     <see cref="WorkspaceSourceSet" /> instance (the shell is the single owner of source state).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> CurrentSourceIdToFiles { get; private set; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

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
    ///     Identifier of the diagram tab currently active/focused, or <see langword="null" /> when no diagram
    ///     tab is open. Updated either by shell operations that open or focus a tab (<see cref="SelectPredefinedView" />,
    ///     <see cref="PreviewCustomView" />, <see cref="OpenNewCustomPreviewTab" />, <see cref="CloseDiagramTab" />)
    ///     or by the UI layer forwarding Dock's own focus-change signal via <see cref="NotifyActiveDiagramTab" />.
    /// </summary>
    public string? ActiveTabId { get; private set; }

    /// <summary>
    ///     The currently active tab, or <see langword="null" /> when <see cref="ActiveTabId" /> is
    ///     <see langword="null" /> or no longer refers to an open tab.
    /// </summary>
    public WorkbenchTab? ActiveTab => _openTabs.FirstOrDefault(tab => tab.Id == ActiveTabId);

    /// <summary>
    ///     Read-only access to the view catalog presenter, exposed so the UI can list predefined views.
    /// </summary>
    public ViewCatalogPresenter ViewCatalog => _viewCatalogPresenter;

    /// <summary>
    ///     Read-only access to the diagnostics list view, exposed so the UI can bind the diagnostics panel.
    /// </summary>
    public DiagnosticsListView Diagnostics => _diagnosticsListView;

    /// <summary>
    ///     Read-only access to the active diagram tab's canvas host, or an idle canvas host with no content
    ///     loaded when no diagram tab is open.
    /// </summary>
    public SvgCanvasHost Canvas => ActiveTab?.Canvas ?? _idleCanvasHost;

    /// <summary>
    ///     Raised whenever the set of open tabs, or which tab is active, changes: a tab is opened, closed,
    ///     re-rendered in place, or the workspace is reloaded (which clears every tab). The Avalonia-aware UI
    ///     layer subscribes to this to reconcile Dock's <c>DocumentDock</c> with <see cref="OpenTabs" />.
    /// </summary>
    /// <remarks>
    ///     Raised via the constructor-injected <see cref="IUiDispatcher" /> (see <see cref="RaiseTabsChanged" />),
    ///     so subscribers are guaranteed to observe it on the dispatcher's target thread even though the shell
    ///     methods that trigger it - most notably <see cref="AddFolderSourceAsync" />, which awaits workspace
    ///     loading with <c>ConfigureAwait(false)</c> - may themselves resume on a background thread pool thread.
    /// </remarks>
    public event EventHandler? TabsChanged;

    /// <summary>
    ///     Raised whenever the set of workspace sources changes: a file or folder source is added or removed.
    ///     Raised after the resulting workspace snapshot has already been applied (<see cref="CurrentWorkspace" />
    ///     and <see cref="CurrentSourceIdToFiles" /> reflect the change), so subscribers such as
    ///     <see cref="WorkspacePanelToolViewModel" /> can rebuild their source tree directly from shell state.
    /// </summary>
    /// <remarks>
    ///     Raised via the same injected <see cref="IUiDispatcher" /> as <see cref="TabsChanged" />, for the same
    ///     reason: source mutators await workspace loading with <c>ConfigureAwait(false)</c> and may resume on a
    ///     background thread.
    /// </remarks>
    public event EventHandler? SourcesChanged;

    /// <summary>
    ///     Dispatcher used to marshal <see cref="TabsChanged" /> notifications onto whatever thread the shell's
    ///     UI-facing consumers require.
    /// </summary>
    private readonly IUiDispatcher _uiDispatcher;

    /// <summary>
    ///     Creates the shell from its constituent subsystem units.
    /// </summary>
    /// <param name="workspaceModel">Owns discovery, load, and reload of the workspace.</param>
    /// <param name="fileWatcher">Detects external workspace changes.</param>
    /// <param name="diagnosticsAggregator">Aggregates per-file diagnostics into a workspace-wide view.</param>
    /// <param name="viewCatalogPresenter">Supplies predefined view choices.</param>
    /// <param name="layoutInvoker">Renders predefined and custom views to SVG.</param>
    /// <param name="diagnosticsListView">Displays workspace diagnostics.</param>
    /// <param name="snippetGenerator">Exports custom-view definitions as SysML text.</param>
    /// <param name="logger">Records shell-level operational events and failures.</param>
    /// <param name="uiDispatcher">
    ///     Dispatcher used to marshal <see cref="TabsChanged" /> notifications. Defaults to
    ///     <see cref="ImmediateUiDispatcher" />, which runs the notification synchronously on the calling thread;
    ///     production wiring should pass a real UI-thread-aware implementation (for example
    ///     <c>AvaloniaUiDispatcher</c>) so notifications reach Avalonia-aware subscribers on the UI thread even
    ///     when raised from a background continuation.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
    public MainWindowShell(
        WorkspaceModel workspaceModel,
        FileWatcher fileWatcher,
        DiagnosticsAggregator diagnosticsAggregator,
        ViewCatalogPresenter viewCatalogPresenter,
        LayoutInvoker layoutInvoker,
        DiagnosticsListView diagnosticsListView,
        SysmlSnippetGenerator snippetGenerator,
        RollingFileLogger logger,
        IUiDispatcher? uiDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(workspaceModel);
        ArgumentNullException.ThrowIfNull(fileWatcher);
        ArgumentNullException.ThrowIfNull(diagnosticsAggregator);
        ArgumentNullException.ThrowIfNull(viewCatalogPresenter);
        ArgumentNullException.ThrowIfNull(layoutInvoker);
        ArgumentNullException.ThrowIfNull(diagnosticsListView);
        ArgumentNullException.ThrowIfNull(snippetGenerator);
        ArgumentNullException.ThrowIfNull(logger);

        _workspaceModel = workspaceModel;
        _fileWatcher = fileWatcher;
        _diagnosticsAggregator = diagnosticsAggregator;
        _viewCatalogPresenter = viewCatalogPresenter;
        _layoutInvoker = layoutInvoker;
        _diagnosticsListView = diagnosticsListView;
        _snippetGenerator = snippetGenerator;
        _logger = logger;
        _uiDispatcher = uiDispatcher ?? new ImmediateUiDispatcher();

        // Eagerly establish a valid, empty (0-source) workspace snapshot at construction, so CurrentWorkspace is
        // never null. Safe to await synchronously here: a zero-source resolution's WorkspaceLoader.LoadAsync([])
        // call performs no I/O and does not throw (confirmed stdlib-only, diagnostic-free result), and
        // LoadInternalAsync uses ConfigureAwait(false) throughout so this cannot deadlock on a captured context.
        var emptySnapshot = _workspaceModel.LoadWorkspaceAsync(_sourceSet.Sources, _sourceSet.Resolve()).GetAwaiter().GetResult();
        CurrentWorkspace = emptySnapshot;
    }

    /// <summary>
    ///     Raises <see cref="TabsChanged" /> via the injected <see cref="IUiDispatcher" />, so the notification
    ///     always reaches subscribers on the dispatcher's target thread, regardless of which thread this method
    ///     is called from.
    /// </summary>
    private void RaiseTabsChanged()
    {
        _uiDispatcher.Post(() => TabsChanged?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    ///     Raises <see cref="SourcesChanged" /> via the injected <see cref="IUiDispatcher" />, so the
    ///     notification always reaches subscribers on the dispatcher's target thread, regardless of which thread
    ///     this method is called from.
    /// </summary>
    private void RaiseSourcesChanged()
    {
        _uiDispatcher.Post(() => SourcesChanged?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    ///     Adds a single file to the workspace's source set, or returns the current snapshot unchanged if the
    ///     same normalized path is already a registered file source.
    /// </summary>
    /// <param name="path">File path to add.</param>
    /// <returns>The freshly resolved and loaded workspace snapshot.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is null or whitespace.</exception>
    public async Task<WorkspaceSnapshot> AddFileSourceAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            _sourceSet.AddFile(path);
            var snapshot = await ApplySourceSetChangeAsync().ConfigureAwait(false);
            _logger.Log(LogLevel.Info, $"Workspace file source added: {path}");
            return snapshot;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.Log(LogLevel.Error, $"Failed to add workspace file source '{path}'", ex);
            throw;
        }
    }

    /// <summary>
    ///     Adds a folder to the workspace's source set, or returns the current snapshot unchanged if the same
    ///     normalized path is already a registered folder source.
    /// </summary>
    /// <param name="path">Folder path to add.</param>
    /// <returns>The freshly resolved and loaded workspace snapshot.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is null or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="path" /> does not exist.</exception>
    public async Task<WorkspaceSnapshot> AddFolderSourceAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            _sourceSet.AddFolder(path);
            var snapshot = await ApplySourceSetChangeAsync().ConfigureAwait(false);
            _logger.Log(LogLevel.Info, $"Workspace folder source added: {path}");
            return snapshot;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.Log(LogLevel.Error, $"Failed to add workspace folder source '{path}'", ex);
            throw;
        }
    }

    /// <summary>
    ///     Removes a source from the workspace's source set. A no-op (still resolves and reapplies) when
    ///     <paramref name="sourceId" /> does not refer to a currently registered source.
    /// </summary>
    /// <param name="sourceId">Identifier of the source to remove.</param>
    /// <returns>The freshly resolved and loaded workspace snapshot.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sourceId" /> is null or whitespace.</exception>
    public async Task<WorkspaceSnapshot> RemoveSourceAsync(string sourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        try
        {
            _sourceSet.RemoveSource(sourceId);
            var snapshot = await ApplySourceSetChangeAsync().ConfigureAwait(false);
            _logger.Log(LogLevel.Info, $"Workspace source removed: {sourceId}");
            return snapshot;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.Log(LogLevel.Error, $"Failed to remove workspace source '{sourceId}'", ex);
            throw;
        }
    }

    /// <summary>
    ///     Closes every source in the workspace's source set at once, returning the shell to the same valid
    ///     empty state used at construction (no sources, no watched sources, no open tabs/diagnostics).
    /// </summary>
    /// <returns>The freshly resolved and loaded (empty) workspace snapshot.</returns>
    public async Task<WorkspaceSnapshot> CloseAllSourcesAsync()
    {
        try
        {
            _sourceSet.ClearSources();
            var snapshot = await ApplySourceSetChangeAsync().ConfigureAwait(false);
            _logger.Log(LogLevel.Info, "All workspace sources closed");
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Failed to close all workspace sources", ex);
            throw;
        }
    }

    /// <summary>
    ///     Resolves the current <see cref="_sourceSet" />, loads the resulting workspace, applies the snapshot,
    ///     diffs the previous and new watch sets to call <see cref="FileWatcher.WatchSource" />/
    ///     <see cref="FileWatcher.UnwatchSource" /> only for sources that actually changed, and raises
    ///     <see cref="SourcesChanged" />.
    /// </summary>
    /// <returns>The freshly resolved and loaded workspace snapshot.</returns>
    /// <remarks>
    ///     Newly added sources are watched <em>before</em> any snapshot/dictionary state is applied and before
    ///     <see cref="SourcesChanged" /> is raised. <see cref="FileWatcher.WatchSource" /> can throw (for
    ///     example <see cref="DirectoryNotFoundException" /> if a folder was deleted or became inaccessible in
    ///     the narrow window between <see cref="WorkspaceSourceSet.AddFolder" />'s own upfront check and this
    ///     call - a real TOCTOU race, not a theoretical one). If it does, the failing source is rolled back out
    ///     of <see cref="_sourceSet" /> and the exception rethrown before any other shell state is touched, so
    ///     the shell never ends up with a source that is registered in <see cref="_sourceSet" /> yet invisible in
    ///     the UI (which only rebuilds from <see cref="SourcesChanged" />) and permanently unwatchable (once its
    ///     id were marked watched despite the watcher never having been created).
    /// </remarks>
    private async Task<WorkspaceSnapshot> ApplySourceSetChangeAsync()
    {
        // Start watching every newly added source (idempotent for sources already watched) before applying any
        // other state, so a watch failure can be rolled back cleanly - see remarks above.
        foreach (var source in _sourceSet.Sources)
        {
            if (_watchedSourceIds.Contains(source.Id))
            {
                continue;
            }

            try
            {
                _fileWatcher.WatchSource(source);
                _watchedSourceIds.Add(source.Id);
            }
            catch (Exception)
            {
                _sourceSet.RemoveSource(source.Id);
                throw;
            }
        }

        var resolution = _sourceSet.Resolve();
        var snapshot = await _workspaceModel.LoadWorkspaceAsync(_sourceSet.Sources, resolution).ConfigureAwait(false);
        ApplyWorkspaceSnapshot(snapshot);
        CurrentSourceIdToFiles = resolution.SourceIdToFiles;

        var currentSourceIds = _sourceSet.Sources.Select(s => s.Id).ToHashSet();

        // Stop watching every source no longer present in the set.
        foreach (var staleSourceId in _watchedSourceIds.Where(id => !currentSourceIds.Contains(id)).ToList())
        {
            _fileWatcher.UnwatchSource(staleSourceId);
            _watchedSourceIds.Remove(staleSourceId);
        }

        RaiseSourcesChanged();
        return snapshot;
    }

    /// <summary>
    ///     Re-applies pending external file changes by reloading the workspace and refreshing all
    ///     workspace-derived shell state.
    /// </summary>
    /// <remarks>
    ///     Safe to call with zero currently watched sources: the shell then skips
    ///     <see cref="FileWatcher.FlushPendingChanges" /> entirely (which itself requires at least one watched
    ///     source) and reloads against zero changed paths, which is a valid, cheap no-op-ish reload rather than a
    ///     failure - refreshing an empty workspace is a first-class, supported operation. Always re-resolves
    ///     <see cref="_sourceSet" /> before reloading, so a file created or deleted externally under a
    ///     still-registered folder source is actually picked up, rather than the reload recomputing against a
    ///     stale file list captured at the last explicit Add/Remove source mutation.
    /// </remarks>
    /// <returns>The refreshed workspace snapshot.</returns>
    public async Task<WorkspaceSnapshot> RefreshFromExternalChangesAsync()
    {
        IReadOnlyList<string> changedPaths = _watchedSourceIds.Count == 0 ? [] : _fileWatcher.FlushPendingChanges();

        try
        {
            var resolution = _sourceSet.Resolve();
            var snapshot = await _workspaceModel.ReloadFilesAsync(changedPaths, resolution).ConfigureAwait(false);
            ApplyWorkspaceSnapshot(snapshot);
            CurrentSourceIdToFiles = resolution.SourceIdToFiles;
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
    ///     Renders a predefined view selected by the user, opening a new diagram tab for it or, if a tab for the
    ///     same <paramref name="viewId" /> is already open, re-rendering into and activating that existing tab
    ///     instead of duplicating it.
    /// </summary>
    /// <param name="viewId">Identifier from <see cref="ViewCatalogPresenter" />.</param>
    /// <returns>Rendered SVG markup now loaded into the resulting tab's canvas.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no workspace is loaded.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewId" /> is not present in the catalog.</exception>
    public string SelectPredefinedView(string viewId)
    {
        if (CurrentWorkspace.Sources.Count == 0)
        {
            throw new InvalidOperationException("A workspace source must be added before a view can be selected.");
        }

        var descriptor = _viewCatalogPresenter.SelectView(viewId);

        try
        {
            var svg = _layoutInvoker.RenderPredefinedView(CurrentWorkspace.Workspace, descriptor);
            var definition = _viewCatalogPresenter.BuildViewDefinition(CurrentWorkspace.Workspace, descriptor.QualifiedName);
            var tab = EnsureTabOpen(descriptor.QualifiedName, descriptor.DisplayName, WorkbenchTabKind.PredefinedView);
            if (tab.SourceDefinition != definition)
            {
                tab = tab with { SourceDefinition = definition };
                _openTabs[_openTabs.FindIndex(t => t.Id == tab.Id)] = tab;
            }

            tab.Canvas.LoadSvg(svg);
            ActivePredefinedView = descriptor;
            ActiveCustomView = null;
            ActiveTabId = tab.Id;
            RaiseTabsChanged();
            return svg;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"Failed to render predefined view '{viewId}'.", ex);
            throw;
        }
    }

    /// <summary>
    ///     Renders the current GUI-authored custom view as a live preview. If the currently active tab is itself
    ///     a <see cref="WorkbenchTabKind.CustomViewPreview" /> tab, it is re-rendered in place (same tab identity
    ///     and canvas); otherwise a brand-new custom-view-preview tab is opened and made active - this covers
    ///     both the "active tab is a predefined view" and "no tab is open" cases.
    /// </summary>
    /// <param name="definition">Normalized custom-view state.</param>
    /// <returns>Rendered SVG markup now loaded into the resulting tab's canvas.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition" /> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no workspace is loaded, or when <paramref name="definition" /> does not validate against
    ///     the current workspace.
    /// </exception>
    public string PreviewCustomView(ViewDefinitionModel definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (CurrentWorkspace.Sources.Count == 0)
        {
            throw new InvalidOperationException("A workspace source must be added before a custom view can be previewed.");
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
            var title = definition.DisplayName ?? "Custom View";

            WorkbenchTab tab;
            if (ActiveTab is { Kind: WorkbenchTabKind.CustomViewPreview } activeTab)
            {
                // Re-render in place: same tab identity and canvas, refreshed title and source definition.
                tab = activeTab with { Title = title, SourceDefinition = definition };
                _openTabs[_openTabs.FindIndex(t => t.Id == activeTab.Id)] = tab;
            }
            else
            {
                tab = CreateTab(NextCustomPreviewTabId(), title, WorkbenchTabKind.CustomViewPreview) with { SourceDefinition = definition };
                _openTabs.Add(tab);
            }

            tab.Canvas.LoadSvg(svg);
            ActiveCustomView = definition;
            ActivePredefinedView = null;
            ActiveTabId = tab.Id;
            RaiseTabsChanged();
            return svg;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Failed to render custom view preview.", ex);
            throw;
        }
    }

    /// <summary>
    ///     Renders the current GUI-authored custom view definition as a live preview <b>without</b> mutating any
    ///     open-tab state: unlike <see cref="PreviewCustomView" />, this method never touches
    ///     <see cref="OpenTabs" />, <see cref="ActiveTabId" />, or <see cref="ActiveCustomView" />, and never
    ///     raises <see cref="TabsChanged" />. Backs <c>ViewBuilderDialog</c>'s left-hand live preview pane, which
    ///     must be able to re-render on every in-progress edit while the dialog is open without leaking a real
    ///     tab into the main window before the user commits via the dialog's OK button.
    /// </summary>
    /// <param name="definition">Normalized custom-view state.</param>
    /// <returns>Rendered SVG markup for the given definition.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="definition" /> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no workspace is loaded, or when <paramref name="definition" /> does not validate against
    ///     the current workspace.
    /// </exception>
    public string RenderCustomViewPreview(ViewDefinitionModel definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (CurrentWorkspace.Sources.Count == 0)
        {
            throw new InvalidOperationException("A workspace source must be added before a custom view can be previewed.");
        }

        var validation = definition.ValidateAgainstWorkspace(CurrentWorkspace.Workspace);
        if (validation.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException(
                "The custom view definition does not validate against the current workspace: "
                + string.Join("; ", validation.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message)));
        }

        var svg = _layoutInvoker.RenderCustomView(CurrentWorkspace.Workspace, definition);
        _logger.Log(LogLevel.Info, "Custom view live preview rendered.");
        return svg;
    }

    /// <summary>
    ///     Opens a brand-new, empty custom-view-preview tab and makes it the active tab, without rendering
    ///     anything into it yet. Backs <c>ViewBuilderDialog</c>'s OK commit path: the dialog calls this method
    ///     immediately followed by <see cref="PreviewCustomView" />, so a subsequent <see cref="PreviewCustomView" />
    ///     call re-renders into this same tab per that method's in-place-update rule, since it becomes the active
    ///     custom-view-preview tab.
    /// </summary>
    /// <returns>The newly opened, empty tab.</returns>
    public WorkbenchTab OpenNewCustomPreviewTab()
    {
        var tab = CreateTab(NextCustomPreviewTabId(), "Custom View", WorkbenchTabKind.CustomViewPreview);
        _openTabs.Add(tab);
        ActiveTabId = tab.Id;
        RaiseTabsChanged();
        return tab;
    }

    /// <summary>
    ///     Closes the diagram tab with the given identifier, if one is open. If the closed tab was the active
    ///     tab, a neighboring tab becomes active, or <see cref="ActiveTabId" /> becomes <see langword="null" />
    ///     if no tabs remain.
    /// </summary>
    /// <param name="tabId">Identifier of the tab to close.</param>
    public void CloseDiagramTab(string tabId)
    {
        var index = _openTabs.FindIndex(t => t.Id == tabId);
        if (index < 0)
        {
            return;
        }

        _openTabs.RemoveAt(index);

        if (ActiveTabId == tabId)
        {
            ActiveTabId = _openTabs.Count == 0 ? null : _openTabs[Math.Min(index, _openTabs.Count - 1)].Id;
        }

        RaiseTabsChanged();
    }

    /// <summary>
    ///     Notifies the shell that a diagram tab has gained UI focus, so a subsequent <see cref="PreviewCustomView" />
    ///     call knows which tab is "active" for its in-place-update-or-new-tab decision. Called by the
    ///     Avalonia-aware UI layer when Dock reports a focus change onto a diagram document.
    /// </summary>
    /// <param name="tabId">Identifier of the newly focused diagram tab.</param>
    public void NotifyActiveDiagramTab(string? tabId)
    {
        if (tabId is not null && _openTabs.All(tab => tab.Id != tabId))
        {
            // Unknown or stale id (for example, a queued focus notification for a tab that has since closed) -
            // ignored rather than clearing a still-valid ActiveTabId.
            return;
        }

        ActiveTabId = tabId;
    }

    /// <summary>
    ///     Returns the canvas host owned by the given open tab.
    /// </summary>
    /// <param name="tabId">Identifier of an open tab.</param>
    /// <returns>The tab's canvas host, or <see langword="null" /> if no tab with that identifier is open.</returns>
    public SvgCanvasHost? GetTabCanvas(string tabId)
    {
        return _openTabs.FirstOrDefault(tab => tab.Id == tabId)?.Canvas;
    }

    /// <summary>
    ///     Returns the workspace file path presented by the given open <see cref="WorkbenchTabKind.SourceText" />
    ///     tab.
    /// </summary>
    /// <param name="tabId">Identifier of an open tab.</param>
    /// <returns>
    ///     The tab's file path, or <see langword="null" /> if no tab with that identifier is open, or the tab is
    ///     not a <see cref="WorkbenchTabKind.SourceText" /> tab.
    /// </returns>
    public string? GetTabFilePath(string tabId)
    {
        return _openTabs.FirstOrDefault(tab => tab.Id == tabId)?.FilePath;
    }

    /// <summary>
    ///     Opens a read-only source-text tab for the given workspace file, reusing an already-open tab for the
    ///     same path instead of duplicating it.
    /// </summary>
    /// <param name="filePath">Absolute path of the file to present.</param>
    /// <returns>The existing (refocused) or newly opened tab.</returns>
    /// <remarks>
    ///     The absolute <paramref name="filePath" /> itself is used as the tab's stable dedupe <see cref="WorkbenchTab.Id" />
    ///     (mirroring how <see cref="SelectPredefinedView" /> uses a predefined view's qualified name), and the
    ///     file's own name (<see cref="Path.GetFileName(string)" />) becomes the tab's <see cref="WorkbenchTab.Title" />.
    ///     Unlike a diagram tab, no rendering happens here - <see cref="SourceTextDocumentViewModel" /> reads the
    ///     file's contents directly (and handles a missing/locked file itself) when the tab is realized.
    /// </remarks>
    public WorkbenchTab OpenSourceTextTab(string filePath)
    {
        var tab = EnsureTabOpen(filePath, Path.GetFileName(filePath), WorkbenchTabKind.SourceText);
        if (tab.FilePath != filePath)
        {
            tab = tab with { FilePath = filePath };
            _openTabs[_openTabs.FindIndex(t => t.Id == tab.Id)] = tab;
        }

        ActiveTabId = tab.Id;
        RaiseTabsChanged();
        return tab;
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
    ///     Reports whether the given open diagram tab has a derivable source definition and can therefore export
    ///     its diagram as a SysML <c>view</c> snippet via <see cref="ExportTabAsSysmlSnippet" />. Backs the
    ///     enabled/disabled state of every diagram tab's "Copy as SysML" context-menu item.
    /// </summary>
    /// <param name="tabId">Identifier of an open tab.</param>
    /// <returns>
    ///     <see langword="true" /> when the tab is open and its <see cref="WorkbenchTab.SourceDefinition" /> is
    ///     ready to export; <see langword="false" /> when the tab is unknown, has no source definition (for
    ///     example an empty custom-preview tab, or an unscoped predefined view with zero expose members), or that
    ///     definition is not yet ready to export.
    /// </returns>
    public bool CanExportTabAsSysml(string tabId)
    {
        var tab = _openTabs.FirstOrDefault(t => t.Id == tabId);
        return tab?.SourceDefinition?.IsReadyToExport == true;
    }

    /// <summary>
    ///     Generates copy-pasteable SysML <c>view</c> text for the diagram currently rendered in the given open
    ///     tab, reusing whichever <see cref="ViewDefinitionModel" /> produced that diagram - whether it came from a
    ///     predefined view or a custom-view-builder preview - so every diagram tab funnels through the same
    ///     snippet-generation path rather than duplicating it per tab kind.
    /// </summary>
    /// <param name="tabId">Identifier of an open tab.</param>
    /// <returns>
    ///     Complete SysML view snippet, or <see langword="null" /> when
    ///     <see cref="CanExportTabAsSysml" /> would report <see langword="false" /> for <paramref name="tabId" /> -
    ///     this is an expected, valid outcome (not a failure), so it is reported rather than thrown.
    /// </returns>
    public string? ExportTabAsSysmlSnippet(string tabId)
    {
        var tab = _openTabs.FirstOrDefault(t => t.Id == tabId);
        if (tab?.SourceDefinition is not { IsReadyToExport: true } definition)
        {
            _logger.Log(LogLevel.Info, $"Tab '{tabId}' has no derivable SysML view definition to copy.");
            return null;
        }

        var snippet = _snippetGenerator.GenerateSnippet(definition);
        _logger.Log(LogLevel.Info, $"Diagram tab '{tabId}' copied as a SysML snippet.");
        return snippet;
    }

    /// <summary>
    ///     Applies a freshly loaded or reloaded workspace snapshot to all workspace-derived shell state.
    /// </summary>
    /// <param name="snapshot">Newly published workspace snapshot.</param>
    /// <remarks>
    ///     Every open tab is closed as part of this reset: a reloaded/newly-opened workspace invalidates every
    ///     currently-rendered SVG (predefined views reference the old workspace's element tree, and custom-view
    ///     previews were validated against the old workspace via <c>ValidateAgainstWorkspace</c>). Keeping stale
    ///     tabs open would silently show diagrams that no longer correspond to any current workspace state, which
    ///     is the same reason <see cref="ActivePredefinedView" />/<see cref="ActiveCustomView" /> are reset here
    ///     too. The custom-preview tab-id sequence counter is deliberately not reset alongside <see cref="_openTabs" />
    ///     so ids remain unique for the shell instance's entire lifetime.
    /// </remarks>
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
        ActiveTabId = null;
        RaiseTabsChanged();
    }

    /// <summary>
    ///     Returns the already-open tab for the given identifier, or opens and returns a new one if none is open.
    /// </summary>
    /// <param name="id">Stable tab identifier.</param>
    /// <param name="title">User-facing tab label.</param>
    /// <param name="kind">Content category shown by the tab.</param>
    /// <returns>The existing or newly opened tab.</returns>
    private WorkbenchTab EnsureTabOpen(string id, string title, WorkbenchTabKind kind)
    {
        var existing = _openTabs.FirstOrDefault(tab => tab.Id == id);
        if (existing is not null)
        {
            return existing;
        }

        var tab = CreateTab(id, title, kind);
        _openTabs.Add(tab);
        return tab;
    }

    /// <summary>
    ///     Constructs a new tab with a freshly created, independent canvas host.
    /// </summary>
    /// <param name="id">Stable tab identifier.</param>
    /// <param name="title">User-facing tab label.</param>
    /// <param name="kind">Content category shown by the tab.</param>
    /// <returns>A new, not-yet-registered tab.</returns>
    private static WorkbenchTab CreateTab(string id, string title, WorkbenchTabKind kind)
    {
        return new WorkbenchTab(id, title, kind, new SvgCanvasHost());
    }

    /// <summary>
    ///     Generates the next unique custom-view-preview tab identifier, drawing from a counter that persists for
    ///     the whole shell lifetime (see <see cref="ApplyWorkspaceSnapshot" />'s remarks).
    /// </summary>
    /// <returns>A unique, stable tab identifier.</returns>
    private string NextCustomPreviewTabId()
    {
        return $"$custom-preview-{_customPreviewTabSequence++}";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _fileWatcher.Dispose();
    }
}
