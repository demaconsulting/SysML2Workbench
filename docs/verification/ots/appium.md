## Appium

### Verification Approach

Integration tests in `test/DemaConsulting.SysML2Workbench.IntegrationTests/MainWindowShellIntegrationTests.cs`
qualify the Windows/NovaWindows Appium session by launching the real,
published `DemaConsulting.SysML2Workbench.Desktop` executable through
`AppFixture` and driving it through its actual accessibility tree, rather
than a mocked or headless UI harness, because the end-to-end platform
integration itself is what this tier is qualifying. These tests only run for
real in `.github/workflows/build.yaml`'s `appium-windows-integration-tests`
job (and locally via `build.ps1 -IntegrationTest`/`run-under-appium.ps1`,
which is cross-platform but only validated on Windows), where an Appium
server and the NovaWindows driver are actually running; elsewhere (the
cross-platform `build` job's `Test` step and `build.ps1 -Test`) they are
excluded via `--filter "Category!=Integration"`, since no Appium/AT-SPI
server is started in those runs.

Several of these tests mutate shared workspace state (adding a real source, or relying on the
`SYSML2WORKBENCH_STARTUP_FILE`-preloaded fixture) against the one `AppFixture`-launched application process shared
across every `[Fact]` in this class (`[Collection("AppFixture")]`). Rather than resetting the workspace before
every test - which would deterministically wipe the `STARTUP_FILE` preload before the one test that depends on it
ever ran, since that preload only happens once at process launch and cannot be redone per test without driving the
very native "Open File" dialog this design avoids - cleanup is owned only by the one state-mutating test itself,
via `AppFixture.CloseAllSources()` (clicks File > Close All through the real accessibility tree) in a `finally`
block. Every other test in this class only asserts menu-item discoverability/enablement, which holds regardless of
whether a source happens to be loaded.

### Test Scenarios

**DesktopApp_Launch_ShowsMainWindowWithExpectedTitle**: Confirms the Appium
session's `Title` reports "SysML2Workbench" immediately after launch, proving
the session is genuinely driving the compiled application's real main
window rather than a stub.

**DesktopApp_FileMenu_AddFileSourceMenuItem_IsDiscoverableAndEnabled**:
Opens the File menu (`MobileBy.Name("File")`) and locates the "Open File..."
item by its `AddFileSourceMenuItem` automation id
(`MobileBy.AccessibilityId`), proving Avalonia's UIA automation peer exposes
the `AutomationProperties.AutomationId` values added to `MainWindowView.axaml`
through the real accessibility tree that Appium's NovaWindows driver reads.
The item is not actually clicked, since doing so would open the OS-native
"Open File" dialog, which lives outside Avalonia's own accessibility tree.

**DesktopApp_FileMenu_AddFolderSourceMenuItem_IsDiscoverableAndEnabled**,
**DesktopApp_ViewMenu_WorkspacePanelMenuItem_IsDiscoverableAndEnabled**,
**DesktopApp_ViewMenu_PredefinedViewsMenuItem_IsDiscoverableAndEnabled**,
**DesktopApp_ViewMenu_DiagnosticsMenuItem_IsDiscoverableAndEnabled**,
**DesktopApp_ViewMenu_ViewBuilderDialogMenuItem_IsDiscoverableAndEnabled**,
**DesktopApp_QueryMenu_QueryDialogMenuItem_IsDiscoverableAndEnabled**, and
**DesktopApp_FileMenu_CloseAllMenuItem_IsDiscoverableAndEnabled**:
Each opens its respective top-level menu (`File`/`View`/`Query`) and locates
one child menu item by automation id, asserting it is both displayed and
enabled, then closes the menu via Escape without clicking the item. These
extend automation-id coverage breadth-first across every subsystem's
top-level entry point (workspace panel, predefined views, diagnostics,
custom view builder, query dialog, close-all) without needing to click
through to a dialog or native OS surface that this tier cannot yet reliably
tear down.

**DesktopApp_HelpMenu_AboutMenuItem_OpensAndClosesAboutDialog**: Opens the
Help menu, clicks "About" (`AboutMenuItem`), confirms the modal About
dialog's `AboutDialogOkButton` becomes visible, then dismisses it - proving
a full menu-click-to-modal-dialog round trip works end-to-end through the
real windowed application.

**DesktopApp_ViewMenu_WorkspacePanelMenuItem_TogglesWorkspacePanel**,
**DesktopApp_ViewMenu_PredefinedViewsMenuItem_TogglesPredefinedViewsPanel**, and
**DesktopApp_ViewMenu_DiagnosticsMenuItem_TogglesDiagnosticsPanel**: Unlike the
`IsDiscoverableAndEnabled` scenarios above, these drive a full close/reopen
round trip through `MainWindowView`'s `ShowOrFocusPanel`, which implements
checkbox-style toggle semantics: a panel that is open (`Tool.IsOpen`) is
destroyed/closed via `WorkbenchDockFactory.CloseDockable` on click, while a
closed panel is restored, activated, and focused on click. Each test clicks
its View-menu item once and polls for the corresponding panel's own
automation id (`WorkspaceAddFileButton`, `PredefinedViewsListBox`,
`DiagnosticsListBox`) to disappear - proving the panel was actually closed,
not merely that the menu item is clickable - then clicks it a second time
and polls for the same control to reappear and be displayed, proving
reopening also brings the panel into view. All three panels start open per
`WorkbenchDockFactory.CreateLayout`, so the first click always closes and
the second always reopens, and each test restores the panel to its
original open state by the end, leaving no residual effect for later
tests. Checked state is verified purely through panel content presence
rather than by reading the menu item's own checked state through UI
Automation, because Avalonia's Win32 automation bridge does not currently
expose the UIA Toggle pattern for `MenuItem` to native automation clients
(confirmed via `System.Windows.Automation` and Inspect.exe, independent of
this codebase) - a real Avalonia platform limitation, not a defect in this
test suite.

**DesktopApp_QueryDialog_AddTypeFilterButton_CapturesInspectionScreenshot**: Opens the Query dialog and captures a
cropped PNG of the shared `ElementFilterView`'s "+" add-type-filter button (`AddTypeFilterButton`) to
`artifacts/inspection/query-dialog-add-type-filter-button.png` via `InspectionScreenshot.CaptureElement`, then
closes the dialog. Not a pass/fail correctness assertion (only that the button is displayed and the capture
mechanism completes without error) - the actual visual review of the saved image is a human/agent task, since
automatically detecting styling defects is not a suitable CI gate. Honors `SYSML2WORKBENCH_THEME` for which theme
the single capture reflects.

**DesktopApp_QueryDialog_PopulatedWithSourceAndChip_CapturesInspectionScreenshot**: Opens the Query dialog against
a workspace already populated by the `SYSML2WORKBENCH_STARTUP_FILE`-preloaded `TestData/InspectionSample.sysml`
fixture (avoiding the unautomatable native "Open File" dialog), clicks `AddTypeFilterButton`, selects the
"attribute" entry from its flyout `ListBox` to add a real filter chip, then captures the whole filter-row region
(`ElementFilterRoot`) to `artifacts/inspection/query-dialog-populated-with-chip.png` - proving the already-applied
`ElementFilterView.axaml` chip-foreground contrast fix (`Foreground="#212121"` on the chip text and its "X" remove
button) is legible with a real populated chip, not just an empty "+" button. Resets the shared session's workspace
back to empty via `AppFixture.CloseAllSources()` in a `finally` block (see the Verification Approach section
above for why cleanup is owned only by this one test).
