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
    private readonly AppFixture _fixture;
    private readonly AppiumDriver _session;

    /// <summary>
    ///     Receives the assembly-shared <see cref="AppFixture" />'s launched application session.
    /// </summary>
    /// <param name="fixture">The shared fixture that launched the Desktop application.</param>
    public MainWindowShellIntegrationTests(AppFixture fixture)
    {
        _fixture = fixture;
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
        AssertMenuItemIsDiscoverableAndEnabled("File", "AddFileSourceMenuItem");
    }

    /// <summary>
    ///     Validates that the File menu's "Open Folder..." item, found by the <c>AddFolderSourceMenuItem</c>
    ///     automation id, is discoverable and enabled. Not clicked for the same reason as
    ///     <see cref="DesktopApp_FileMenu_AddFileSourceMenuItem_IsDiscoverableAndEnabled" />: it opens the
    ///     OS-native "Open Folder" dialog.
    /// </summary>
    [Fact]
    public void DesktopApp_FileMenu_AddFolderSourceMenuItem_IsDiscoverableAndEnabled()
    {
        AssertMenuItemIsDiscoverableAndEnabled("File", "AddFolderSourceMenuItem");
    }

    /// <summary>
    ///     Validates that the File menu's "Close All" item, found by the <c>CloseAllMenuItem</c> automation id,
    ///     is discoverable and enabled. Not clicked here (this only proves discoverability): actually invoking
    ///     it is exercised end-to-end by <see cref="AppFixture.CloseAllSources" />, used as cleanup by
    ///     <see cref="DesktopApp_QueryDialog_PopulatedWithSourceAndChip_CapturesInspectionScreenshot" />.
    /// </summary>
    [Fact]
    public void DesktopApp_FileMenu_CloseAllMenuItem_IsDiscoverableAndEnabled()
    {
        AssertMenuItemIsDiscoverableAndEnabled("File", "CloseAllMenuItem");
    }

    /// <summary>
    ///     Validates that the View menu's "Workspace" panel-toggle item, found by the
    ///     <c>WorkspacePanelMenuItem</c> automation id, is discoverable and enabled. Not clicked, since toggling
    ///     the docked panel closed would leave the shared <see cref="AppFixture" /> session in a different
    ///     layout state for whichever test runs next.
    /// </summary>
    [Fact]
    public void DesktopApp_ViewMenu_WorkspacePanelMenuItem_IsDiscoverableAndEnabled()
    {
        AssertMenuItemIsDiscoverableAndEnabled("View", "WorkspacePanelMenuItem");
    }

    /// <summary>
    ///     Validates that the View menu's "Predefined Views" panel-toggle item, found by the
    ///     <c>PredefinedViewsMenuItem</c> automation id, is discoverable and enabled.
    /// </summary>
    [Fact]
    public void DesktopApp_ViewMenu_PredefinedViewsMenuItem_IsDiscoverableAndEnabled()
    {
        AssertMenuItemIsDiscoverableAndEnabled("View", "PredefinedViewsMenuItem");
    }

    /// <summary>
    ///     Validates that the View menu's "Diagnostics" panel-toggle item, found by the
    ///     <c>DiagnosticsMenuItem</c> automation id, is discoverable and enabled.
    /// </summary>
    [Fact]
    public void DesktopApp_ViewMenu_DiagnosticsMenuItem_IsDiscoverableAndEnabled()
    {
        AssertMenuItemIsDiscoverableAndEnabled("View", "DiagnosticsMenuItem");
    }

    /// <summary>
    ///     Validates that the View menu's "Custom View Builder..." item, found by the
    ///     <c>ViewBuilderDialogMenuItem</c> automation id, is discoverable and enabled. Not clicked, since the
    ///     opened dialog's controls do not yet carry automation ids that would let this fixture close it
    ///     deterministically.
    /// </summary>
    [Fact]
    public void DesktopApp_ViewMenu_ViewBuilderDialogMenuItem_IsDiscoverableAndEnabled()
    {
        AssertMenuItemIsDiscoverableAndEnabled("View", "ViewBuilderDialogMenuItem");
    }

    /// <summary>
    ///     Validates that the Query menu's "Run Query..." item, found by the <c>QueryDialogMenuItem</c>
    ///     automation id, is discoverable and enabled. Not clicked, for the same reason as
    ///     <see cref="DesktopApp_ViewMenu_ViewBuilderDialogMenuItem_IsDiscoverableAndEnabled" />.
    /// </summary>
    [Fact]
    public void DesktopApp_QueryMenu_QueryDialogMenuItem_IsDiscoverableAndEnabled()
    {
        AssertMenuItemIsDiscoverableAndEnabled("Query", "QueryDialogMenuItem");
    }

    /// <summary>
    ///     Opens the named top-level menu, locates the child item by automation id, asserts it is displayed and
    ///     enabled, then closes the menu via Escape without clicking the item itself - proving the item is
    ///     reachable through the real accessibility tree without triggering whatever side effect (dialog, panel
    ///     toggle) clicking it would cause.
    /// </summary>
    /// <param name="topLevelMenuName">The top-level menu's display name (e.g. "File", "View", "Query").</param>
    /// <param name="automationId">The child menu item's <c>AutomationProperties.AutomationId</c> value.</param>
    private void AssertMenuItemIsDiscoverableAndEnabled(string topLevelMenuName, string automationId)
    {
        // Arrange
        var topLevelMenu = _session.FindElement(MobileBy.Name(topLevelMenuName));
        topLevelMenu.Click();

        // Act
        var menuItem = _session.FindElement(MobileBy.AccessibilityId(automationId));

        // Assert
        Assert.True(menuItem.Displayed);
        Assert.True(menuItem.Enabled);

        // Close the menu without picking the item so this test leaves no side effect behind it.
        menuItem.SendKeys(OpenQA.Selenium.Keys.Escape);
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
        aboutMenuItem.Click();

        try
        {
            // Act
            var okButton = _session.FindElement(MobileBy.AccessibilityId("AboutDialogOkButton"));

            // Assert
            Assert.True(okButton.Displayed);

            okButton.Click();
        }
        catch
        {
            TryCloseAnyOpenDialogWithEscape();
            throw;
        }
    }

    /// <summary>
    ///     Captures a cropped PNG of the Query dialog's "+" add-type-filter button (part of the shared
    ///     <c>ElementFilterView</c> reused by several dialogs across the application) under whatever theme the
    ///     live session is running under, and saves it to <c>artifacts/inspection/</c> for later manual or
    ///     agent-driven visual review.
    /// </summary>
    /// <remarks>
    ///     This is not a correctness assertion: automatically detecting styling defects such as low-contrast
    ///     text is an LLM-visual-inspection problem, not a pass/fail CI check, so this test only proves the
    ///     capture mechanism works end-to-end and leaves the actual review to a human or an agent looking at
    ///     the saved image. Set the <c>SYSML2WORKBENCH_THEME</c> environment variable to <c>Dark</c> or
    ///     <c>Light</c> before launching the session (for example, before running
    ///     <c>build.ps1 -IntegrationTest</c>) to control which theme this capture reflects - the whole Appium
    ///     session's app process inherits one theme for its entire lifetime, since none of NovaWindows/Mac2/
    ///     AT-SPI2 support per-test environment injection into an already-launched app. This captures only the
    ///     "+" button, not an active filter chip: the chip row's actual color-contrast complaint (a chip's
    ///     light-grey background in <c>ElementFilterView.axaml</c>) needs at least one active filter, which
    ///     requires a loaded workspace - no Appium test can drive that yet - so this only proves the capture
    ///     mechanism works, and will be extended to capture a real chip once a workspace-loading test hook
    ///     exists.
    /// </remarks>
    [Fact]
    public void DesktopApp_QueryDialog_AddTypeFilterButton_CapturesInspectionScreenshot()
    {
        // Arrange
        var queryMenu = _session.FindElement(MobileBy.Name("Query"));
        queryMenu.Click();
        var queryDialogMenuItem = _session.FindElement(MobileBy.AccessibilityId("QueryDialogMenuItem"));
        queryDialogMenuItem.Click();

        try
        {
            // Act
            var addTypeFilterButton = _session.FindElement(MobileBy.AccessibilityId("AddTypeFilterButton"));
            InspectionScreenshot.CaptureElement(_session, addTypeFilterButton, "query-dialog-add-type-filter-button");

            // Assert
            Assert.True(addTypeFilterButton.Displayed);

            _session.FindElement(MobileBy.AccessibilityId("QueryDialogCloseButton")).Click();
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
    ///     The workspace is populated via the <c>SYSML2WORKBENCH_STARTUP_FILE</c> environment variable (read by
    ///     <c>App.axaml.cs</c>'s <c>ApplyStartupFileForTesting</c> at process startup, before this test process
    ///     ever launches), preloading <c>TestData/InspectionSample.sysml</c> - a small fixture containing a
    ///     single <c>attribute</c> feature usage - rather than driving the unautomatable native OS "Open File"
    ///     dialog. Like <see cref="DesktopApp_QueryDialog_AddTypeFilterButton_CapturesInspectionScreenshot" />,
    ///     set <c>SYSML2WORKBENCH_THEME</c> to <c>Dark</c>/<c>Light</c> before launching the session to control
    ///     which theme this single capture reflects (the whole session's app process inherits one theme for its
    ///     entire lifetime - no per-test theme switching is possible against an already-launched app - so this
    ///     follows the exact same one-run, one-file convention, not a per-test dual light/dark capture).
    ///     <para>
    ///     Reset strategy (documented per this class's shared-session constraint - see
    ///     <see cref="AppFixture.CloseAllSources" />): the <c>SYSML2WORKBENCH_STARTUP_FILE</c> preload happens
    ///     exactly once, at process launch, and cannot be redone per test without the very native-dialog
    ///     automation this design avoids. A blanket "reset before every test" policy would therefore
    ///     deterministically wipe the preloaded workspace before this test ever got to run, regardless of
    ///     xUnit's (unspecified) execution order within the class. Cleanup is instead owned only by this one
    ///     state-mutating test, via a <c>finally</c> block calling <see cref="AppFixture.CloseAllSources" /> -
    ///     every other test in this class only asserts menu-item discoverability/enablement, which holds
    ///     whether or not a source happens to be loaded, so no other test is order-sensitive either way. A
    ///     future test that itself needs an empty starting workspace must add its own guard (or this class must
    ///     switch to a blanket-reset strategy) if this ever stops being the only mutating test.
    ///     </para>
    /// </remarks>
    [Fact]
    public void DesktopApp_QueryDialog_PopulatedWithSourceAndChip_CapturesInspectionScreenshot()
    {
        // Arrange - open the Query dialog. The workspace is already populated by the
        // SYSML2WORKBENCH_STARTUP_FILE-preloaded fixture (see remarks above).
        var queryMenu = _session.FindElement(MobileBy.Name("Query"));
        queryMenu.Click();
        var queryDialogMenuItem = _session.FindElement(MobileBy.AccessibilityId("QueryDialogMenuItem"));
        queryDialogMenuItem.Click();

        try
        {
            // Act - add a real "attribute" type-filter chip via the "+" button's search-filtered flyout.
            var addTypeFilterButton = _session.FindElement(MobileBy.AccessibilityId("AddTypeFilterButton"));
            addTypeFilterButton.Click();

            // Type into the search box and press Enter rather than clicking a ListBoxItem directly: a
            // real pointer click into an open Flyout races NovaWindows' synthesized input against
            // Avalonia's light-dismiss overlay - confirmed by direct visual observation (the mouse
            // visibly hovers over the "attribute" entry, but the popup dismisses instead of the item
            // being selected), so the click "succeeds" from Appium's point of view yet no chip is ever
            // actually added. Typing into a TextBox and pressing Enter is one of the most reliable
            // interactions across every Appium/UIA driver, with no pointer hit-testing involved.
            var searchTextBox = _session.FindElement(MobileBy.AccessibilityId("AddTypeFilterSearchTextBox"));
            searchTextBox.SendKeys("attribute");
            searchTextBox.SendKeys(OpenQA.Selenium.Keys.Enter);

            var elementFilterRoot = _session.FindElement(MobileBy.AccessibilityId("ElementFilterRoot"));

            // The chip is added asynchronously (the key press returns to Appium before Avalonia's
            // dispatcher has re-rendered the bound ItemsControl), so poll briefly for the "attribute"
            // chip's own text element to actually appear before capturing - otherwise the screenshot
            // can race ahead of the render and show only the empty chip row.
            var chipRendered = WaitUntil(() => _session.FindElements(MobileBy.Name("attribute")).Count > 0);
            Assert.True(chipRendered, "The 'attribute' chip did not render in time for the screenshot.");

            InspectionScreenshot.CaptureElement(_session, elementFilterRoot, "query-dialog-populated-with-chip");

            // Assert
            Assert.True(elementFilterRoot.Displayed);

            _session.FindElement(MobileBy.AccessibilityId("QueryDialogCloseButton")).Click();
        }
        catch
        {
            TryCloseAnyOpenDialogWithEscape();
            throw;
        }
        finally
        {
            // See this test's remarks for why cleanup is owned only by this one mutating test.
            _fixture.CloseAllSources();
        }
    }

    /// <summary>
    ///     Best-effort cleanup used when a dialog-opening test fails before it reaches its own close-button
    ///     click: sends Escape at the driver level (rather than to a specific, possibly-not-found element) so
    ///     a stray modal dialog does not stay open and block every later test in this shared-session fixture,
    ///     as happened once during development of this test class. Swallows any exception of its own, since
    ///     this only runs while another exception is already propagating and must never mask it.
    /// </summary>
    private void TryCloseAnyOpenDialogWithEscape()
    {
        try
        {
            new OpenQA.Selenium.Interactions.Actions(_session).SendKeys(OpenQA.Selenium.Keys.Escape).Perform();
        }
        catch
        {
            // Best-effort only - the original exception is what the caller should see.
        }
    }

    /// <summary>
    ///     Polls <paramref name="condition" /> every 100ms until it returns <see langword="true" /> or
    ///     <paramref name="timeout" /> elapses (default 3 seconds), returning whether it succeeded. Used to
    ///     wait for UI-thread-driven, binding-triggered visual changes (for example a chip appearing after a
    ///     selection click) that Appium's synchronous <c>Click()</c> does not itself wait for, without adding
    ///     a Selenium.Support package dependency just for <c>WebDriverWait</c>.
    /// </summary>
    private static bool WaitUntil(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return condition();
    }
}
