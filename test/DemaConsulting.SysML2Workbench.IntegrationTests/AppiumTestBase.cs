using OpenQA.Selenium.Appium;

namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Base class for Appium integration test classes, giving every individual <c>[Fact]</c> its own dedicated
///     application launch and WebDriver session rather than sharing one session across a whole test class or
///     collection.
/// </summary>
/// <remarks>
///     xUnit constructs a fresh instance of a test class for every <c>[Fact]</c>/<c>[Theory]</c> case by
///     design, so a plain <see cref="AppFixture" /> field owned directly by this base class (rather than
///     injected via <c>[Collection]</c>/<c>ICollectionFixture&lt;T&gt;</c>) already gives every test method its
///     own process launch and session with zero custom xUnit lifecycle plumbing - <see cref="Dispose" /> quits
///     it again once that one test finishes, whether it passed or failed. <see cref="StartApp" /> additionally
///     lets a single test method relaunch with different startup arguments mid-test (quitting/disposing any
///     previous session first), for scenarios that need to prove real process-level restart behavior rather
///     than just an in-app "reset" action (for example, a "Close All" menu item test could instead relaunch and
///     assert the workspace comes back empty, exercising the same guarantee a real user restart would rely on).
///     <para>
///     This intentionally trades the speed of one shared, long-lived session (avoiding per-test launch/attach
///     overhead) for full test isolation: no test can leak dialog/panel/workspace state into a later test, and
///     no test needs its own bespoke cleanup (<c>CloseAllSources</c>, "restore the panel to its original open
///     state", <c>TryCloseAnyOpenDialogWithEscape</c>-after-failure reasoning, etc.) purely to protect
///     shared-session neighbors it has no other relationship with.
///     </para>
/// </remarks>
public abstract class AppiumTestBase : IDisposable
{
    private AppFixture? _fixture;

    /// <summary>
    ///     The live WebDriver session from the most recent <see cref="StartApp" /> call (including the
    ///     no-argument default call made by this base class's constructor).
    /// </summary>
    protected AppiumDriver Session =>
        _fixture?.Driver ?? throw new InvalidOperationException($"Call {nameof(StartApp)}() before using {nameof(Session)}.");

    /// <summary>
    ///     Launches the application with no startup arguments, giving every derived test class a ready-to-use
    ///     <see cref="Session" /> without requiring an explicit <see cref="StartApp" /> call in the common case.
    /// </summary>
    protected AppiumTestBase()
    {
        StartApp();
    }

    /// <summary>
    ///     Launches (or relaunches) the application with the given startup arguments, replacing
    ///     <see cref="Session" />. Any previous session started by this test is quit/disposed first, so calling
    ///     this more than once within a single test method exercises a genuine process-level quit/relaunch
    ///     round trip, not just an in-app reset.
    /// </summary>
    /// <param name="startupArguments">
    ///     Command-line arguments forwarded to the launched process via <see cref="AppFixture" />'s per-session
    ///     <c>appArguments</c>/<c>arguments</c> capability - see <c>App.axaml.cs</c>'s
    ///     <c>ApplyStartupSourceArgumentsForTesting</c> for the recognized <c>--startup-source &lt;path&gt;</c>
    ///     format (repeatable, one path - file or folder - per occurrence).
    /// </param>
    protected void StartApp(string startupArguments = "")
    {
        _fixture?.Dispose();
        _fixture = new AppFixture(startupArguments);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _fixture?.Dispose();
    }

    /// <summary>
    ///     Opens the named top-level menu once, locates each child item by automation id, asserts each is
    ///     displayed and enabled, then closes the menu once via Escape without clicking any item - the
    ///     multi-item counterpart to <see cref="AssertMenuItemIsDiscoverableAndEnabled" /> for checking several
    ///     items in the same menu without reopening it once per item.
    /// </summary>
    /// <param name="topLevelMenuName">The top-level menu's display name (e.g. "File", "View").</param>
    /// <param name="automationIds">The child menu items' <c>AutomationProperties.AutomationId</c> values.</param>
    protected void AssertMenuItemsAreDiscoverableAndEnabled(string topLevelMenuName, params string[] automationIds)
    {
        // Arrange
        var topLevelMenu = Session.FindElement(MobileBy.Name(topLevelMenuName));
        topLevelMenu.Click();

        // Act / Assert
        OpenQA.Selenium.IWebElement? lastMenuItem = null;
        foreach (var automationId in automationIds)
        {
            var menuItem = Session.FindElement(MobileBy.AccessibilityId(automationId));
            Assert.True(menuItem.Displayed);
            Assert.True(menuItem.Enabled);
            lastMenuItem = menuItem;
        }

        // Close the menu without picking any item so this test leaves no side effect behind it.
        lastMenuItem?.SendKeys(OpenQA.Selenium.Keys.Escape);
    }

    /// <summary>
    ///     Opens the named top-level menu, locates the child item by automation id, asserts it is displayed and
    ///     enabled, then closes the menu via Escape without clicking the item itself - proving the item is
    ///     reachable through the real accessibility tree without triggering whatever side effect (dialog, panel
    ///     toggle) clicking it would cause.
    /// </summary>
    /// <param name="topLevelMenuName">The top-level menu's display name (e.g. "File", "View", "Query").</param>
    /// <param name="automationId">The child menu item's <c>AutomationProperties.AutomationId</c> value.</param>
    protected void AssertMenuItemIsDiscoverableAndEnabled(string topLevelMenuName, string automationId)
    {
        // Arrange
        var topLevelMenu = Session.FindElement(MobileBy.Name(topLevelMenuName));
        topLevelMenu.Click();

        // Act
        var menuItem = Session.FindElement(MobileBy.AccessibilityId(automationId));

        // Assert
        Assert.True(menuItem.Displayed);
        Assert.True(menuItem.Enabled);

        // Close the menu without picking the item so this test leaves no side effect behind it.
        menuItem.SendKeys(OpenQA.Selenium.Keys.Escape);
    }

    /// <summary>
    ///     Drives one full close/reopen round trip through a View-menu panel-toggle item. Confirms the toggle
    ///     behavior purely via the panel's own control (<paramref name="panelControlAutomationId" />) becoming
    ///     absent then present again, rather than reading the menu item's checked state through UI Automation:
    ///     Avalonia's Win32 automation bridge does not currently surface the UIA Toggle pattern for
    ///     <c>MenuItem</c> to native automation clients (its cross-platform <c>MenuItemAutomationPeer</c>
    ///     implements <c>IToggleProvider</c> internally, but that provider is never reachable from a real UI
    ///     Automation client such as NovaWindows/System.Windows.Automation, which always throws "Unsupported
    ///     Pattern" attempting to read it) - so content presence is the only reliable ground truth available.
    /// </summary>
    /// <param name="topLevelMenuName">The top-level menu's display name (e.g. "View").</param>
    /// <param name="menuItemAutomationId">The child menu item's <c>AutomationProperties.AutomationId</c> value.</param>
    /// <param name="panelControlAutomationId">
    ///     An automation id unique to a control that is always present while the panel is open and the active
    ///     tab (not conditionally hidden by panel content state, e.g. an always-visible toolbar button), used to
    ///     confirm the panel is genuinely shown or hidden.
    /// </param>
    protected void AssertMenuItemTogglesPanel(string topLevelMenuName, string menuItemAutomationId, string panelControlAutomationId)
    {
        // Act 1 - click once; the panel starts open, so this closes it.
        ClickMenuItem(topLevelMenuName, menuItemAutomationId);
        var closed = WaitUntil(() => Session.FindElements(MobileBy.AccessibilityId(panelControlAutomationId)).Count == 0);
        Assert.True(closed, $"The panel control '{panelControlAutomationId}' was still present after closing it via '{menuItemAutomationId}'.");

        // Act 2 - click again; the panel is now closed, so this reopens and focuses it.
        ClickMenuItem(topLevelMenuName, menuItemAutomationId);
        var reopened = WaitUntil(() => Session.FindElements(MobileBy.AccessibilityId(panelControlAutomationId)).Count > 0);
        Assert.True(reopened, $"The panel control '{panelControlAutomationId}' did not reappear after reopening it via '{menuItemAutomationId}'.");
        Assert.True(Session.FindElement(MobileBy.AccessibilityId(panelControlAutomationId)).Displayed);
    }

    /// <summary>
    ///     Opens the named top-level menu, clicks the child item by automation id (which also closes the menu,
    ///     matching normal menu-click behavior), then returns.
    /// </summary>
    /// <param name="topLevelMenuName">The top-level menu's display name (e.g. "View").</param>
    /// <param name="menuItemAutomationId">The child menu item's <c>AutomationProperties.AutomationId</c> value.</param>
    protected void ClickMenuItem(string topLevelMenuName, string menuItemAutomationId)
    {
        Session.FindElement(MobileBy.Name(topLevelMenuName)).Click();
        Session.FindElement(MobileBy.AccessibilityId(menuItemAutomationId)).Click();
    }

    /// <summary>
    ///     Best-effort cleanup used when a dialog-opening test fails before it reaches its own close-button
    ///     click: sends Escape at the driver level (rather than to a specific, possibly-not-found element) so a
    ///     stray modal dialog does not stay open and mask the real assertion failure with a spurious one from
    ///     xUnit's own teardown. Swallows any exception of its own, since this only runs while another exception
    ///     is already propagating and must never mask it.
    /// </summary>
    protected void TryCloseAnyOpenDialogWithEscape()
    {
        try
        {
            new OpenQA.Selenium.Interactions.Actions(Session).SendKeys(OpenQA.Selenium.Keys.Escape).Perform();
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
    ///     selection click) that Appium's synchronous <c>Click()</c> does not itself wait for, without adding a
    ///     Selenium.Support package dependency just for <c>WebDriverWait</c>.
    /// </summary>
    protected static bool WaitUntil(Func<bool> condition, TimeSpan? timeout = null)
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
