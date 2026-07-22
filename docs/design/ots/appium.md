## Appium

SysML2Workbench uses Appium's WebDriver client, together with Appium's
NovaWindows driver, to drive the compiled desktop application end-to-end
through its real accessibility tree as a system-level UI automation test tier.

### Purpose

Appium was chosen because it exercises the actual compiled
`DemaConsulting.SysML2Workbench.Desktop` application in a real window, driven
the same way a user would interact with it, complementing the in-process
`test/DemaConsulting.SysML2Workbench.UiTests` headless tier and the
`test/DemaConsulting.SysML2Workbench.Tests` unit/subsystem tier. Avalonia
12.1.0's `AutomationPeer` exposes controls to UIA (Windows), NSAccessibility
(macOS, via Appium's Mac2 driver), and AT-SPI2 (Linux, via Avalonia's X11
backend only - Wayland support is unconfirmed), so Appium can, in principle,
drive the same application on all three desktop platforms through a common
`AutomationProperties.AutomationId`-based control lookup.

### Features Used

- **`Appium.WebDriver`'s `WindowsDriver`** — connects to a local Appium server
  running the NovaWindows driver (the maintained successor to the deprecated
  WinAppDriver) and launches the published Desktop executable as the driven
  application.
- **`AutomationProperties.AutomationId`** — added to `MainWindowView.axaml`'s
  menu items and dock control, and to the interactive controls of the About
  dialog, Workspace panel, and Predefined Views panel, giving
  `MobileBy.AccessibilityId` lookups a stable, Appium-reliable target
  independent of `Name`/`x:Name`.
- **`MobileBy.Name`/`MobileBy.AccessibilityId`** — locate top-level menus and
  automation-id-tagged controls in the real accessibility tree.

### Integration Pattern

`test/DemaConsulting.SysML2Workbench.IntegrationTests` has no `ProjectReference`
to the local UI project: it drives the compiled application externally as a
black-box system-level tier, per `testing-principles.md`'s hierarchy-boundary
rule. `AppFixture` follows Avalonia's documented Appium `AppFixture` pattern,
branching by `OperatingSystem.IsWindows()`/`IsMacOS()`/`IsLinux()`. Only the
Windows/NovaWindows branch is fully implemented and exercised by real tests
today; the macOS/Mac2 and Linux/AT-SPI2 branches are structurally correct
(matching the same shape) but intentionally throw rather than attempt an
session with no driver provisioned, since no macOS or Linux CI runner installs the
corresponding Appium driver yet. `.github/workflows/build.yaml`'s
`appium-windows-integration-tests` job is the only CI job that runs this
tier: it publishes the Desktop application, installs Appium and the
NovaWindows driver via npm, starts the Appium server, and runs
`dotnet test` against `IntegrationTests`. Every other `dotnet test` invocation
in this repository (the cross-platform `build` job's `Test` step and
`build.ps1`) excludes this tier with `--filter "Category!=Integration"`,
since `IntegrationTests`' tests carry `[Trait("Category", "Integration")]`
and require a running Appium server that those invocations do not start.
