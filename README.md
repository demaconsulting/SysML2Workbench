# SysML2Workbench

[![GitHub forks][badge-forks]][link-forks]
[![GitHub stars][badge-stars]][link-stars]
[![GitHub contributors][badge-contributors]][link-contributors]
[![License][badge-license]][link-license]
[![Build][badge-build]][link-build]
[![Quality Gate][badge-quality]][link-quality]
[![Security][badge-security]][link-security]
[![Release][badge-release]][link-release]

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
- `test/DemaConsulting.SysML2Workbench.IntegrationTests/` - Appium/AT-SPI-driven
  end-to-end tests against the compiled Desktop application (Windows, macOS,
  and Linux; CI runs the Windows job today; requires a running automation
  server - see "Building and Testing" below).
- `test/OtsSoftwareTests/` - integration tests for the off-the-shelf (OTS)
  dependencies (SysML2Tools, Rendering, Avalonia, xUnit).
- `docs/` - requirements (`docs/reqstream/`), design (`docs/design/`),
  verification (`docs/verification/`), the SysML2 architecture model
  (`docs/sysml2/`), and the [user guide](docs/user_guide/).

## Building and Testing

```powershell
pwsh ./build.ps1 -Build -Test   # restore, build, and run the unit/headless test suite
pwsh ./build.ps1                # no switches: prints usage and exits non-zero
pwsh ./fix.ps1                  # auto-fix formatting
pwsh ./lint.ps1                 # lint and compliance checks
```

`build.ps1` accepts combinable switches: `-Build` (restore + build),
`-Test` (build if needed, then run the unit/headless suite), `-IntegrationTest`
(build if needed, then run the Appium/AT-SPI suite), and `-All`
(equivalent to all three). `build.ps1 -Test` and CI's cross-platform build
job both run `dotnet test --filter "Category!=Integration"`, so
`test/DemaConsulting.SysML2Workbench.IntegrationTests`' Appium-driven tests
are excluded from that default test run. That tier runs for real in CI's
dedicated `appium-windows-integration-tests` job (`windows-latest`), and can
now also be run locally on Windows, macOS, or Linux via
`pwsh ./build.ps1 -IntegrationTest`, which delegates to `run-under-appium.ps1`:
on Windows/macOS it installs Appium and the NovaWindows/Mac2 driver, publishes
the Desktop application, starts and polls a local Appium server, runs the
tests, and always stops the server afterward; on Linux it publishes the
Desktop application and delegates the whole test run to the pre-installed
`selenium-webdriver-at-spi-run` wrapper (see `docs/design/ots/appium.md`).

[badge-forks]: https://img.shields.io/github/forks/demaconsulting/SysML2Workbench?style=plastic
[badge-stars]: https://img.shields.io/github/stars/demaconsulting/SysML2Workbench?style=plastic
[badge-contributors]: https://img.shields.io/github/contributors/demaconsulting/SysML2Workbench?style=plastic
[badge-license]: https://img.shields.io/github/license/demaconsulting/SysML2Workbench?style=plastic
[badge-build]: https://img.shields.io/github/actions/workflow/status/demaconsulting/SysML2Workbench/build_on_push.yaml?style=plastic
[badge-quality]: https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_SysML2Workbench&metric=alert_status
[badge-security]: https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_SysML2Workbench&metric=security_rating
[badge-release]: https://img.shields.io/github/v/release/demaconsulting/SysML2Workbench?style=plastic

[link-forks]: https://github.com/demaconsulting/SysML2Workbench/network/members
[link-stars]: https://github.com/demaconsulting/SysML2Workbench/stargazers
[link-contributors]: https://github.com/demaconsulting/SysML2Workbench/graphs/contributors
[link-license]: https://github.com/demaconsulting/SysML2Workbench/blob/main/LICENSE
[link-build]: https://github.com/demaconsulting/SysML2Workbench/actions/workflows/build_on_push.yaml
[link-quality]: https://sonarcloud.io/dashboard?id=demaconsulting_SysML2Workbench
[link-security]: https://sonarcloud.io/dashboard?id=demaconsulting_SysML2Workbench
[link-release]: https://github.com/demaconsulting/SysML2Workbench/releases/latest
