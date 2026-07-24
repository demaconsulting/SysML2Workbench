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

            ApplyStartupSourceArgumentsForTesting(shell, desktop.Args ?? []);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    ///     Preloads zero or more workspace file/folder sources from repeated <c>--startup-source &lt;path&gt;</c>
    ///     command-line arguments, so an Appium/AT-SPI integration-test session launched with per-session
    ///     <c>appArguments</c>/<c>arguments</c> capabilities (see <c>AppiumTestBase.StartApp</c>) can start with
    ///     a real, populated workspace tailored to that one test, without driving an unautomatable native OS
    ///     "Open File"/"Open Folder" dialog.
    /// </summary>
    /// <remarks>
    ///     Unlike a fixed environment variable (which would only be inherited once, for the whole lifetime of
    ///     the Appium/AT-SPI server process that launches this application - see
    ///     <see cref="ApplyThemeOverrideForTesting" />'s remarks for why <c>SYSML2WORKBENCH_THEME</c> uses that
    ///     approach instead), command-line arguments are supplied fresh with every individual WebDriver
    ///     session-creation call, so they can vary per test. Each occurrence of
    ///     <c>--startup-source</c> is followed by exactly one path; a path is added as a folder source if
    ///     <see cref="Directory.Exists(string)" /> returns true, otherwise as a file source. This is a test-only
    ///     hook with no effect on normal end-user usage, which never launches the application with these
    ///     arguments. Blocking synchronously here with <c>GetAwaiter().GetResult()</c> is safe: it is invoked
    ///     right after <paramref name="shell" /> and its <see cref="MainWindowView" /> are constructed, before
    ///     the Avalonia message loop starts pumping, and <see cref="MainWindowShell.AddFileSourceAsync" />'s/
    ///     <see cref="MainWindowShell.AddFolderSourceAsync" />'s awaited call chains (source-set add,
    ///     file-watcher registration, workspace load, snapshot apply,
    ///     <see cref="MainWindowShell.SourcesChanged" /> raised via a fire-and-forget dispatcher post) never
    ///     await a continuation that itself requires the not-yet-started message loop to be pumping.
    /// </remarks>
    /// <param name="shell">The freshly composed shell to preload sources into.</param>
    /// <param name="args">The raw command-line arguments the process was launched with.</param>
    private static void ApplyStartupSourceArgumentsForTesting(MainWindowShell shell, IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (args[i] != "--startup-source")
            {
                continue;
            }

            var path = args[i + 1];
            if (Directory.Exists(path))
            {
                shell.AddFolderSourceAsync(path).GetAwaiter().GetResult();
            }
            else if (File.Exists(path))
            {
                shell.AddFileSourceAsync(path).GetAwaiter().GetResult();
            }
        }
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
