namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Real Appium tests validating the View menu's items, through a dedicated per-test
///     <see cref="AppFixture" /> session (see <see cref="AppiumTestBase" />). Marked
///     <c>[Trait("Category", "Integration")]</c> so <c>.github/workflows/build.yaml</c>'s cross-platform
///     <c>build</c> job (which has no Appium server running) can exclude this tier via
///     <c>--filter "Category!=Integration"</c>, while the dedicated <c>appium-windows-integration-tests</c> job
///     (and a developer running <c>build.ps1 -IntegrationTest</c> locally on any OS) runs it for real.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ViewMenuIntegrationTests : AppiumTestBase
{
    /// <summary>
    ///     Validates that the View menu's "Workspace" (<c>WorkspacePanelMenuItem</c>), "Predefined Views"
    ///     (<c>PredefinedViewsMenuItem</c>), "Diagnostics" (<c>DiagnosticsMenuItem</c>), and "Custom View
    ///     Builder..." (<c>ViewBuilderDialogMenuItem</c>) items are all discoverable and enabled. Combined into
    ///     a single test (rather than one per item) since all four are pure, side-effect-free discoverability
    ///     checks against the same menu - opening the View menu once, checking all four items in place, then
    ///     closing it once, instead of reopening the menu per item across four separate tests and application
    ///     launches. None of the items are clicked here: the panel-toggle items have their own dedicated
    ///     click-through round-trip tests below, and <c>ViewBuilderDialogMenuItem</c>'s opened dialog does not
    ///     yet carry automation ids that would let this fixture close it deterministically.
    /// </summary>
    [Fact]
    public void DesktopApp_ViewMenu_Items_AreDiscoverableAndEnabled()
    {
        AssertMenuItemsAreDiscoverableAndEnabled(
            "View",
            "WorkspacePanelMenuItem",
            "PredefinedViewsMenuItem",
            "DiagnosticsMenuItem",
            "ViewBuilderDialogMenuItem");
    }

    /// <summary>
    ///     Validates that clicking the View menu's "Workspace" item, found by the <c>WorkspacePanelMenuItem</c>
    ///     automation id, toggles the Workspace panel open/closed - proving <c>MainWindowView</c>'s
    ///     <c>ShowOrFocusPanel</c> genuinely destroys/recreates the panel's dock presence (checkbox semantics:
    ///     checked means the panel exists and clicking removes it, unchecked means it does not and clicking
    ///     creates/shows it), and that opening it also brings it into focus. See
    ///     <see cref="AppiumTestBase.AssertMenuItemTogglesPanel" /> for the full round-trip this drives.
    /// </summary>
    [Fact]
    public void DesktopApp_ViewMenu_WorkspacePanelMenuItem_TogglesWorkspacePanel()
    {
        AssertMenuItemTogglesPanel("View", "WorkspacePanelMenuItem", "WorkspaceAddFileButton");
    }

    /// <summary>
    ///     Validates that clicking the View menu's "Predefined Views" item, found by the
    ///     <c>PredefinedViewsMenuItem</c> automation id, toggles the Predefined Views panel open/closed - see
    ///     <see cref="DesktopApp_ViewMenu_WorkspacePanelMenuItem_TogglesWorkspacePanel" /> for the shared
    ///     checkbox-toggle semantics this proves.
    /// </summary>
    [Fact]
    public void DesktopApp_ViewMenu_PredefinedViewsMenuItem_TogglesPredefinedViewsPanel()
    {
        AssertMenuItemTogglesPanel("View", "PredefinedViewsMenuItem", "PredefinedViewsListBox");
    }

    /// <summary>
    ///     Validates that clicking the View menu's "Diagnostics" item, found by the <c>DiagnosticsMenuItem</c>
    ///     automation id, toggles the Diagnostics panel open/closed - see
    ///     <see cref="DesktopApp_ViewMenu_WorkspacePanelMenuItem_TogglesWorkspacePanel" /> for the shared
    ///     checkbox-toggle semantics this proves.
    /// </summary>
    [Fact]
    public void DesktopApp_ViewMenu_DiagnosticsMenuItem_TogglesDiagnosticsPanel()
    {
        AssertMenuItemTogglesPanel("View", "DiagnosticsMenuItem", "DiagnosticsListBox");
    }
}
