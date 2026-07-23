using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.UiTests.AppShellSubsystem;

/// <summary>
///     Local view/view-model interaction tests for <see cref="WorkspacePanelToolView" />, verifying that clicking
///     its buttons raises the expected <see cref="WorkspacePanelToolViewModel" /> events/commands, independent of
///     the Avalonia framework integration already qualified by <c>test/OtsSoftwareTests</c>.
/// </summary>
public sealed class WorkspacePanelUiTests : IDisposable
{
    private readonly string _tempLogRoot = Directory.CreateTempSubdirectory("sysml2workbench-ui-tests-workspace-logs-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempLogRoot))
        {
            Directory.Delete(_tempLogRoot, recursive: true);
        }
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
    ///     Validates that clicking the "Add File..." button raises <see cref="WorkspacePanelToolViewModel.RequestAddFile" />,
    ///     the seam the real window uses to open a storage-provider file picker, proving the button's
    ///     <c>Command</c> binding is wired to the view model's <c>AddFileCommand</c>.
    /// </summary>
    [AvaloniaFact]
    public void WorkspacePanelToolView_AddFileButton_Click_RaisesRequestAddFile()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);
        var view = new WorkspacePanelToolView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var raised = false;
        viewModel.RequestAddFile += (_, _) => raised = true;
        var addFileButton = view.FindControl<Button>("AddFileButton");
        Assert.NotNull(addFileButton);

        // Act
        addFileButton.Command?.Execute(null);

        // Assert
        Assert.True(raised);

        window.Close();
    }

    /// <summary>
    ///     Validates that a newly composed shell with no workspace sources presents an empty workspace tree,
    ///     showing the panel's empty-state text block instead of the tree view.
    /// </summary>
    [AvaloniaFact]
    public void WorkspacePanelToolView_Startup_EmptyWorkspace_ShowsEmptyState()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);

        // Act
        var view = new WorkspacePanelToolView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Assert
        Assert.True(viewModel.IsEmpty);
        var emptyStateTextBlock = view.FindControl<TextBlock>("EmptyStateTextBlock");
        Assert.NotNull(emptyStateTextBlock);
        Assert.True(emptyStateTextBlock.IsVisible);

        window.Close();
    }
}
