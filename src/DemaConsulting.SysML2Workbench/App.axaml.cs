using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench;

/// <summary>
///     Avalonia application entry point. Composes the concrete subsystem units into one
///     <see cref="MainWindowShell" /> and hosts it in the <see cref="MainWindowView" />.
/// </summary>
/// <remarks>
///     This class is deliberately a thin composition root: all orchestration logic lives in the fully
///     unit-tested <see cref="MainWindowShell" />, and this class's only responsibility is wiring real
///     dependencies (an Avalonia UI dispatcher, a local log folder, a live debounce window) that only make
///     sense inside a running Avalonia application.
/// </remarks>
public sealed class App : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SysML2Workbench",
                "logs");

            var shell = new MainWindowShell(
                new WorkspaceModel(),
                new FileWatcher(TimeSpan.FromMilliseconds(500), dispatcher: new AvaloniaUiDispatcher()),
                new DiagnosticsAggregator(),
                new ViewCatalogPresenter(),
                new LayoutInvoker(),
                new DiagnosticsListView(),
                new SysmlSnippetGenerator(),
                new RollingFileLogger(logDirectory));

            desktop.MainWindow = new MainWindowView(shell);
            desktop.ShutdownRequested += (_, _) => shell.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
