## Appium

### Verification Approach

Integration tests in `test/DemaConsulting.SysML2Workbench.IntegrationTests/` (split by feature area into
`MainWindowLaunchIntegrationTests.cs`, `FileMenuIntegrationTests.cs`, `ViewMenuIntegrationTests.cs`,
`QueryDialogIntegrationTests.cs`, and `HelpMenuIntegrationTests.cs`) qualify the Windows/NovaWindows Appium
session by launching the real, published `DemaConsulting.SysML2Workbench.Desktop` executable through
`AppFixture` and driving it through its actual accessibility tree, rather than a mocked or headless UI harness,
because the end-to-end platform integration itself is what this tier is qualifying. These tests only run for
real in `.github/workflows/build.yaml`'s `appium-windows-integration-tests`
job (and locally via `build.ps1 -IntegrationTest`/`run-under-appium.ps1`,
which is cross-platform but only validated on Windows), where an Appium
server and the NovaWindows driver are actually running; elsewhere (the
cross-platform `build` job's `Test` step and `build.ps1 -Test`) they are
excluded via `--filter "Category!=Integration"`, since no Appium/AT-SPI
server is started in those runs.

Every test class derives from `AppiumTestBase`, which gives each individual `[Fact]` its own dedicated
`AppFixture` launch and WebDriver session - xUnit constructs a fresh test-class instance per fact, so a plain
`AppFixture` field owned by the base class already gives full per-test isolation with no shared-collection
fixture, and `Dispose()` quits that one test's application process again once it finishes, whether it passed
or failed. `AppiumTestBase.StartApp(string startupArguments = "")` additionally lets a test relaunch mid-test
with different startup arguments (quitting/disposing any previous session first) - `AppFixture` forwards these
arguments to the launched process via the `appium:appArguments`/`appium:arguments` capabilities (recognized by
`App.axaml.cs`'s `ApplyStartupSourceArgumentsForTesting` as repeatable `--startup-source <path>` tokens), which
is how `DesktopApp_QueryDialog_PopulatedWithSourceAndChip_CapturesInspectionScreenshot` preloads a real
workspace without driving the unautomatable native "Open File" dialog. Because every test gets its own
application instance, no test needs bespoke cleanup (a shared-session `CloseAllSources` reset, restoring a
panel to its originally-open state, and so on) purely to protect other tests it has no other relationship
with.

### Test Scenarios

**DesktopApp_Launch_ShowsMainWindowWithExpectedTitle**: Confirms the Appium
session's `Title` reports "SysML2Workbench" immediately after launch, proving
the session is genuinely driving the compiled application's real main
window rather than a stub.

**DesktopApp_FileMenu_Items_AreDiscoverableAndEnabled**: Opens the File menu
(`MobileBy.Name("File")`) once and locates "Open File..." (`AddFileSourceMenuItem`),
"Open Folder..." (`AddFolderSourceMenuItem`), and "Close All"
(`CloseAllMenuItem`) by automation id (`MobileBy.AccessibilityId`), asserting
each is displayed and enabled, then closes the menu once via Escape without
clicking any item - proving Avalonia's UIA automation peer exposes the
`AutomationProperties.AutomationId` values added to `MainWindowView.axaml`
through the real accessibility tree that Appium's NovaWindows driver reads.
All three checks share one menu-open/close and one dedicated application
launch since they are pure, side-effect-free discoverability checks against
the same menu. None of the items are actually clicked: "Open File..."/"Open
Folder..." would open an OS-native dialog that lives outside Avalonia's own
accessibility tree, and "Close All" has no assertable close/reopen
counterpart the way a panel-toggle item does.

**DesktopApp_ViewMenu_Items_AreDiscoverableAndEnabled**: The same
discoverability-only shape as the File menu scenario above, applied to the
View menu's "Workspace" (`WorkspacePanelMenuItem`), "Predefined Views"
(`PredefinedViewsMenuItem`), "Diagnostics" (`DiagnosticsMenuItem`), and
"Custom View Builder..." (`ViewBuilderDialogMenuItem`) items, all checked
against one menu open/close in one test/application launch. The panel-toggle
items also have their own dedicated click-through round-trip tests (see
below); `ViewBuilderDialogMenuItem` is not yet clicked because its opened
dialog's controls don't carry automation ids that would let this fixture
close it deterministically.

**DesktopApp_QueryMenu_QueryDialogMenuItem_IsDiscoverableAndEnabled**: Opens
the Query menu and locates "Run Query..." (`QueryDialogMenuItem`) by
automation id, asserting it is displayed and enabled, then closes the menu
via Escape without clicking it - the same discoverability-only shape as the
File/View menu scenarios above, kept as its own single-item test since the
Query menu has only this one item to check.

**DesktopApp_HelpMenu_AboutMenuItem_OpensAndClosesAboutDialog**: Opens the
Help menu, clicks "About" (`AboutMenuItem`), confirms the modal About
dialog's `AboutDialogOkButton` becomes visible, then dismisses it and
confirms `AboutDialogOkButton` disappears from the accessibility tree -
proving a full menu-click-to-modal-dialog-and-back round trip works
end-to-end through the real windowed application.

**DesktopApp_QueryMenu_QueryDialogMenuItem_OpensAndClosesQueryDialog**: The
same open/click/assert/close round-trip shape as the About dialog test
above, applied to the Query dialog - opens the Query menu, clicks "Run
Query..." (`QueryDialogMenuItem`), confirms the dialog's own
`AddTypeFilterButton` (part of the shared `ElementFilterView`) becomes
visible, captures a cropped PNG of that button to
`artifacts/inspection/query-dialog-add-type-filter-button.png` via
`InspectionScreenshot.CaptureElement` (folded into this same round trip
rather than a separate open/close cycle, since the screenshot is just an
incidental capture of a control this test already has open and asserted
on), then dismisses the dialog via `QueryDialogCloseButton` and confirms
`AddTypeFilterButton` disappears from the accessibility tree. The
screenshot capture is not itself a pass/fail correctness assertion (only
that the button is displayed and the capture mechanism completes without
error) - the actual visual review of the saved image is a human/agent task,
since automatically detecting styling defects is not a suitable CI gate.
Honors `SYSML2WORKBENCH_THEME` for which theme the capture reflects.

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
the second always reopens; no residual-state cleanup is needed since each
test's dedicated application instance is quit at the end of the test
regardless of outcome. Checked state is verified purely through panel content presence
rather than by reading the menu item's own checked state through UI
Automation, because Avalonia's Win32 automation bridge does not currently
expose the UIA Toggle pattern for `MenuItem` to native automation clients
(confirmed via `System.Windows.Automation` and Inspect.exe, independent of
this codebase) - a real Avalonia platform limitation, not a defect in this
test suite.

**DesktopApp_QueryDialog_PopulatedWithSourceAndChip_CapturesInspectionScreenshot**: Relaunches its own dedicated
application instance with `StartApp("--startup-source <path>")` pointing at
`TestData/InspectionSample.sysml` (avoiding the unautomatable native "Open File" dialog), opens the Query
dialog, clicks `AddTypeFilterButton`, selects the "attribute" entry from its flyout `ListBox` to add a real
filter chip, then captures the whole filter-row region (`ElementFilterRoot`) to
`artifacts/inspection/query-dialog-populated-with-chip.png` - proving the already-applied
`ElementFilterView.axaml` chip-foreground contrast fix (`Foreground="#212121"` on the chip text and its "X" remove
button) is legible with a real populated chip, not just an empty "+" button. No cleanup is needed afterward: this
test's own dedicated application instance is quit once the test finishes, whether it passed or failed, so the
preloaded workspace can never leak into another test.
