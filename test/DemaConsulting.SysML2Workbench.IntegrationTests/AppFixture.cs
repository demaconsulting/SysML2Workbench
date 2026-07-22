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
///     <para>
///         Only the Windows branch (Appium's NovaWindows driver, since WinAppDriver is deprecated) is wired into
///         CI today (<c>.github/workflows/build.yaml</c>'s <c>appium-windows-integration-tests</c> job) and
///         exercised by real <c>[Fact]</c>-decorated tests in this assembly. Avalonia 12.1.0's
///         <c>AutomationPeer</c> also exposes controls to macOS's NSAccessibility (via Appium's Mac2 driver) and
///         Linux's AT-SPI2 (via the X11 backend only; Wayland support is unconfirmed), so the branches below are
///         structurally present and correct, but neither has a provisioned CI runner/driver install today - see
///         <c>.agent-logs/planning-appium-integration-tests-7f3a1c2e.md</c> for the scope decision. Enabling them
///         later only requires provisioning the respective driver/session in CI and removing the
///         <see cref="NotSupportedException" /> below; no further changes to this class's shape are expected.
///     </para>
/// </remarks>
public sealed class AppFixture : IDisposable
{
    private const string DefaultWindowsAppPath = "publish/win-x64/DemaConsulting.SysML2Workbench.Desktop.exe";

    /// <summary>
    ///     Creates the fixture, launching the compiled Desktop application through a local Appium server
    ///     (<c>http://127.0.0.1:4723</c>) using the driver appropriate for the current operating system.
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
    ///     Resolves the path to the published Desktop executable used by the Windows/NovaWindows session,
    ///     preferring the <c>SYSML2WORKBENCH_APP_PATH</c> environment variable (set by the CI job that publishes
    ///     the app immediately before running these tests) and falling back to the conventional local
    ///     <c>publish/win-x64</c> output used elsewhere in this repository's build tooling.
    /// </summary>
    private static string ResolveWindowsAppPath()
    {
        var configured = Environment.GetEnvironmentVariable("SYSML2WORKBENCH_APP_PATH");
        return string.IsNullOrWhiteSpace(configured) ? DefaultWindowsAppPath : configured;
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
            App = Path.GetFullPath(ResolveWindowsAppPath()),
        };

        return new WindowsDriver(new Uri("http://127.0.0.1:4723"), options);
    }

    /// <summary>
    ///     Structurally correct macOS session builder using Appium's Mac2 driver against Avalonia's
    ///     NSAccessibility automation peer support, matching the OS-branching shape the Windows session above
    ///     uses. Not yet exercised by CI (no macOS Appium runner is provisioned), so it throws rather than
    ///     attempting a session that would only fail with a connection error in this environment; a future change
    ///     provisioning macOS CI only needs to replace this throw with the equivalent
    ///     <see cref="MacDriver" /> construction shown in the comment below.
    /// </summary>
    private static AppiumDriver CreateMacDriver()
    {
        // Future macOS CI enablement:
        //   var options = new AppiumOptions { AutomationName = "Mac2", PlatformName = "Mac" };
        //   options.AddAdditionalAppiumOption("bundleId", "com.demaconsulting.sysml2workbench");
        //   return new MacDriver(new Uri("http://127.0.0.1:4723"), options);
        throw new NotSupportedException(
            "macOS Appium/Mac2 sessions are not yet provisioned in CI; see AppFixture's remarks and "
            + ".agent-logs/planning-appium-integration-tests-7f3a1c2e.md for the scope decision.");
    }

    /// <summary>
    ///     Structurally correct Linux session builder targeting Avalonia's AT-SPI2 automation peer support
    ///     (available only through Avalonia's X11 backend today; Wayland support is unconfirmed), driven through
    ///     Appium's generic <c>selenium-webdriver-at-spi</c> driver rather than a dedicated .NET client type. Not
    ///     yet exercised by CI (no Linux Appium/AT-SPI runner is provisioned), so it throws for the same reason as
    ///     <see cref="CreateMacDriver" />.
    /// </summary>
    private static AppiumDriver CreateLinuxDriver()
    {
        // Future Linux CI enablement (X11 only; Wayland is unconfirmed):
        //   var options = new AppiumOptions { AutomationName = "atspi", PlatformName = "Linux" };
        //   options.AddAdditionalAppiumOption("appExecutable", ResolveWindowsAppPath());
        //   return new AppiumDriver(new Uri("http://127.0.0.1:4723"), options);
        throw new NotSupportedException(
            "Linux Appium/AT-SPI2 sessions are not yet provisioned in CI; see AppFixture's remarks and "
            + ".agent-logs/planning-appium-integration-tests-7f3a1c2e.md for the scope decision.");
    }
}
