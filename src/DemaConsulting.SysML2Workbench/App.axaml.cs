using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
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
        ApplyThemeOverrideForTesting();

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
                new RollingFileLogger(logDirectory),
                uiDispatcher: new AvaloniaUiDispatcher());

            desktop.MainWindow = new MainWindowView(shell);
            desktop.ShutdownRequested += (_, _) => shell.Dispose();

            ApplyStartupFileForTesting(shell);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    ///     Preloads a single workspace file source from the <c>SYSML2WORKBENCH_STARTUP_FILE</c> environment
    ///     variable, when set, so an Appium/AT-SPI integration-test session can start with a real, populated
    ///     workspace without driving an unautomatable native OS "Open File" dialog.
    /// </summary>
    /// <remarks>
    ///     This is a test-only hook mirroring <see cref="ApplyThemeOverrideForTesting" />'s precedent: it has no
    ///     effect on normal end-user usage of the application, which never has this variable set. It is invoked
    ///     right after <paramref name="shell" /> and its <see cref="MainWindowView" /> are constructed, before the
    ///     Avalonia message loop starts pumping. Blocking synchronously here with
    ///     <c>GetAwaiter().GetResult()</c> is safe: <see cref="MainWindowShell.AddFileSourceAsync" />'s awaited
    ///     call chain (source-set add, file-watcher registration, workspace load, snapshot apply,
    ///     <see cref="MainWindowShell.SourcesChanged" /> raised via a fire-and-forget dispatcher post) never
    ///     awaits a continuation that itself requires the not-yet-started message loop to be pumping.
    /// </remarks>
    /// <param name="shell">The freshly composed shell to preload a source into.</param>
    private static void ApplyStartupFileForTesting(MainWindowShell shell)
    {
        var startupFile = Environment.GetEnvironmentVariable("SYSML2WORKBENCH_STARTUP_FILE");
        if (string.IsNullOrWhiteSpace(startupFile))
        {
            return;
        }

        shell.AddFileSourceAsync(startupFile).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Overrides <c>RequestedThemeVariant</c> from the <c>SYSML2WORKBENCH_THEME</c> environment
    ///     variable ("Dark" or "Light", case-insensitive) when set, otherwise leaves the XAML-declared
    ///     "Default" (OS-following) variant untouched.
    /// </summary>
    /// <remarks>
    ///     This is a test-only hook: it exists so an Appium/AT-SPI integration-test session (which cannot
    ///     otherwise force a specific theme, since NovaWindows/Mac2/AT-SPI2 launch the published app as an
    ///     independent OS process with no per-test capability for injecting environment variables - only
    ///     whatever environment the Appium server itself inherited when <c>run-under-appium.ps1</c> started
    ///     it) can capture dark-mode inspection screenshots on demand, by setting this variable before running
    ///     <c>build.ps1 -IntegrationTest</c>. It has no effect on normal end-user usage of the application,
    ///     which always follows the OS theme unless this variable happens to be set in the user's environment.
    /// </remarks>
    private static void ApplyThemeOverrideForTesting()
    {
        var themeOverride = Environment.GetEnvironmentVariable("SYSML2WORKBENCH_THEME");
        if (Current is null)
        {
            return;
        }

        Current.RequestedThemeVariant = themeOverride?.ToUpperInvariant() switch
        {
            "DARK" => ThemeVariant.Dark,
            "LIGHT" => ThemeVariant.Light,
            _ => Current.RequestedThemeVariant,
        };
    }
}
