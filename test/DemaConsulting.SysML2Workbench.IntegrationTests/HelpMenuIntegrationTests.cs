using OpenQA.Selenium.Appium;

namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Real Appium tests validating the Help menu's items, through a dedicated per-test
///     <see cref="AppFixture" /> session (see <see cref="AppiumTestBase" />). Marked
///     <c>[Trait("Category", "Integration")]</c> so <c>.github/workflows/build.yaml</c>'s cross-platform
///     <c>build</c> job (which has no Appium server running) can exclude this tier via
///     <c>--filter "Category!=Integration"</c>, while the dedicated <c>appium-windows-integration-tests</c> job
///     (and a developer running <c>build.ps1 -IntegrationTest</c> locally on any OS) runs it for real.
/// </summary>
[Trait("Category", "Integration")]
public sealed class HelpMenuIntegrationTests : AppiumTestBase
{
    /// <summary>
    ///     Validates that clicking the Help menu's "About" item, found by the <c>AboutMenuItem</c> automation
    ///     id, opens the modal About dialog, discoverable by its own <c>AboutDialogOkButton</c> automation id,
    ///     and that dismissing it via that button actually closes the dialog again (confirmed by waiting for
    ///     <c>AboutDialogOkButton</c> to disappear from the accessibility tree).
    /// </summary>
    [Fact]
    public void DesktopApp_HelpMenu_AboutMenuItem_OpensAndClosesAboutDialog()
    {
        // Arrange
        var helpMenu = Session.FindElement(MobileBy.Name("Help"));
        helpMenu.Click();
        var aboutMenuItem = Session.FindElement(MobileBy.AccessibilityId("AboutMenuItem"));
        aboutMenuItem.Click();

        try
        {
            // Act
            var okButton = Session.FindElement(MobileBy.AccessibilityId("AboutDialogOkButton"));

            // Assert
            Assert.True(okButton.Displayed);

            okButton.Click();

            var closed = WaitUntil(() => Session.FindElements(MobileBy.AccessibilityId("AboutDialogOkButton")).Count == 0);
            Assert.True(closed, "The About dialog's 'AboutDialogOkButton' was still present after closing it via 'AboutDialogOkButton'.");
        }
        catch
        {
            TryCloseAnyOpenDialogWithEscape();
            throw;
        }
    }
}
