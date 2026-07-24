using OpenQA.Selenium.Appium;

namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Real Appium tests validating the compiled <c>DemaConsulting.SysML2Workbench.Desktop</c> application's
///     main window launches correctly, through a dedicated per-test <see cref="AppFixture" /> session (see
///     <see cref="AppiumTestBase" />). Marked <c>[Trait("Category", "Integration")]</c> so
///     <c>.github/workflows/build.yaml</c>'s cross-platform <c>build</c> job (which has no Appium server
///     running) can exclude this tier via <c>--filter "Category!=Integration"</c>, while the dedicated
///     <c>appium-windows-integration-tests</c> job (and a developer running <c>build.ps1 -IntegrationTest</c>
///     locally on any OS) runs it for real.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MainWindowLaunchIntegrationTests : AppiumTestBase
{
    /// <summary>
    ///     Validates that launching the real Desktop application presents its main window titled
    ///     "SysML2Workbench", proving the Appium session is actually driving the compiled application rather
    ///     than a stub, and that the window title <c>test/DemaConsulting.SysML2Workbench.UiTests</c>'s
    ///     <c>MainWindowView_Startup_HasSysML2WorkbenchTitle</c> test asserts in-process also holds true when the
    ///     real windowed application is launched end-to-end.
    /// </summary>
    [Fact]
    public void DesktopApp_Launch_ShowsMainWindowWithExpectedTitle()
    {
        // Arrange / Act
        var title = Session.Title;

        // Assert
        Assert.Equal("SysML2Workbench", title);
    }
}
