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
///     Local view/view-model interaction tests for <see cref="MainWindowView" />, running under Avalonia's
///     headless test platform (<see cref="AvaloniaFactAttribute" />). Unlike
///     <c>test/OtsSoftwareTests/AvaloniaTests.cs</c>, which qualifies the OTS Avalonia dependency itself against
///     its own requirements, these tests exercise this repository's own menu-command wiring and window
///     composition logic in-process, with no real window/Appium session involved.
/// </summary>
public sealed class MainWindowShellUiTests : IDisposable
{
    private readonly string _tempLogRoot = Directory.CreateTempSubdirectory("sysml2workbench-ui-tests-logs-").FullName;

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
    ///     Validates that the main window is constructed with the application's fixed title, since Appium's
    ///     Windows-path tests in <c>test/DemaConsulting.SysML2Workbench.IntegrationTests</c> rely on this exact
    ///     title to find and assert against the real launched window.
    /// </summary>
    [AvaloniaFact]
    public void MainWindowView_Startup_HasSysML2WorkbenchTitle()
    {
        // Arrange
        using var shell = CreateShell();

        // Act
        var window = new MainWindowView(shell);

        // Assert
        Assert.Equal("SysML2Workbench", window.Title);
    }

    /// <summary>
    ///     Validates that clicking the File menu's "Exit" item closes the main window, proving the click handler
    ///     wired in <c>MainWindowView.axaml.cs</c> (<c>OnExitMenuItemClick</c>) actually invokes
    ///     <see cref="Window.Close()" /> rather than merely being present in the XAML.
    /// </summary>
    [AvaloniaFact]
    public void MainWindowView_ExitMenuItem_Click_ClosesWindow()
    {
        // Arrange
        using var shell = CreateShell();
        var window = new MainWindowView(shell);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var exitMenuItem = window.FindControl<MenuItem>("ExitMenuItem");
        Assert.NotNull(exitMenuItem);

        var closed = false;
        window.Closed += (_, _) => closed = true;

        // Act
        exitMenuItem.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        // Assert
        Assert.True(closed);
    }
}
