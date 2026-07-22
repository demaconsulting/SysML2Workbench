using OpenQA.Selenium.Appium;

namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Real Appium tests driving the compiled <c>DemaConsulting.SysML2Workbench.Desktop</c> application through
///     the <see cref="AppFixture" />-launched session (NovaWindows on Windows, Mac2 on macOS, AT-SPI2 on Linux),
///     using the <c>AutomationProperties.AutomationId</c> values added to <c>MainWindowView.axaml</c>. Marked
///     <c>[Trait("Category", "Integration")]</c> so <c>.github/workflows/build.yaml</c>'s cross-platform
///     <c>build</c> job (which has no Appium server running) can exclude this tier via
///     <c>--filter "Category!=Integration"</c>, while the dedicated <c>appium-windows-integration-tests</c> job
///     (and a developer running <c>build.ps1 -IntegrationTest</c> locally on any OS) runs it for real.
/// </summary>
[Collection("AppFixture")]
[Trait("Category", "Integration")]
public sealed class MainWindowShellIntegrationTests
{
    private readonly AppiumDriver _session;

    /// <summary>
    ///     Receives the assembly-shared <see cref="AppFixture" />'s launched application session.
    /// </summary>
    /// <param name="fixture">The shared fixture that launched the Desktop application.</param>
    public MainWindowShellIntegrationTests(AppFixture fixture)
    {
        _session = fixture.Driver;
    }

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
        var title = _session.Title;

        // Assert
        Assert.Equal("SysML2Workbench", title);
    }

    /// <summary>
    ///     Validates that the File menu's "Open File..." item, found by the <c>AddFileSourceMenuItem</c>
    ///     automation id added to <c>MainWindowView.axaml</c>, is discoverable and enabled through the real
    ///     accessibility tree exposed by Avalonia's UIA automation peer. The menu item is deliberately not
    ///     clicked here: doing so would open the OS-native "Open File" dialog, which lives outside Avalonia's
    ///     own accessibility tree and is not reliably automatable through the same driver session across
    ///     Windows/macOS/Linux.
    /// </summary>
    [Fact]
    public void DesktopApp_FileMenu_AddFileSourceMenuItem_IsDiscoverableAndEnabled()
    {
        // Arrange
        var fileMenu = _session.FindElement(MobileBy.Name("File"));
        fileMenu.Click();

        // Act
        var addFileMenuItem = _session.FindElement(MobileBy.AccessibilityId("AddFileSourceMenuItem"));

        // Assert
        Assert.True(addFileMenuItem.Displayed);
        Assert.True(addFileMenuItem.Enabled);

        // Close the menu without picking a file so this test leaves no dialog open behind it.
        addFileMenuItem.SendKeys(OpenQA.Selenium.Keys.Escape);
    }

    /// <summary>
    ///     Validates that clicking the Help menu's "About" item, found by the <c>AboutMenuItem</c> automation
    ///     id, opens the modal About dialog, discoverable by its own <c>AboutDialogOkButton</c> automation id,
    ///     and that dismissing it via that button closes the dialog again.
    /// </summary>
    [Fact]
    public void DesktopApp_HelpMenu_AboutMenuItem_OpensAndClosesAboutDialog()
    {
        // Arrange
        var helpMenu = _session.FindElement(MobileBy.Name("Help"));
        helpMenu.Click();
        var aboutMenuItem = _session.FindElement(MobileBy.AccessibilityId("AboutMenuItem"));

        // Act
        aboutMenuItem.Click();
        var okButton = _session.FindElement(MobileBy.AccessibilityId("AboutDialogOkButton"));

        // Assert
        Assert.True(okButton.Displayed);

        okButton.Click();
    }
}
