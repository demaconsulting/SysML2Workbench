namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Real Appium tests validating the File menu's items, through a dedicated per-test
///     <see cref="AppFixture" /> session (see <see cref="AppiumTestBase" />). Marked
///     <c>[Trait("Category", "Integration")]</c> so <c>.github/workflows/build.yaml</c>'s cross-platform
///     <c>build</c> job (which has no Appium server running) can exclude this tier via
///     <c>--filter "Category!=Integration"</c>, while the dedicated <c>appium-windows-integration-tests</c> job
///     (and a developer running <c>build.ps1 -IntegrationTest</c> locally on any OS) runs it for real.
/// </summary>
[Trait("Category", "Integration")]
public sealed class FileMenuIntegrationTests : AppiumTestBase
{
    /// <summary>
    ///     Validates that the File menu's "Open File..." (<c>AddFileSourceMenuItem</c>), "Open Folder..."
    ///     (<c>AddFolderSourceMenuItem</c>), and "Close All" (<c>CloseAllMenuItem</c>) items are all
    ///     discoverable and enabled through the real accessibility tree exposed by Avalonia's UIA automation
    ///     peer. Combined into a single test (rather than one per item) since all three are pure, side-effect-free
    ///     discoverability checks against the same menu - opening the File menu once, checking all three items
    ///     in place, then closing it once, instead of reopening the menu per item across three separate tests
    ///     and application launches. None of the items are actually clicked: "Open File..."/"Open Folder..."
    ///     would open an OS-native dialog that lives outside Avalonia's own accessibility tree and is not
    ///     reliably automatable through the same driver session across Windows/macOS/Linux, and "Close All" has
    ///     no assertable close/reopen counterpart the way a panel-toggle item does.
    /// </summary>
    [Fact]
    public void DesktopApp_FileMenu_Items_AreDiscoverableAndEnabled()
    {
        AssertMenuItemsAreDiscoverableAndEnabled(
            "File",
            "AddFileSourceMenuItem",
            "AddFolderSourceMenuItem",
            "CloseAllMenuItem");
    }
}
