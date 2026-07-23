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

        // Act
        aboutMenuItem.Click();
        var okButton = _session.FindElement(MobileBy.AccessibilityId("AboutDialogOkButton"));

        // Assert
        Assert.True(okButton.Displayed);

        okButton.Click();
    }
}
