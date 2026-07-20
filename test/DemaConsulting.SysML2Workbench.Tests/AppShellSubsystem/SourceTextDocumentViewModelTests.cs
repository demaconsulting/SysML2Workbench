using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.AppShellSubsystem;

/// <summary>
///     Unit tests for <see cref="SourceTextDocumentViewModel" />.
/// </summary>
public sealed class SourceTextDocumentViewModelTests : IDisposable
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
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot));
    }

    /// <summary>
    ///     Validates that <see cref="SourceTextDocumentViewModel.Text" /> equals the exact on-disk contents of the
    ///     file the tab presents.
    /// </summary>
    [Fact]
    public async Task Text_OpenedFile_MatchesFileContents()
    {
        // Arrange
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        var contents = "package Sample {\n    part def Engine;\n}\n";
        await File.WriteAllTextAsync(filePath, contents, TestContext.Current.CancellationToken);
        using var shell = CreateShell();
        var tab = shell.OpenSourceTextTab(filePath);

        // Act
        var viewModel = new SourceTextDocumentViewModel(shell, tab.Id);

        // Assert
        Assert.Equal(contents, viewModel.Text);
    }

    /// <summary>
    ///     Validates that the view model's title equals the opened file's own name.
    /// </summary>
    [Fact]
    public async Task Title_OpenedFile_EqualsFileName()
    {
        // Arrange
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await File.WriteAllTextAsync(filePath, "package Sample {}\n", TestContext.Current.CancellationToken);
        using var shell = CreateShell();
        var tab = shell.OpenSourceTextTab(filePath);

        // Act
        var viewModel = new SourceTextDocumentViewModel(shell, tab.Id);

        // Assert
        Assert.Equal("Sample.sysml", viewModel.Title);
    }

    /// <summary>
    ///     Validates that a missing/deleted file produces a friendly in-<see cref="SourceTextDocumentViewModel.Text" />
    ///     error message rather than throwing from the constructor.
    /// </summary>
    [Fact]
    public async Task Text_DeletedFile_ProducesFriendlyErrorMessage_DoesNotThrow()
    {
        // Arrange
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await File.WriteAllTextAsync(filePath, "package Sample {}\n", TestContext.Current.CancellationToken);
        using var shell = CreateShell();
        var tab = shell.OpenSourceTextTab(filePath);
        File.Delete(filePath);

        // Act
        var viewModel = new SourceTextDocumentViewModel(shell, tab.Id);

        // Assert
        Assert.NotNull(viewModel.Text);
        Assert.Contains(filePath, viewModel.Text);
    }
}
