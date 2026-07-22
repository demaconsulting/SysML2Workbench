# SysML2Workbench

A cross-platform desktop viewer and GUI custom-view builder for SysML v2
models, built on the SysML2Tools rendering engine.

SysML2Workbench opens a folder of `.sysml` files as a live workspace, renders
the same predefined diagrams (General, Interconnection, State Transition,
Action Flow, Sequence, and Grid) produced by the SysML2Tools CLI in an
interactive pan/zoom viewer, and lets you build ad-hoc custom views through a
GUI - picking a view kind, multi-selecting target elements, and optionally
filtering - without hand-writing SysML `view` syntax. Custom views can be
exported as copy-pasteable SysML `view ... expose ...` text to promote them
into a permanent model file, and any workspace `.sysml` file's raw source
text can be opened read-only, with syntax highlighting, by double-clicking
it in the workspace tree. A live-updating diagnostics panel surfaces parser
and reference-resolution problems across the whole workspace, and a local
rolling log file is written for bug-report attachments.

This is a **Phase 0, read-only** release: there is no git integration, no
text/structural editing, no telemetry, and custom views are session-only until
exported as text. See [docs/design/introduction.md](docs/design/introduction.md)
for the full scope and [docs/user_guide/](docs/user_guide/) for usage
instructions.

## Getting Started

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet run --project src/DemaConsulting.SysML2Workbench.Desktop
```

See [docs/user_guide/getting_started.md](docs/user_guide/getting_started.md)
for a walkthrough of opening a workspace, browsing predefined views, and
building a custom view.

## Repository Layout

- `src/DemaConsulting.SysML2Workbench/` - shared application project (all
  subsystems: workspace, view catalog, view builder, layout/rendering,
  diagnostics panel, logging, app shell).
- `src/DemaConsulting.SysML2Workbench.Desktop/` - desktop platform head
  (Windows/Linux/macOS entry point).
- `test/DemaConsulting.SysML2Workbench.Tests/` - unit and subsystem-level
  tests mirroring `src/`.
- `test/DemaConsulting.SysML2Workbench.UiTests/` - headless, in-process
  Avalonia UI tests (view/view-model interaction, no real window).
- `test/DemaConsulting.SysML2Workbench.IntegrationTests/` - Appium-driven
  end-to-end tests against the compiled Desktop application (Windows-only
  in CI today; requires a running Appium server - see "Building and
  Testing" below).
- `test/OtsSoftwareTests/` - integration tests for the off-the-shelf (OTS)
  dependencies (SysML2Tools, Rendering, Avalonia, xUnit).
- `docs/` - requirements (`docs/reqstream/`), design (`docs/design/`),
  verification (`docs/verification/`), the SysML2 architecture model
  (`docs/sysml2/`), and the [user guide](docs/user_guide/).

## Building and Testing

```powershell
pwsh ./build.ps1   # restore, build, and run all tests
pwsh ./fix.ps1      # auto-fix formatting
pwsh ./lint.ps1     # lint and compliance checks
```

`build.ps1` and CI's cross-platform build job both run `dotnet test --filter
"Category!=Integration"`, so `test/DemaConsulting.SysML2Workbench.IntegrationTests`'
Appium-driven tests are excluded from the default local/CI test run. That
tier only runs for real in CI's dedicated `appium-windows-integration-tests`
job (`windows-latest`), which publishes the Desktop application, starts a
local Appium server with the NovaWindows driver, and runs
`dotnet test test/DemaConsulting.SysML2Workbench.IntegrationTests/...`
against it. Running it locally requires a published Desktop build, a
running Appium server, and the NovaWindows driver installed.
