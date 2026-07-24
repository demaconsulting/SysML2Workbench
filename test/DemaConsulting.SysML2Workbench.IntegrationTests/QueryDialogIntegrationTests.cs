using OpenQA.Selenium.Appium;

namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Real Appium tests validating the Query menu's "Run Query..." item and the modal Query dialog it opens,
///     through a dedicated per-test <see cref="AppFixture" /> session (see <see cref="AppiumTestBase" />).
///     Marked <c>[Trait("Category", "Integration")]</c> so <c>.github/workflows/build.yaml</c>'s cross-platform
///     <c>build</c> job (which has no Appium server running) can exclude this tier via
///     <c>--filter "Category!=Integration"</c>, while the dedicated <c>appium-windows-integration-tests</c> job
///     (and a developer running <c>build.ps1 -IntegrationTest</c> locally on any OS) runs it for real.
/// </summary>
[Trait("Category", "Integration")]
public sealed class QueryDialogIntegrationTests : AppiumTestBase
{
    /// <summary>
    ///     Absolute, cwd-independent path to the small fixture file (a single <c>attribute</c> feature usage)
    ///     copied alongside the test binaries by this project's <c>TestData\**</c> <c>CopyToOutputDirectory</c>
    ///     item, used as a <c>--startup-source</c> argument to preload a real workspace without driving the
    ///     unautomatable native OS "Open File" dialog.
    /// </summary>
    private static readonly string InspectionSampleFilePath =
        Path.Combine(AppContext.BaseDirectory, "TestData", "InspectionSample.sysml");

    /// <summary>
    ///     Validates that the Query menu's "Run Query..." item, found by the <c>QueryDialogMenuItem</c>
    ///     automation id, is discoverable and enabled. Not clicked here (see
    ///     <see cref="DesktopApp_QueryMenu_QueryDialogMenuItem_OpensAndClosesQueryDialog" /> for the full
    ///     open/close round trip through the real dialog).
    /// </summary>
    [Fact]
    public void DesktopApp_QueryMenu_QueryDialogMenuItem_IsDiscoverableAndEnabled()
    {
        AssertMenuItemIsDiscoverableAndEnabled("Query", "QueryDialogMenuItem");
    }

    /// <summary>
    ///     Validates that clicking the Query menu's "Run Query..." item, found by the
    ///     <c>QueryDialogMenuItem</c> automation id, opens the modal Query dialog, discoverable by its own
    ///     <c>AddTypeFilterButton</c> automation id (part of the shared <c>ElementFilterView</c>), and that
    ///     dismissing it via its <c>QueryDialogCloseButton</c> actually closes the dialog again (confirmed by
    ///     waiting for <c>AddTypeFilterButton</c> to disappear from the accessibility tree) - the same
    ///     open/click/assert/close round-trip shape as
    ///     <see cref="HelpMenuIntegrationTests.DesktopApp_HelpMenu_AboutMenuItem_OpensAndClosesAboutDialog" />.
    ///     Also captures a cropped PNG of the "+" add-type-filter button under whatever theme the live session
    ///     is running under, saved to <c>artifacts/inspection/</c> for later manual or agent-driven visual
    ///     review, folded into this same round trip (rather than a separate open/close cycle) since the
    ///     screenshot is just an incidental capture of a control this test already has open and asserted on.
    ///     This is not itself a correctness assertion for the screenshot: automatically detecting styling
    ///     defects such as low-contrast text is an LLM-visual-inspection problem, not a pass/fail CI check, so
    ///     capturing it here only proves the capture mechanism works end-to-end and leaves the actual review to
    ///     a human or an agent looking at the saved image. Set the <c>SYSML2WORKBENCH_THEME</c> environment
    ///     variable to <c>Dark</c> or <c>Light</c> before launching the session (for example, before running
    ///     <c>build.ps1 -IntegrationTest</c>) to control which theme this capture reflects - the whole Appium
    ///     session's app process inherits one theme for its entire lifetime, since none of NovaWindows/Mac2/
    ///     AT-SPI2 support per-test environment injection into an already-launched app. This captures only the
    ///     "+" button, not an active filter chip: the chip row's actual color-contrast complaint (a chip's
    ///     light-grey background in <c>ElementFilterView.axaml</c>) needs at least one active filter, which
    ///     requires a loaded workspace - see
    ///     <see cref="DesktopApp_QueryDialog_PopulatedWithSourceAndChip_CapturesInspectionScreenshot" /> for
    ///     that.
    /// </summary>
    [Fact]
    public void DesktopApp_QueryMenu_QueryDialogMenuItem_OpensAndClosesQueryDialog()
    {
        // Arrange
        var queryMenu = Session.FindElement(MobileBy.Name("Query"));
        queryMenu.Click();
        var queryDialogMenuItem = Session.FindElement(MobileBy.AccessibilityId("QueryDialogMenuItem"));
        queryDialogMenuItem.Click();

        try
        {
            // Act
            var addTypeFilterButton = Session.FindElement(MobileBy.AccessibilityId("AddTypeFilterButton"));
            InspectionScreenshot.CaptureElement(Session, addTypeFilterButton, "query-dialog-add-type-filter-button");

            // Assert
            Assert.True(addTypeFilterButton.Displayed);

            Session.FindElement(MobileBy.AccessibilityId("QueryDialogCloseButton")).Click();

            var closed = WaitUntil(() => Session.FindElements(MobileBy.AccessibilityId("AddTypeFilterButton")).Count == 0);
            Assert.True(closed, "The Query dialog's 'AddTypeFilterButton' was still present after closing it via 'QueryDialogCloseButton'.");
        }
        catch
        {
            TryCloseAnyOpenDialogWithEscape();
            throw;
        }
    }

    /// <summary>
    ///     Captures a larger PNG of the whole Query dialog's type-filter row (<c>ElementFilterRoot</c>, the
    ///     shared <c>ElementFilterView</c>'s chip row plus its "+" add-chip button and search box) with a real,
    ///     populated workspace and an actual "attribute" filter chip added, proving the already-applied
    ///     <c>ElementFilterView.axaml</c> chip-foreground contrast fix (<c>Foreground="#212121"</c> on the chip
    ///     text and its "X" remove button) is legible under whatever theme the live session is running under.
    /// </summary>
    /// <remarks>
    ///     The workspace is populated via <see cref="AppiumTestBase.StartApp" />'s <c>--startup-source</c>
    ///     argument, preloading <see cref="InspectionSampleFilePath" /> - a small fixture containing a single
    ///     <c>attribute</c> feature usage - rather than driving the unautomatable native OS "Open File" dialog.
    ///     Like <see cref="DesktopApp_QueryMenu_QueryDialogMenuItem_OpensAndClosesQueryDialog" />, set
    ///     <c>SYSML2WORKBENCH_THEME</c> to <c>Dark</c>/<c>Light</c> before launching the session to control which
    ///     theme this single capture reflects. No shared-session cleanup is needed here (unlike before this
    ///     class adopted <see cref="AppiumTestBase" />'s per-test launch): this test's own dedicated application
    ///     instance is quit at the end of the test regardless of outcome, so nothing it preloads or mutates can
    ///     ever leak into another test.
    /// </remarks>
    [Fact]
    public void DesktopApp_QueryDialog_PopulatedWithSourceAndChip_CapturesInspectionScreenshot()
    {
        // Arrange - relaunch with the workspace preloaded via --startup-source, then open the Query dialog.
        StartApp($"--startup-source \"{InspectionSampleFilePath}\"");
        var queryMenu = Session.FindElement(MobileBy.Name("Query"));
        queryMenu.Click();
        var queryDialogMenuItem = Session.FindElement(MobileBy.AccessibilityId("QueryDialogMenuItem"));
        queryDialogMenuItem.Click();

        try
        {
            // Act - add a real "attribute" type-filter chip via the "+" button's search-filtered flyout.
            var addTypeFilterButton = Session.FindElement(MobileBy.AccessibilityId("AddTypeFilterButton"));
            addTypeFilterButton.Click();

            // Type into the search box and press Enter rather than clicking a ListBoxItem directly: a
            // real pointer click into an open Flyout races NovaWindows' synthesized input against
            // Avalonia's light-dismiss overlay - confirmed by direct visual observation (the mouse
            // visibly hovers over the "attribute" entry, but the popup dismisses instead of the item
            // being selected), so the click "succeeds" from Appium's point of view yet no chip is ever
            // actually added. Typing into a TextBox and pressing Enter is one of the most reliable
            // interactions across every Appium/UIA driver, with no pointer hit-testing involved.
            var searchTextBox = Session.FindElement(MobileBy.AccessibilityId("AddTypeFilterSearchTextBox"));
            searchTextBox.SendKeys("attribute");
            searchTextBox.SendKeys(OpenQA.Selenium.Keys.Enter);

            var elementFilterRoot = Session.FindElement(MobileBy.AccessibilityId("ElementFilterRoot"));

            // The chip is added asynchronously (the key press returns to Appium before Avalonia's
            // dispatcher has re-rendered the bound ItemsControl), so poll briefly for the "attribute"
            // chip's own text element to actually appear before capturing - otherwise the screenshot
            // can race ahead of the render and show only the empty chip row. Scope the search to
            // elementFilterRoot's own subtree (rather than the whole session) so this cannot be
            // satisfied by a same-named element that isn't the chip: the search TextBox still holds the
            // typed "attribute" text, and the flyout's candidate ListBoxItem may still be visible for a
            // moment after Enter is pressed - both would otherwise produce a false-positive match.
            var chipRendered = WaitUntil(() => elementFilterRoot.FindElements(MobileBy.Name("attribute")).Count > 0);
            Assert.True(chipRendered, "The 'attribute' chip did not render in time for the screenshot.");

            InspectionScreenshot.CaptureElement(Session, elementFilterRoot, "query-dialog-populated-with-chip");

            // Assert
            Assert.True(elementFilterRoot.Displayed);

            Session.FindElement(MobileBy.AccessibilityId("QueryDialogCloseButton")).Click();
        }
        catch
        {
            TryCloseAnyOpenDialogWithEscape();
            throw;
        }
    }
}
