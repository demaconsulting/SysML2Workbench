## Appium

SysML2Workbench uses Appium's WebDriver client, together with Appium's
NovaWindows and Mac2 drivers (and KDE's `selenium-webdriver-at-spi` on Linux),
to drive the compiled desktop application end-to-end through its real
accessibility tree as a system-level UI automation test tier.

### Purpose

Appium was chosen because it exercises the actual compiled
`DemaConsulting.SysML2Workbench.Desktop` application in a real window, driven
the same way a user would interact with it, complementing the in-process
`test/DemaConsulting.SysML2Workbench.UiTests` headless tier and the
`test/DemaConsulting.SysML2Workbench.Tests` unit/subsystem tier. Avalonia's
`AutomationPeer` exposes controls to UIA (Windows), NSAccessibility
(macOS, via Appium's Mac2 driver), and AT-SPI2 (Linux, via Avalonia's X11
backend only - Wayland support is unconfirmed), so Appium can, in principle,
drive the same application on all three desktop platforms through a common
`AutomationProperties.AutomationId`-based control lookup.

### Features Used

- **`Appium.WebDriver`'s `WindowsDriver`/`MacDriver`** — connect to a local
  Appium server running the NovaWindows driver (the maintained successor to
  the deprecated WinAppDriver) or the Mac2 driver, and launch the published
  Desktop executable as the driven application. Linux has no dedicated
  first-party Appium .NET client type (KDE's `selenium-webdriver-at-spi` is
  not one of Appium's officially-known platforms), so `AppFixture` defines a
  minimal `LinuxDriver : AppiumDriver` subclass purely to reach an
  otherwise-inaccessible base constructor.
- **`AutomationProperties.AutomationId`** — added to `MainWindowView.axaml`'s
  menu items and dock control, and to the interactive controls of the About
  dialog, Query dialog (shared `ElementFilterView`'s type-filter controls plus
  the dialog's own close button), Workspace panel, and Predefined Views panel,
  giving `MobileBy.AccessibilityId` lookups a stable, Appium-reliable target
  independent of `Name`/`x:Name`.
- **`MobileBy.Name`/`MobileBy.AccessibilityId`** — locate top-level menus and
  automation-id-tagged controls in the real accessibility tree.

### Integration Pattern

`test/DemaConsulting.SysML2Workbench.IntegrationTests` has no `ProjectReference`
to the local UI project: it drives the compiled application externally as a
black-box system-level tier, per `testing-principles.md`'s hierarchy-boundary
rule. `AppFixture` follows Avalonia's documented Appium `AppFixture` pattern,
branching by `OperatingSystem.IsWindows()`/`IsMacOS()`/`IsLinux()` to select
the right driver/capabilities, but it never starts, stops, or otherwise
manages an Appium/AT-SPI server process itself - it always just connects a
WebDriver client to `http://127.0.0.1:4723`, trusting that something already
made a server available there before the test process launched. Only the
Windows/NovaWindows branch is exercised by CI today and validated against a
real Appium server; the macOS/Mac2 and Linux/AT-SPI2 branches are implemented
from documentation so a developer can run this tier locally, but neither has
a provisioned CI runner nor has been exercised against real hardware - treat
both as best-effort and unvalidated until proven otherwise.

That server's lifecycle is owned entirely by **`run-under-appium.ps1`**
(repository root), invoked by `build.ps1 -IntegrationTest` and by
`.github/workflows/build.yaml`'s `appium-windows-integration-tests` job. It
wraps an arbitrary command (typically `dotnet test ... IntegrationTests.csproj`)
differently per OS:

- **Windows/macOS**: starts a local Appium server itself (resolving `node`/
  `appium` directly - on Windows via `node.exe <appium's index.js>` to avoid
  `Start-Process`'s inability to exec a `.cmd` wrapper without breaking PID
  tracking; on macOS `appium` is a real shebang script, so no such workaround
  is needed), polls `/status` until ready, runs the wrapped command, then
  stops the server in a `finally` block regardless of outcome.
- **Linux**: this is architecturally inverted rather than merely
  unimplemented. KDE's `selenium-webdriver-at-spi` has no standalone,
  directly-startable server binary or `--port` flag - its only supported
  entry point is `selenium-webdriver-at-spi-run <command>`, a wrapper that
  itself boots a nested Wayland compositor session plus its own Flask/AT-SPI2
  server, runs the wrapped command as its *child*, and tears everything down
  together once that child exits. So on Linux, `run-under-appium.ps1` simply
  delegates the entire wrapped command to that external tool instead of
  managing anything itself; `build.ps1 -IntegrationTest` does not install or
  build it - a Linux user is expected to have already built and installed it
  themselves (see KDE's docs at <https://community.kde.org/Selenium>) before
  running `-IntegrationTest` there.

`build.ps1 -IntegrationTest` is cross-platform (Windows/macOS/Linux): on
Windows/macOS it installs Appium and the appropriate driver (NovaWindows via
`--source=npm`, or Mac2) via `npm`/`appium driver install`, skipping that step
entirely on Linux; then it publishes the Desktop application for the current
OS/architecture's RID and runs `./run-under-appium.ps1 -- dotnet test ...`.
Only the Windows path is exercised in CI and treated as validated; macOS and
Linux are provided for developers who want to run this tier locally on those
platforms.

`.github/workflows/build.yaml`'s `appium-windows-integration-tests` job is the
only CI job that runs this tier, and remains Windows-only (`windows-latest`)
even though `build.ps1 -IntegrationTest` itself is now cross-platform - no
macOS/Linux CI runner is provisioned. It installs Appium and the NovaWindows
driver via npm, restores/builds/publishes the Desktop application as its own
separate steps (so each reports its own pass/fail status independently in the
GitHub Actions UI), then delegates to the same `run-under-appium.ps1` script
`build.ps1 -IntegrationTest` uses locally to start Appium, run
`IntegrationTests`, and stop Appium - sharing one tested code path for that
part of the sequence instead of duplicating it inline. The cross-platform
`build` job's `Test` step and `build.ps1 -Test` both exclude this tier with
`--filter "Category!=Integration"`, since `IntegrationTests`' tests carry
`[Trait("Category", "Integration")]` and require a running Appium server
that those invocations do not start.

### Troubleshooting: "Failed to locate window of the app." on Windows

If every NovaWindows test in `IntegrationTests` fails identically with
`WindowsDriver` throwing `UnknownError: Failed to locate window of the app.`
(surfaced via NovaWindowsDriver's `changeRootElement`), enable the Appium
server's own log output and look for an underlying error such as:

```text
Exception calling "SetFocus" with "0" argument(s): "Target element cannot receive focus."
```

This signature means NovaWindowsDriver *did* find the launched process and
its window, but Windows refused `SetForegroundWindow`/UI Automation
`SetFocus` for it. The most common root cause is that **the interactive
console session driving the test run is locked** - Windows switches to the
secure Winlogon/`LockApp` desktop while locked, which is isolated from
automation and blocks focus/foreground operations for *any* process, not
just the one under test. This is an environment precondition, not a code
regression: it reproduces identically even with no application code from the
current change involved.

`run-under-appium.ps1` includes a Windows-only preflight check that detects
this condition (via `GetForegroundWindow`/`GetWindowThreadProcessId`
resolving to `LockApp`/`LogonUI`) before running the wrapped test command,
and fails fast with an actionable error instead of letting all tests fail
slowly, one at a time, with this cryptic stack trace. The remediation is
simply to **unlock the interactive console/RDP session** and re-run
`-IntegrationTest`; no code change can substitute for this.
