using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.AppShellSubsystem;

/// <summary>
///     Unit tests for <see cref="MainWindowShell" />.
/// </summary>
public sealed class MainWindowShellTests : IDisposable
{
    /// <summary>
    ///     Temporary workspace root folder created fresh for each test and removed on disposal.
    /// </summary>
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;

    /// <summary>
    ///     Temporary log directory created fresh for each test and removed on disposal.
    /// </summary>
    private readonly string _tempLogRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-logs-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        if (Directory.Exists(_tempLogRoot))
        {
            Directory.Delete(_tempLogRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Writes a small sample workspace with one predefined view and two elements that can be exposed.
    /// </summary>
    private async Task WriteSampleWorkspaceAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    part def Wheel;\n"
            + "    view PredefinedView {\n"
            + "        expose Engine;\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "    view PredefinedView2 {\n"
            + "        expose Wheel;\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Builds a shell wired with real (non-mocked) subsystem units.
    /// </summary>
    /// <param name="fileWatcher">
    ///     File watcher to wire into the shell. Defaults to a freshly created watcher when not supplied, matching
    ///     every existing test's expectations.
    /// </param>
    private MainWindowShell CreateShell(FileWatcher? fileWatcher = null)
    {
        return new MainWindowShell(
            new WorkspaceModel(),
            fileWatcher ?? new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot));
    }

    /// <summary>
    ///     Validates that opening a workspace arranges the primary workspace, diagram, and diagnostics regions:
    ///     the catalog, diagnostics list, and canvas host all reflect the freshly loaded workspace.
    /// </summary>
    [Fact]
    public async Task Startup_ArrangesPrimaryWorkspaceAndDiagramRegions()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();

        // Act
        var snapshot = await shell.AddFolderSourceAsync(_tempRoot);

        // Assert: the workspace is loaded and downstream regions were refreshed
        Assert.Same(snapshot, shell.CurrentWorkspace);
        Assert.Equal(2, shell.ViewCatalog.AvailableViews.Count);
        Assert.False(shell.Canvas.IsContentLoaded);
    }

    /// <summary>
    ///     Validates that opening tabs manages tabbed presentation: selecting a predefined view and then
    ///     previewing a custom view each open a distinct tab, without duplicating an already-open tab.
    /// </summary>
    [Fact]
    public async Task OpenViews_ManagesTabbedPresentation()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];

        // Act: select the predefined view twice - the second selection must not duplicate the tab
        shell.SelectPredefinedView(view.QualifiedName);
        shell.SelectPredefinedView(view.QualifiedName);

        // Assert
        Assert.Single(shell.OpenTabs);
        Assert.Equal(WorkbenchTabKind.PredefinedView, shell.OpenTabs[0].Kind);
        Assert.Equal(shell.OpenTabs[0].Id, shell.ActiveTabId);

        // Act: preview a custom view, opening a second, distinct tab
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        shell.PreviewCustomView(definition);

        // Assert
        Assert.Equal(2, shell.OpenTabs.Count);
        Assert.Contains(shell.OpenTabs, t => t.Kind == WorkbenchTabKind.CustomViewPreview);
    }

    /// <summary>
    ///     Validates that reloading the workspace after an external change resynchronizes visible shell regions
    ///     (diagnostics and catalog) and resets stale active-view state.
    /// </summary>
    [Fact]
    public async Task SessionStateChanges_SynchronizeVisibleRegions()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        Assert.NotNull(shell.ActivePredefinedView);

        // Act: simulate an external edit and a debounce-window flush, then refresh
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Extra.sysml"),
            "package Extra {\n    part def Bracket;\n}\n",
            TestContext.Current.CancellationToken);
        await Task.Delay(5, TestContext.Current.CancellationToken);
        var refreshed = await shell.RefreshFromExternalChangesAsync();

        // Assert: the refreshed workspace now includes the new file and prior active-view state was reset
        Assert.Equal(2, refreshed.Files.Count);
        Assert.Null(shell.ActivePredefinedView);
    }

    /// <summary>
    ///     Validates that the full round trip of selecting a custom view kind, exposing targets, previewing, and
    ///     exporting a snippet works end to end from the shell.
    /// </summary>
    [Fact]
    public async Task CustomViewWorkflow_PreviewsAndExportsFromShell()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.Interconnection);
        definition.AddExposeTarget("Sample::Engine");
        definition.AddExposeTarget("Sample::Wheel");
        definition.SetDisplayName("EngineOverview");

        // Act: preview, then export
        var svg = shell.PreviewCustomView(definition);
        var snippet = shell.ExportCustomViewSnippet(definition);

        // Assert
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("view EngineOverview {", snippet);
        Assert.Contains("expose Sample::Engine::**;", snippet);
        Assert.Contains("expose Sample::Wheel::**;", snippet);
        Assert.True(shell.Canvas.IsContentLoaded);
    }

    /// <summary>
    ///     Validates that a rendered predefined-view diagram tab can export its diagram as a SysML snippet, and
    ///     that <see cref="MainWindowShell.CanExportTabAsSysml" /> reports readiness consistently.
    /// </summary>
    [Fact]
    public async Task ExportTabAsSysmlSnippet_PredefinedViewTab_ReturnsSnippet()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        var tabId = shell.ActiveTabId!;

        // Act
        var canExport = shell.CanExportTabAsSysml(tabId);
        var snippet = shell.ExportTabAsSysmlSnippet(tabId);

        // Assert
        Assert.True(canExport);
        Assert.NotNull(snippet);
        Assert.Contains("view PredefinedView {", snippet);
        Assert.Contains("expose Sample::Engine;", snippet);
    }

    /// <summary>
    ///     Validates that a custom-view-preview diagram tab can export its diagram as a SysML snippet.
    /// </summary>
    [Fact]
    public async Task ExportTabAsSysmlSnippet_CustomPreviewTab_ReturnsSnippet()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        definition.SetDisplayName("EngineOnly");
        shell.PreviewCustomView(definition);
        var tabId = shell.ActiveTabId!;

        // Act
        var canExport = shell.CanExportTabAsSysml(tabId);
        var snippet = shell.ExportTabAsSysmlSnippet(tabId);

        // Assert
        Assert.True(canExport);
        Assert.NotNull(snippet);
        Assert.Contains("view EngineOnly {", snippet);
    }

    /// <summary>
    ///     Validates that an unknown tab id reports no export readiness and returns <see langword="null" /> rather
    ///     than throwing.
    /// </summary>
    [Fact]
    public async Task ExportTabAsSysmlSnippet_UnknownTabId_ReturnsNull()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        // Act / Assert
        Assert.False(shell.CanExportTabAsSysml("does-not-exist"));
        Assert.Null(shell.ExportTabAsSysmlSnippet("does-not-exist"));
    }

    /// <summary>
    ///     Validates that a predefined view with zero expose members (a valid, unscoped "expose everything" view)
    ///     cannot be exported, since there is no finite expose list to serialize - it is reported gracefully
    ///     rather than throwing.
    /// </summary>
    [Fact]
    public async Task ExportTabAsSysmlSnippet_PredefinedViewWithNoExposeMembers_ReturnsNull()
    {
        // Arrange: a workspace whose predefined view declares no expose members at all
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    view UnscopedView {\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        var tabId = shell.ActiveTabId!;

        // Act / Assert
        Assert.False(shell.CanExportTabAsSysml(tabId));
        Assert.Null(shell.ExportTabAsSysmlSnippet(tabId));
    }

    /// <summary>
    ///     Validates that <see cref="MainWindowShell.CanExportTabAsSysml" /> mirrors the same true/false outcomes
    ///     as <see cref="MainWindowShell.ExportTabAsSysmlSnippet" /> across the exportable, unknown-tab, and
    ///     no-expose-members cases.
    /// </summary>
    [Fact]
    public async Task CanExportTabAsSysml_MirrorsExportability()
    {
        // Arrange: an exportable predefined-view tab
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        var exportableTabId = shell.ActiveTabId!;

        // Assert: exportable tab
        Assert.True(shell.CanExportTabAsSysml(exportableTabId));
        Assert.NotNull(shell.ExportTabAsSysmlSnippet(exportableTabId));

        // Assert: unknown tab
        Assert.False(shell.CanExportTabAsSysml("unknown-tab"));
        Assert.Null(shell.ExportTabAsSysmlSnippet("unknown-tab"));
    }

    /// <summary>
    ///     Validates that selecting a predefined view is rejected while the workspace has zero sources.
    /// </summary>
    [Fact]
    public async Task SelectPredefinedView_NoWorkspaceOpened_ThrowsInvalidOperationException()
    {
        using var shell = CreateShell();

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => shell.AddFolderSourceAsync(Path.Combine(_tempRoot, "missing")));
        Assert.Throws<InvalidOperationException>(() => shell.SelectPredefinedView("anything"));
    }

    /// <summary>
    ///     Validates that previewing a custom view while a custom-view-preview tab is already active re-renders
    ///     in place rather than opening a second tab (product decision 3, first bullet).
    /// </summary>
    [Fact]
    public async Task PreviewCustomView_WhenActiveTabIsCustomPreview_UpdatesInPlace()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        var first = new ViewDefinitionModel();
        first.SetViewKind(ViewKind.General);
        first.AddExposeTarget("Sample::Engine");
        first.SetDisplayName("First");

        var second = new ViewDefinitionModel();
        second.SetViewKind(ViewKind.General);
        second.AddExposeTarget("Sample::Wheel");
        second.SetDisplayName("Second");

        // Act
        shell.PreviewCustomView(first);
        var firstTabId = shell.ActiveTabId;
        shell.PreviewCustomView(second);

        // Assert: same tab identity reused, and the second definition's content is what is now shown
        Assert.Single(shell.OpenTabs);
        Assert.Equal(firstTabId, shell.ActiveTabId);
        Assert.Equal("Second", shell.OpenTabs[0].Title);
    }

    /// <summary>
    ///     Validates that previewing a custom view while a predefined-view tab is active opens a brand-new,
    ///     distinct custom-preview tab rather than reusing or replacing the predefined-view tab (product decision
    ///     3, second bullet).
    /// </summary>
    [Fact]
    public async Task PreviewCustomView_WhenActiveTabIsPredefinedView_OpensNewTab()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        var predefinedTabId = shell.ActiveTabId;

        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");

        // Act
        shell.PreviewCustomView(definition);

        // Assert
        Assert.Equal(2, shell.OpenTabs.Count);
        Assert.NotEqual(predefinedTabId, shell.ActiveTabId);
        Assert.Contains(shell.OpenTabs, t => t.Id == predefinedTabId && t.Kind == WorkbenchTabKind.PredefinedView);
        Assert.Contains(shell.OpenTabs, t => t.Id == shell.ActiveTabId && t.Kind == WorkbenchTabKind.CustomViewPreview);
    }

    /// <summary>
    ///     Validates that previewing a custom view with zero tabs open opens exactly one new, active custom-preview
    ///     tab (product decision 3's "or there is no active/focused diagram tab at all" clause).
    /// </summary>
    [Fact]
    public async Task PreviewCustomView_WithNoTabsOpen_OpensNewTab()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");

        // Act
        shell.PreviewCustomView(definition);

        // Assert
        Assert.Single(shell.OpenTabs);
        Assert.Equal(WorkbenchTabKind.CustomViewPreview, shell.OpenTabs[0].Kind);
        Assert.Equal(shell.OpenTabs[0].Id, shell.ActiveTabId);
    }

    /// <summary>
    ///     Validates that <see cref="MainWindowShell.RenderCustomViewPreview" /> renders successfully without
    ///     mutating any open-tab state - unlike <see cref="MainWindowShell.PreviewCustomView" />, it must not
    ///     touch <see cref="MainWindowShell.OpenTabs" />, <see cref="MainWindowShell.ActiveTabId" />, or
    ///     <see cref="MainWindowShell.ActiveCustomView" />.
    /// </summary>
    [Fact]
    public async Task RenderCustomViewPreview_DoesNotMutateOpenTabsOrActiveTab()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");

        var openTabsBefore = shell.OpenTabs.Count;
        var activeTabIdBefore = shell.ActiveTabId;
        var activeCustomViewBefore = shell.ActiveCustomView;

        // Act
        var svg = shell.RenderCustomViewPreview(definition);

        // Assert
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(openTabsBefore, shell.OpenTabs.Count);
        Assert.Equal(activeTabIdBefore, shell.ActiveTabId);
        Assert.Equal(activeCustomViewBefore, shell.ActiveCustomView);
    }

    /// <summary>
    ///     Validates that <see cref="MainWindowShell.RenderCustomViewPreview" /> throws when no workspace has
    ///     been opened yet, matching <see cref="MainWindowShell.PreviewCustomView" />'s own empty-workspace
    ///     guard.
    /// </summary>
    [Fact]
    public void RenderCustomViewPreview_NoWorkspaceOpened_ThrowsInvalidOperationException()
    {
        using var shell = CreateShell();

        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");

        Assert.Throws<InvalidOperationException>(() => shell.RenderCustomViewPreview(definition));
    }

    /// <summary>
    ///     Validates the "+ New Diagram Tab" affordance end to end: it opens an empty, active tab, and a
    ///     subsequent preview call updates that same tab in place (product decision 4).
    /// </summary>
    [Fact]
    public async Task OpenNewCustomPreviewTab_OpensEmptyActiveTab_AndSubsequentPreviewUpdatesIt()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);

        // Act
        var newTab = shell.OpenNewCustomPreviewTab();

        // Assert: empty, active tab
        Assert.False(newTab.Canvas.IsContentLoaded);
        Assert.Equal(newTab.Id, shell.ActiveTabId);

        // Act: preview into it
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        shell.PreviewCustomView(definition);

        // Assert: still exactly one tab, now with content
        Assert.Single(shell.OpenTabs);
        Assert.Equal(newTab.Id, shell.OpenTabs[0].Id);
        Assert.True(shell.GetTabCanvas(newTab.Id)!.IsContentLoaded);
    }

    /// <summary>
    ///     Validates that closing a diagram tab removes it and reassigns the active tab to a neighbor, and that
    ///     closing the final tab leaves no open tabs, a null active tab, and an idle (empty) canvas.
    /// </summary>
    [Fact]
    public async Task CloseDiagramTab_RemovesTab_AndReassignsActiveTab()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var views = shell.ViewCatalog.AvailableViews;
        shell.SelectPredefinedView(views[0].QualifiedName);
        var firstTabId = shell.ActiveTabId!;
        shell.SelectPredefinedView(views[1].QualifiedName);
        var secondTabId = shell.ActiveTabId!;

        // Act: close the active (second) tab
        shell.CloseDiagramTab(secondTabId);

        // Assert: one tab remains and becomes active
        Assert.Single(shell.OpenTabs);
        Assert.Equal(firstTabId, shell.ActiveTabId);

        // Act: close the last remaining tab
        shell.CloseDiagramTab(firstTabId);

        // Assert: no tabs remain, no active tab, and the canvas falls back to the idle canvas
        Assert.Empty(shell.OpenTabs);
        Assert.Null(shell.ActiveTabId);
        Assert.False(shell.Canvas.IsContentLoaded);
    }

    /// <summary>
    ///     Validates that notifying the shell of an unknown/stale tab id is ignored rather than clearing a
    ///     still-valid <see cref="MainWindowShell.ActiveTabId" />.
    /// </summary>
    [Fact]
    public async Task NotifyActiveDiagramTab_UnknownId_IsIgnored()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        var activeTabId = shell.ActiveTabId;

        // Act
        shell.NotifyActiveDiagramTab("not-a-real-tab");

        // Assert
        Assert.Equal(activeTabId, shell.ActiveTabId);
    }

    /// <summary>
    ///     Validates that each diagram tab owns a fully independent canvas: opening two predefined views produces
    ///     two distinct canvas host instances, and zooming one does not affect the other's zoom level (the
    ///     central technical-debt fix motivating the multi-tab feature).
    /// </summary>
    [Fact]
    public async Task SelectPredefinedView_TabsHaveIndependentCanvases()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var views = shell.ViewCatalog.AvailableViews;

        // Act
        shell.SelectPredefinedView(views[0].QualifiedName);
        var id1 = shell.ActiveTabId!;
        shell.SelectPredefinedView(views[1].QualifiedName);
        var id2 = shell.ActiveTabId!;

        var canvas1 = shell.GetTabCanvas(id1)!;
        var canvas2 = shell.GetTabCanvas(id2)!;

        // Assert: distinct instances
        Assert.NotSame(canvas1, canvas2);

        // Act: zoom the second tab's canvas only
        canvas2.SetZoom(2.5);

        // Assert: the first tab's zoom is unaffected
        Assert.Equal(1.0, canvas1.ZoomLevel);
        Assert.Equal(2.5, canvas2.ZoomLevel);
    }

    /// <summary>
    ///     Validates that adding a second, distinct folder source is additive (not a replacement): both folders'
    ///     files remain in the workspace, both sources' ids are watched, and no previously accumulated pending
    ///     change state under the first folder is discarded just because a second source was added.
    /// </summary>
    [Fact]
    public async Task AddFolderSourceAsync_SecondDistinctFolder_IsAdditiveAndWatchesBothSources()
    {
        // Arrange: a second, distinct temporary workspace root, and a locally held file watcher reference so its
        // watched sources and pending state can be inspected directly.
        var secondRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
        try
        {
            await WriteSampleWorkspaceAsync();
            await File.WriteAllTextAsync(
                Path.Combine(secondRoot, "Other.sysml"),
                "package Other {\n    part def Bracket;\n}\n",
                TestContext.Current.CancellationToken);

            var fileWatcher = new FileWatcher(TimeSpan.FromMilliseconds(1));
            using var shell = CreateShell(fileWatcher);

            // Act: add folder source A, queue a pending change against A directly on the watcher, then add
            // folder source B.
            await shell.AddFolderSourceAsync(_tempRoot);
            Assert.Single(fileWatcher.WatchedSourceIds);
            var stalePathUnderA = Path.Combine(_tempRoot, "Stale.sysml");
            fileWatcher.QueueChange(stalePathUnderA);
            Assert.Contains(stalePathUnderA, fileWatcher.PendingChanges);

            var snapshot = await shell.AddFolderSourceAsync(secondRoot);

            // Assert: both sources are now registered and watched, both folders' files are present, and A's
            // pending change survived the addition of B (additive, not a replacement/retarget).
            Assert.Equal(2, shell.CurrentWorkspace.Sources.Count);
            Assert.Equal(2, fileWatcher.WatchedSourceIds.Count);
            Assert.Equal(2, snapshot.Files.Count);
            Assert.Contains(stalePathUnderA, fileWatcher.PendingChanges);
        }
        finally
        {
            Directory.Delete(secondRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Validates that removing sources down to zero produces a valid, non-throwing empty snapshot, unwatches
    ///     every previously watched source, and raises <see cref="MainWindowShell.SourcesChanged" /> for both the
    ///     add and the remove.
    /// </summary>
    [Fact]
    public async Task RemoveSourceAsync_DownToZeroSources_ProducesEmptySnapshotAndUnwatchesEverything()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        var fileWatcher = new FileWatcher(TimeSpan.FromMilliseconds(1));
        using var shell = CreateShell(fileWatcher);
        var sourcesChangedCount = 0;
        shell.SourcesChanged += (_, _) => sourcesChangedCount++;

        await shell.AddFolderSourceAsync(_tempRoot);
        var sourceId = shell.CurrentWorkspace.Sources[0].Id;

        // Act
        var snapshot = await shell.RemoveSourceAsync(sourceId);

        // Assert: a valid, empty snapshot; the source is no longer watched; and both mutations raised the event.
        Assert.Empty(snapshot.Sources);
        Assert.Empty(snapshot.Files);
        Assert.Empty(fileWatcher.WatchedSourceIds);
        Assert.Equal(2, sourcesChangedCount);
    }

    /// <summary>
    ///     Validates that <see cref="MainWindowShell.CloseAllSourcesAsync" /> closes every registered source at
    ///     once (a folder plus a file), producing the same valid empty snapshot shape as removing every source
    ///     one at a time, unwatching every previously watched source, and raising
    ///     <see cref="MainWindowShell.SourcesChanged" /> for every add plus the close-all.
    /// </summary>
    [Fact]
    public async Task CloseAllSourcesAsync_WithMultipleSources_ProducesEmptySnapshotAndUnwatchesEverything()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        var secondRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
        try
        {
            var filePath = Path.Combine(secondRoot, "Other.sysml");
            await File.WriteAllTextAsync(filePath, "package Other {\n    part def Bracket;\n}\n", TestContext.Current.CancellationToken);

            var fileWatcher = new FileWatcher(TimeSpan.FromMilliseconds(1));
            using var shell = CreateShell(fileWatcher);
            var sourcesChangedCount = 0;
            shell.SourcesChanged += (_, _) => sourcesChangedCount++;

            await shell.AddFolderSourceAsync(_tempRoot);
            await shell.AddFileSourceAsync(filePath);
            Assert.Equal(2, shell.CurrentWorkspace.Sources.Count);

            // Act
            var snapshot = await shell.CloseAllSourcesAsync();

            // Assert: a valid, empty snapshot; nothing is watched; and every mutation (2 adds + 1 close-all)
            // raised the event.
            Assert.Empty(snapshot.Sources);
            Assert.Empty(snapshot.Files);
            Assert.Empty(shell.CurrentWorkspace.Sources);
            Assert.Empty(fileWatcher.WatchedSourceIds);
            Assert.Equal(3, sourcesChangedCount);
        }
        finally
        {
            Directory.Delete(secondRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Validates that <see cref="MainWindowShell.SourcesChanged" /> is raised for each add mutation, and that
    ///     it is not raised at construction time (construction only establishes the initial empty snapshot; it
    ///     does not represent a source-set mutation).
    /// </summary>
    [Fact]
    public async Task SourcesChanged_RaisedOnAdd_NotRaisedAtConstruction()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        var raisedCount = 0;
        shell.SourcesChanged += (_, _) => raisedCount++;

        // Act
        await shell.AddFolderSourceAsync(_tempRoot);

        // Assert
        Assert.Equal(1, raisedCount);
    }

    /// <summary>
    ///     Validates that a freshly constructed shell already has a valid, non-null, zero-source
    ///     <see cref="MainWindowShell.CurrentWorkspace" /> snapshot, per the eager-empty-snapshot-at-construction
    ///     contract.
    /// </summary>
    [Fact]
    public void Construction_EstablishesValidEmptySnapshot()
    {
        // Arrange / Act
        using var shell = CreateShell();

        // Assert
        Assert.Empty(shell.CurrentWorkspace.Sources);
        Assert.Empty(shell.CurrentWorkspace.Files);
        Assert.NotNull(shell.CurrentWorkspace.Workspace);
    }

    /// <summary>
    ///     Validates that opening a source-text tab for a file creates exactly one new tab, makes it active, and
    ///     raises <see cref="MainWindowShell.TabsChanged" />.
    /// </summary>
    [Fact]
    public async Task OpenSourceTextTab_NewFile_CreatesOneNewActiveTab()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        var raisedCount = 0;
        shell.TabsChanged += (_, _) => raisedCount++;

        // Act
        var tab = shell.OpenSourceTextTab(filePath);

        // Assert
        Assert.Single(shell.OpenTabs);
        Assert.Equal(tab.Id, shell.ActiveTabId);
        Assert.Equal(WorkbenchTabKind.SourceText, tab.Kind);
        Assert.Equal(1, raisedCount);
    }

    /// <summary>
    ///     Validates that opening a source-text tab for a path that is already open re-focuses the existing tab
    ///     instead of duplicating it, while still raising <see cref="MainWindowShell.TabsChanged" /> for the
    ///     re-focus.
    /// </summary>
    [Fact]
    public async Task OpenSourceTextTab_SamePathTwice_RefocusesExistingTabWithoutDuplicating()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        var firstTab = shell.OpenSourceTextTab(filePath);

        // Open an unrelated tab so the source-text tab is no longer active, then re-open the same path.
        var otherFilePath = Path.Combine(_tempRoot, "Other.sysml");
        await File.WriteAllTextAsync(otherFilePath, "package Other {}\n", TestContext.Current.CancellationToken);
        shell.OpenSourceTextTab(otherFilePath);

        var raisedCount = 0;
        shell.TabsChanged += (_, _) => raisedCount++;

        // Act
        var reopenedTab = shell.OpenSourceTextTab(filePath);

        // Assert
        Assert.Equal(2, shell.OpenTabs.Count);
        Assert.Equal(firstTab.Id, reopenedTab.Id);
        Assert.Equal(reopenedTab.Id, shell.ActiveTabId);
        Assert.Equal(1, raisedCount);
    }

    /// <summary>
    ///     Validates that <see cref="MainWindowShell.GetTabFilePath" /> returns the correct path for an open
    ///     source-text tab, and <see langword="null" /> for a nonexistent tab id or a tab of a different kind.
    /// </summary>
    [Fact]
    public async Task GetTabFilePath_ReturnsPathForSourceTextTab_AndNullOtherwise()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.AddFolderSourceAsync(_tempRoot);
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        var sourceTextTab = shell.OpenSourceTextTab(filePath);
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        var diagramTabId = shell.ActiveTabId!;

        // Act / Assert
        Assert.Equal(filePath, shell.GetTabFilePath(sourceTextTab.Id));
        Assert.Null(shell.GetTabFilePath(diagramTabId));
        Assert.Null(shell.GetTabFilePath("not-a-real-tab"));
    }
}
