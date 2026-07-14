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
            + "}\n",
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Builds a shell wired with real (non-mocked) subsystem units.
    /// </summary>
    private MainWindowShell CreateShell()
    {
        return new MainWindowShell(
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new SvgCanvasHost(),
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
        var snapshot = await shell.OpenWorkspaceAsync(_tempRoot);

        // Assert: the workspace is loaded and downstream regions were refreshed
        Assert.Same(snapshot, shell.CurrentWorkspace);
        Assert.Single(shell.ViewCatalog.AvailableViews);
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
        await shell.OpenWorkspaceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];

        // Act: select the predefined view twice - the second selection must not duplicate the tab
        shell.SelectPredefinedView(view.QualifiedName);
        shell.SelectPredefinedView(view.QualifiedName);

        // Assert
        Assert.Single(shell.OpenTabs);
        Assert.Equal(WorkbenchTabKind.PredefinedView, shell.OpenTabs[0].Kind);

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
        await shell.OpenWorkspaceAsync(_tempRoot);
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
        await shell.OpenWorkspaceAsync(_tempRoot);

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
    ///     Validates that opening a workspace is rejected before it exists.
    /// </summary>
    [Fact]
    public async Task SelectPredefinedView_NoWorkspaceOpened_ThrowsInvalidOperationException()
    {
        using var shell = CreateShell();

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => shell.OpenWorkspaceAsync(Path.Combine(_tempRoot, "missing")));
        Assert.Throws<InvalidOperationException>(() => shell.SelectPredefinedView("anything"));
    }
}
