using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Appium.Windows;

namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Launches the real, compiled <c>DemaConsulting.SysML2Workbench.Desktop</c> application under a local
///     Appium server and exposes the resulting WebDriver session to the tests in this assembly, following the
///     OS-branching <c>AppFixture</c> pattern documented at
///     https://docs.avaloniaui.net/docs/testing/ui-testing-with-appium.
/// </summary>
/// <remarks>
///     This fixture never starts, stops, or otherwise manages an Appium/AT-SPI server process itself - it
///     always just connects a WebDriver client to <c>http://127.0.0.1:4723</c>, assuming one is already
///     listening there. That server's lifecycle is owned entirely by <c>run-under-appium.ps1</c> (invoked by
///     <c>build.ps1 -IntegrationTest</c>), which wraps the whole <c>dotnet test</c> invocation on every OS:
///     <list type="bullet">
///         <item>
///             <description>
///                 Windows/macOS: starts a local Appium server (NovaWindows/Mac2 driver), runs the wrapped
///                 command, then stops the server.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Linux: delegates the entire wrapped command to KDE's <c>selenium-webdriver-at-spi-run</c>
///                 (https://community.kde.org/Selenium), which boots a nested Wayland session plus its own
///                 AT-SPI2-backed WebDriver server, runs the wrapped command as its child, and tears everything
///                 down together. There is no standalone, directly-startable server binary for this backend -
///                 the <c>-run</c> wrapper is the only supported entry point - so this fixture simply trusts
///                 that it is already running inside that wrapper's session by the time the test process starts.
///             </description>
///         </item>
///     </list>
///     Only the Windows branch (Appium's NovaWindows driver, since WinAppDriver is deprecated) is exercised by
///     CI today (<c>.github/workflows/build.yaml</c>'s <c>appium-windows-integration-tests</c> job) and has been
///     validated against a real Appium server. The macOS branch (Mac2 driver, targeting Avalonia's
///     NSAccessibility automation peer support) and the Linux branch (targeting Avalonia's AT-SPI2 automation
///     peer support) are implemented from documentation so a developer can run this tier locally, but neither
///     has a provisioned CI runner nor has been exercised against real hardware - treat both as best-effort and
///     unvalidated until proven otherwise.
/// </remarks>
public sealed class AppFixture : IDisposable
{
    private const string DefaultWindowsAppPath = "publish/win-x64/DemaConsulting.SysML2Workbench.Desktop.exe";
    private const string DefaultMacOsX64AppPath = "publish/osx-x64/DemaConsulting.SysML2Workbench.Desktop";
    private const string DefaultMacOsArm64AppPath = "publish/osx-arm64/DemaConsulting.SysML2Workbench.Desktop";
    private const string DefaultLinuxAppPath = "publish/linux-x64/DemaConsulting.SysML2Workbench.Desktop";

    private static readonly Uri ServerUri = new("http://127.0.0.1:4723");

    /// <summary>
    ///     Creates the fixture, connecting to the Appium/AT-SPI server that <c>run-under-appium.ps1</c> already
    ///     started before this test process launched, and using the driver appropriate for the current
    ///     operating system.
    /// </summary>
    public AppFixture()
    {
        Driver = OperatingSystem.IsWindows()
            ? CreateWindowsDriver()
            : OperatingSystem.IsMacOS()
                ? CreateMacDriver()
                : OperatingSystem.IsLinux()
                    ? CreateLinuxDriver()
                    : throw new PlatformNotSupportedException(
                        "SysML2Workbench's Appium AppFixture only recognizes Windows, macOS, and Linux.");
    }

    /// <summary>
    ///     The live Appium/WebDriver session driving the launched Desktop application.
    /// </summary>
    public AppiumDriver Driver { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }

    /// <summary>
    ///     Resolves the path to the published Desktop executable/binary for the current operating system,
    ///     preferring the <c>SYSML2WORKBENCH_APP_PATH</c> environment variable (set by <c>build.ps1</c>/CI
    ///     immediately after publishing the app) and falling back to the conventional
    ///     <c>publish/&lt;rid&gt;</c> output used elsewhere in this repository's build tooling.
    /// </summary>
    private static string ResolveAppPath()
    {
        var configured = Environment.GetEnvironmentVariable("SYSML2WORKBENCH_APP_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (OperatingSystem.IsWindows())
        {
            return DefaultWindowsAppPath;
        }

        if (OperatingSystem.IsMacOS())
        {
            return RuntimeInformation.OSArchitecture == Architecture.Arm64
                ? DefaultMacOsArm64AppPath
                : DefaultMacOsX64AppPath;
        }

        return DefaultLinuxAppPath;
    }

    /// <summary>
    ///     Creates the fully implemented, CI-validated Windows session: Appium's NovaWindows driver (the
    ///     maintained successor to the deprecated WinAppDriver) pointed at the published Desktop executable.
    /// </summary>
    private static WindowsDriver CreateWindowsDriver()
    {
        var options = new AppiumOptions
        {
            AutomationName = "NovaWindows",
            PlatformName = "Windows",
            App = Path.GetFullPath(ResolveAppPath()),
        };

        return new WindowsDriver(ServerUri, options);
    }

    /// <summary>
    ///     Best-effort macOS session using Appium's Mac2 driver against Avalonia's NSAccessibility automation
    ///     peer support. This repository does not currently produce a signed <c>.app</c> bundle for macOS (only
    ///     a raw <c>dotnet publish</c> binary), so this points Mac2's <c>app</c> capability directly at that
    ///     published binary rather than a bundle path/bundle id; if Mac2 turns out to require a real bundle to
    ///     launch a non-bundled executable, this will need revisiting once someone can validate it on real
    ///     macOS hardware. Not exercised by CI and not validated against a real macOS machine - see
    ///     <see cref="AppFixture" />'s remarks.
    /// </summary>
    private static MacDriver CreateMacDriver()
    {
        var options = new AppiumOptions
        {
            AutomationName = "Mac2",
            PlatformName = "Mac",
            App = Path.GetFullPath(ResolveAppPath()),
        };

        return new MacDriver(ServerUri, options);
    }

    /// <summary>
    ///     Best-effort Linux session targeting KDE's <c>selenium-webdriver-at-spi</c>
    ///     (https://community.kde.org/Selenium), which drives Avalonia's AT-SPI2 automation peer support (X11
    ///     backend only today; Wayland is unconfirmed). This server has no <c>appium driver install</c>-able
    ///     plugin form and no standalone directly-startable binary - it only runs via its
    ///     <c>selenium-webdriver-at-spi-run</c> wrapper, which <c>run-under-appium.ps1</c> is expected to have
    ///     already launched (wrapping this entire test process) before this fixture is constructed. Per its
    ///     documented "command line" app-launch mode, the <c>app</c> capability is set directly to the published
    ///     binary's path. Not exercised by CI and not validated against a real Linux machine - see
    ///     <see cref="AppFixture" />'s remarks.
    /// </summary>
    private static AppiumDriver CreateLinuxDriver()
    {
        var options = new AppiumOptions
        {
            PlatformName = "Linux",
            App = Path.GetFullPath(ResolveAppPath()),
        };

        return new LinuxDriver(ServerUri, options);
    }

    /// <summary>
    ///     <see cref="AppiumDriver" /> is abstract with no generic/platform-agnostic concrete form in this
    ///     package version (Appium.WebDriver 8.x only ships concrete subclasses for its officially-known
    ///     platforms - Windows, Mac, Android, iOS), and its <c>(Uri, ICapabilities)</c> convenience constructor
    ///     is not accessible to subclasses outside its own assembly. This trivial subclass exists solely so the
    ///     AT-SPI2/Linux session (which has no first-party Appium .NET client type) can still be instantiated
    ///     and driven through the same base <see cref="AppiumDriver" /> API surface as the other platforms, by
    ///     going through the accessible <c>(ICommandExecutor, ICapabilities)</c> constructor directly with a
    ///     plain Selenium <see cref="OpenQA.Selenium.Remote.HttpCommandExecutor" />.
    /// </summary>
    private sealed class LinuxDriver : AppiumDriver
    {
        public LinuxDriver(Uri remoteAddress, AppiumOptions options)
            : base(new OpenQA.Selenium.Remote.HttpCommandExecutor(remoteAddress, TimeSpan.FromSeconds(60)), options.ToCapabilities())
        {
        }
    }
}
