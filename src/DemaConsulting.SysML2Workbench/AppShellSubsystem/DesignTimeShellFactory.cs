using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Builds a throwaway <see cref="MainWindowShell" /> used only by <see cref="MainWindowView" />'s
///     parameterless constructor, which the Avalonia XAML previewer/designer requires but which is never
///     invoked at application runtime (the real composition root is <see cref="App" />).
/// </summary>
internal static class DesignTimeShellFactory
{
    /// <summary>
    ///     Creates a shell wired with real subsystem units pointed at a temporary log folder, suitable only for
    ///     design-time preview rendering.
    /// </summary>
    /// <returns>A usable, but not application-composed, shell instance.</returns>
    public static MainWindowShell Create()
    {
        return new MainWindowShell(
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(500)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new SvgCanvasHost(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(Path.Combine(Path.GetTempPath(), "SysML2Workbench-DesignTime")));
    }
}
