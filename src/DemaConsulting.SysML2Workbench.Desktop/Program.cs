using Avalonia;
using DemaConsulting.SysML2Workbench;

namespace DemaConsulting.SysML2Workbench.Desktop;

/// <summary>
///     Desktop platform head. This is the thin, undocumented bootstrap unit called out in the architecture: it
///     only configures and starts the Avalonia classic desktop application lifetime, deferring all real
///     composition to <see cref="SysML2Workbench.App" />.
/// </summary>
internal static class Program
{
    /// <summary>
    ///     Process entry point.
    /// </summary>
    /// <param name="args">Command-line arguments forwarded to Avalonia.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    ///     Configures the Avalonia application, platform detection, font rendering, and logging.
    /// </summary>
    /// <returns>A configured <see cref="AppBuilder" /> ready to start.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
