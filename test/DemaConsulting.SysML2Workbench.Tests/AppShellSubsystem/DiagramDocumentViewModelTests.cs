using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.AppShellSubsystem;

/// <summary>
///     Unit tests for <see cref="DiagramDocumentViewModel" />.
/// </summary>
public sealed class DiagramDocumentViewModelTests : IDisposable
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
    ///     Fake <see cref="IClipboardService" /> test double that captures the last copied text instead of
    ///     touching any real OS clipboard.
    /// </summary>
    private sealed class FakeClipboardService : IClipboardService
    {
        public string? LastCopiedText { get; private set; }

        public Task SetTextAsync(string text)
        {
            LastCopiedText = text;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Builds a shell wired with real (non-mocked) subsystem units, with a small workspace containing one
    ///     predefined view already loaded.
    /// </summary>
    private async Task<MainWindowShell> CreateShellWithSampleWorkspaceAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    view PredefinedView {\n"
            + "        expose Engine;\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "}\n");

        var shell = new MainWindowShell(
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot));

        await shell.AddFolderSourceAsync(_tempRoot);
        return shell;
    }

    /// <summary>
    ///     Validates that a diagram document view model whose tab has a derivable definition reports
    ///     <see cref="DiagramDocumentViewModel.CanCopyAsSysml" /> as <see langword="true" /> and copies the
    ///     generated snippet to the injected clipboard service.
    /// </summary>
    [Fact]
    public async Task CopyAsSysmlAsync_ExportableTab_CopiesSnippetToClipboard()
    {
        // Arrange
        using var shell = await CreateShellWithSampleWorkspaceAsync();
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        var tabId = shell.ActiveTabId!;

        var viewModel = new DiagramDocumentViewModel(shell, tabId);
        var clipboard = new FakeClipboardService();
        viewModel.ClipboardService = clipboard;

        // Act
        var canCopy = viewModel.CanCopyAsSysml;
        await viewModel.CopyAsSysmlAsync();

        // Assert
        Assert.True(canCopy);
        Assert.NotNull(clipboard.LastCopiedText);
        Assert.Contains("view PredefinedView {", clipboard.LastCopiedText);
        Assert.Contains("expose Sample::Engine;", clipboard.LastCopiedText);
    }

    /// <summary>
    ///     Validates that a diagram document view model for a tab with no derivable definition (a brand-new,
    ///     unrendered custom-preview tab) reports <see cref="DiagramDocumentViewModel.CanCopyAsSysml" /> as
    ///     <see langword="false" /> and leaves the clipboard untouched.
    /// </summary>
    [Fact]
    public async Task CopyAsSysmlAsync_NonExportableTab_LeavesClipboardUntouched()
    {
        // Arrange
        using var shell = await CreateShellWithSampleWorkspaceAsync();
        var tab = shell.OpenNewCustomPreviewTab();

        var viewModel = new DiagramDocumentViewModel(shell, tab.Id);
        var clipboard = new FakeClipboardService();
        viewModel.ClipboardService = clipboard;

        // Act
        var canCopy = viewModel.CanCopyAsSysml;
        await viewModel.CopyAsSysmlAsync();

        // Assert
        Assert.False(canCopy);
        Assert.Null(clipboard.LastCopiedText);
    }

    /// <summary>
    ///     Validates that copying as SysML is a safe no-op - not an exception - when no
    ///     <see cref="IClipboardService" /> has been assigned yet.
    /// </summary>
    [Fact]
    public async Task CopyAsSysmlAsync_NoClipboardServiceAssigned_DoesNotThrow()
    {
        // Arrange
        using var shell = await CreateShellWithSampleWorkspaceAsync();
        var view = shell.ViewCatalog.AvailableViews[0];
        shell.SelectPredefinedView(view.QualifiedName);
        var tabId = shell.ActiveTabId!;

        var viewModel = new DiagramDocumentViewModel(shell, tabId);

        // Act / Assert
        await viewModel.CopyAsSysmlAsync();
    }
}
