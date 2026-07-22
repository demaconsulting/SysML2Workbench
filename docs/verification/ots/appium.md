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

### Test Scenarios

**DesktopApp_Launch_ShowsMainWindowWithExpectedTitle**: Confirms the Appium
session's `Title` reports "SysML2Workbench" immediately after launch, proving
the session is genuinely driving the compiled application's real main
window rather than a stub.

**DesktopApp_FileMenu_AddFileSourceMenuItem_IsDiscoverableAndClickable**:
Opens the File menu (`MobileBy.Name("File")`) and locates the "Open File..."
item by its `AddFileSourceMenuItem` automation id
(`MobileBy.AccessibilityId`), proving Avalonia's UIA automation peer exposes
the `AutomationProperties.AutomationId` values added to `MainWindowView.axaml`
through the real accessibility tree that Appium's NovaWindows driver reads.

**DesktopApp_HelpMenu_AboutMenuItem_OpensAndClosesAboutDialog**: Opens the
Help menu, clicks "About" (`AboutMenuItem`), confirms the modal About
dialog's `AboutDialogOkButton` becomes visible, then dismisses it - proving
a full menu-click-to-modal-dialog round trip works end-to-end through the
real windowed application.
