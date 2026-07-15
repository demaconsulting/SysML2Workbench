using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     System-level tests exercising the whole SysML2Workbench Phase 0 workflow end to end through
///     <see cref="MainWindowShell" />, per docs/reqstream/sysml2-workbench.yaml.
/// </summary>
public sealed class SysML2WorkbenchTests : IDisposable
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
    ///     Validates that opening a workspace loads it and, after an external change, refreshes it.
    /// </summary>
    [Fact]
    public async Task OpenWorkspace_LoadsAndRefreshesWorkspace()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();

        // Act: initial open
        var opened = await shell.OpenWorkspaceAsync(_tempRoot);

        // Assert
        Assert.Single(opened.Files);

        // Act: an external change followed by a refresh
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Extra.sysml"),
            "package Extra {\n    part def Bracket;\n}\n",
            TestContext.Current.CancellationToken);
        await Task.Delay(5, TestContext.Current.CancellationToken);
        var refreshed = await shell.RefreshFromExternalChangesAsync();

        // Assert
        Assert.Equal(2, refreshed.Files.Count);
    }

    /// <summary>
    ///     Validates that selecting a predefined view renders its diagram end to end.
    /// </summary>
    [Fact]
    public async Task SelectPredefinedView_RendersDiagram()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        using var shell = CreateShell();
        await shell.OpenWorkspaceAsync(_tempRoot);
        var view = shell.ViewCatalog.AvailableViews[0];

        // Act
        var svg = shell.SelectPredefinedView(view.QualifiedName);

        // Assert
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.True(shell.Canvas.IsContentLoaded);
    }

    /// <summary>
    ///     Validates the full GUI custom-view builder workflow: building a definition, previewing it, and
    ///     exporting it as a SysML snippet.
    /// </summary>
    [Fact]
    public async Task BuildCustomView_PreviewsAndExportsSnippet()
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

        // Act
        var svg = shell.PreviewCustomView(definition);
        var snippet = shell.ExportCustomViewSnippet(definition);

        // Assert
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("view EngineOverview {", snippet);
        Assert.Contains("expose Sample::Engine::**;", snippet);
        Assert.Contains("expose Sample::Wheel::**;", snippet);
        Assert.Contains("render asInterconnectionDiagram;", snippet);
    }

    /// <summary>
    ///     Validates that opening a workspace with parser/reference-resolution problems shows workspace
    ///     diagnostics.
    /// </summary>
    [Fact]
    public async Task OpenWorkspace_ShowsWorkspaceDiagnostics()
    {
        // Arrange: a file with a deliberate syntax error
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Broken.sysml"),
            "package Broken {\n    part def Widget\n",
            TestContext.Current.CancellationToken);
        using var shell = CreateShell();

        // Act
        await shell.OpenWorkspaceAsync(_tempRoot);

        // Assert
        Assert.NotEmpty(shell.Diagnostics.VisibleDiagnostics);
    }

    /// <summary>
    ///     Validates that starting a session opens the shell and writes operational log entries locally.
    /// </summary>
    [Fact]
    public async Task StartSession_OpensShellAndWritesOperationalLogs()
    {
        // Arrange
        await WriteSampleWorkspaceAsync();
        var logger = new RollingFileLogger(_tempLogRoot);
        using var shell = new MainWindowShell(
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            logger);

        // Act
        await shell.OpenWorkspaceAsync(_tempRoot);

        // Assert: the shell is usable and an operational log entry was written locally
        Assert.NotNull(shell.CurrentWorkspace);
        Assert.True(File.Exists(logger.ActiveFilePath));
        Assert.Contains("Workspace opened", File.ReadAllText(logger.ActiveFilePath));
    }
}
