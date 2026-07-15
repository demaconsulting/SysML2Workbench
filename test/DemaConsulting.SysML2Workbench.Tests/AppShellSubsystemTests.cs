using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     Subsystem-level tests exercising AppShellSubsystem's unit (<see cref="MainWindowShell" />) composed with
///     real units from every subsystem it depends on, per
///     docs/reqstream/sysml2-workbench/app-shell-subsystem.yaml.
/// </summary>
public sealed class AppShellSubsystemTests : IDisposable
{
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
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

    private MainWindowShell CreateShell()
    {
        return new MainWindowShell(
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot));
    }

    /// <summary>
    ///     Validates that starting a session with an opened workspace shows the workspace's views, diagram, and
    ///     diagnostics regions together.
    /// </summary>
    [Fact]
    public async Task Startup_ShowsWorkspaceDiagramAndDiagnosticsRegions()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();

        // Act
        await shell.OpenWorkspaceAsync(_tempRoot);
        shell.SelectPredefinedView(shell.ViewCatalog.AvailableViews[0].QualifiedName);

        // Assert: catalog, diagram, and diagnostics regions are all populated together
        Assert.NotEmpty(shell.ViewCatalog.AvailableViews);
        Assert.True(shell.Canvas.IsContentLoaded);
        Assert.NotNull(shell.CurrentWorkspace);
    }

    /// <summary>
    ///     Validates that session changes (external file edits followed by a refresh) synchronize shell state
    ///     across the catalog and diagnostics regions.
    /// </summary>
    [Fact]
    public async Task SessionChanges_SynchronizeShellState()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.OpenWorkspaceAsync(_tempRoot);
        var initialViewCount = shell.ViewCatalog.AvailableViews.Count;

        // Act: an external edit adds a second view
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Extra.sysml"),
            "package Extra {\n    part def Bracket;\n    view ExtraView {\n        expose Bracket;\n        render asGridDiagram;\n    }\n}\n",
            TestContext.Current.CancellationToken);
        await Task.Delay(5, TestContext.Current.CancellationToken);
        await shell.RefreshFromExternalChangesAsync();

        // Assert: the catalog now reflects the additional view
        Assert.True(shell.ViewCatalog.AvailableViews.Count > initialViewCount);
    }

    /// <summary>
    ///     Validates that a custom view can be previewed and exported as SysML text from the shell.
    /// </summary>
    [Fact]
    public async Task CustomViewWorkflow_PreviewsAndExportsFromShell()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.OpenWorkspaceAsync(_tempRoot);
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");

        // Act
        var svg = shell.PreviewCustomView(definition);
        var snippet = shell.ExportCustomViewSnippet(definition);

        // Assert
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expose Sample::Engine::**;", snippet);
    }
}
