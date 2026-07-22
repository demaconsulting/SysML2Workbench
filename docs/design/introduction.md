# Introduction

SysML2Workbench is a single software system: a cross-platform desktop viewer
for SysML v2 textual models. It is built as a thin shell over the
`DemaConsulting.SysML2Tools` OTS libraries (parser, semantic model, layout
engine) and the `DemaConsulting.Rendering` OTS library, all consumed as
published NuGet packages. Local design coverage is organized as one system,
eight subsystems (no nesting), and fifteen units. There are no Shared Packages
in this repository; OTS items are documented under the parallel `ots/`
folders.

## Purpose

This document defines the design for each software item in SysML2Workbench —
full architectural and detailed design for the local system, its subsystems,
and its units, and integration/usage design for OTS software items. A
reviewer should be able to understand how each item satisfies its
requirements without reading source code.

## Scope

Local items:

- **SysML2Workbench**: system, subsystem, and unit design for the Phase 0
  read-only viewer, covering workspace loading, predefined and custom view
  rendering, diagnostics, and logging.

OTS items:

- **DemaConsulting.SysML2Tools**: integration and usage design (parser,
  semantic model, layout engine).
- **DemaConsulting.Rendering**: integration and usage design (SVG rendering).
- **Avalonia**: integration and usage design (UI framework).
- **AvaloniaEdit**: integration and usage design (read-only syntax-highlighted text editor control).
- **xUnit**: integration and usage design (test framework).
- **Appium**: integration and usage design (end-to-end desktop UI automation
  driver for the compiled application, Windows-only in CI today).

Out of scope: no Shared Packages exist in this repository. Design documents
are not produced for test projects or build pipeline CI configuration (this
means no per-project design doc for `test/DemaConsulting.SysML2Workbench.Tests`,
`UiTests`, `IntegrationTests`, or `OtsSoftwareTests` themselves - the test
*strategy* they implement is still described below), and the internal design
of OTS items is excluded — only their integration and usage within
SysML2Workbench is documented.

## Software Structure

Agents should query the SysML2 model under `docs/sysml2/` (see the
`sysml2tools-query` skill) rather than parsing this diagram, for an
up-to-date, machine-queryable view of software structure, purpose, and
relationships.

![Software Structure](SoftwareStructureView.svg)

The system decomposes as follows. `Desktop` is the platform-head entry point
project under `AppShellSubsystem` — it hosts `MainWindowShell` for the
desktop target but is not itself a separately documented unit, since it is a
thin bootstrap/entry-point project with no independent design of its own.

- **SysML2Workbench** (System)
  - **WorkspaceSubsystem** — folder-based workspace, live file watching, glob/import resolution
    - **WorkspaceModel** (Unit) — in-memory folder/file tree and per-file parse state
    - **FileWatcher** (Unit) — detects external file changes, triggers incremental reload
    - **DiagnosticsAggregator** (Unit) — collects `SysmlDiagnostic` across the workspace
  - **ViewCatalogSubsystem** — renders the six predefined view kinds
    - **ViewCatalogPresenter** (Unit) — lists/selects available views defined in the model
  - **ViewBuilderSubsystem** — GUI for constructing custom views
    - **ViewDefinitionModel** (Unit) — view type, multi-target expose selection, filter expression (session-only)
    - **SysmlSnippetGenerator** (Unit) — emits copy-pasteable `view ... expose ...` SysML text
  - **LayoutRenderingSubsystem** — turns a selected view into pixels
    - **LayoutInvoker** (Unit) — wraps `DemaConsulting.SysML2Tools.Rendering.DiagramRenderer.RenderWorkspace` to render a predefined or GUI-authored custom view directly to SVG
    - **SvgCanvasHost** (Unit) — hosts DemaConsulting.Rendering SVG output in an Avalonia SVG control, with pan/zoom
  - **DiagnosticsPanelSubsystem** — UI list of diagnostics
    - **DiagnosticsListView** (Unit) — structured for future click-to-navigate
  - **LoggingSubsystem**
    - **RollingFileLogger** (Unit) — local rolling log file for user-attachable bug reports
  - **ElementPickerSubsystem** — reusable dialog-agnostic element-picker control
    - **ElementFilter** (Unit) — chip/search filter-only view model + view, composed by ElementPicker for selection-bearing callers
    - **ElementPicker** (Unit) — chip/search/list view model + view for a caller-built candidate list
  - **AppShellSubsystem** — window, navigation, tabbed views
    - **MainWindowShell** (Unit)
    - **WorkspacePanel** (Unit) — workspace tree panel view model
    - **AboutDialog** (Unit) — modal about dialog
    - **ViewBuilderDialog** (Unit) — modal custom-view builder dialog
    - **QueryDialog** (Unit) — modal query dialog (Browse + Element Query tabs)
    - Desktop (platform head / entry point project; not a separately documented unit)

## Test Strategy

The repository runs four distinct test tiers, each with a different scope and
dependency boundary:

- **`test/DemaConsulting.SysML2Workbench.Tests`** — unit and subsystem-level
  xUnit v3 tests mirroring `src/`, with no UI framework dependency.
- **`test/DemaConsulting.SysML2Workbench.UiTests`** — headless, in-process
  Avalonia tests (`Avalonia.Headless.XUnit`) exercising this repository's own
  view/view-model interaction logic (menu command wiring, dialog open/close,
  panel state) without a visible window.
- **`test/DemaConsulting.SysML2Workbench.IntegrationTests`** — Appium-driven,
  black-box, system-level tests that launch the real, compiled
  `DemaConsulting.SysML2Workbench.Desktop` executable and drive it through
  its actual accessibility tree, the same way a user would. This tier has no
  `ProjectReference` to the local UI project (see `docs/design/ots/appium.md`).
- **`test/OtsSoftwareTests`** — a sibling, not a duplicate, of the above: it
  qualifies the OTS dependencies themselves (Avalonia, Dock, AvaloniaEdit,
  SysML2Tools, Rendering) against their own OTS requirements, using the same
  headless-Avalonia mechanism as `UiTests` but for a different purpose.

Only the Windows/NovaWindows path of `IntegrationTests` runs automatically in
CI today (`.github/workflows/build.yaml`'s `appium-windows-integration-tests`
job, `windows-latest`). Avalonia 12.1.0 also exposes controls to macOS's
NSAccessibility (Appium's Mac2 driver) and Linux's AT-SPI2 (X11 backend only;
Wayland is unconfirmed), and `AppFixture`'s OS-branching code for both is
structurally present and correct, but neither has a provisioned CI runner or
driver install yet - see `docs/design/ots/appium.md` for the integration
pattern and `.agent-logs/planning-appium-integration-tests-7f3a1c2e.md` for
the scope decision. `build.ps1 -Test` and the cross-platform `build` job's
`Test` step both exclude `IntegrationTests`' tests carrying the `Integration`
trait via `--filter "Category!=Integration"`, so adding `IntegrationTests` to
`SysML2Workbench.slnx` does not break those runs. `build.ps1 -IntegrationTest`
provides a Windows-only local equivalent of CI's
`appium-windows-integration-tests` job, running this tier for real on demand
outside CI.

## Folder Layout

- **src/** - source files and projects
  - **DemaConsulting.SysML2Workbench/** - shared UI project: views, view models, app logic for all subsystems
  - **DemaConsulting.SysML2Workbench.Desktop/** - desktop platform head (Windows/Linux/macOS entry point)
- **test/** - test projects
  - **DemaConsulting.SysML2Workbench.Tests/** - unit and subsystem-level tests mirroring `src/`
  - **DemaConsulting.SysML2Workbench.UiTests/** - headless, in-process Avalonia UI tests
  - **DemaConsulting.SysML2Workbench.IntegrationTests/** - Appium-driven end-to-end tests against the compiled Desktop application
  - **OtsSoftwareTests/** - integration tests qualifying the OTS dependencies themselves

## Companion Artifact Structure

Each local software item has corresponding artifacts in parallel directory trees:

- Requirements: `docs/reqstream/{system-name}.yaml`, `docs/reqstream/{system-name}[/{subsystem-name}...]/{item}.yaml`
- Design: `docs/design/{system-name}.md`, `docs/design/{system-name}[/{subsystem-name}...]/{item}.md`
- Verification: `docs/verification/{system-name}.md`, `docs/verification/{system-name}[/{subsystem-name}...]/{item}.md`
- Source: `src/{SystemName}[/{SubsystemName}...]/{Item}.cs`
- Tests: `test/{SystemName}.Tests[/{SubsystemName}...]/{Item}Tests.cs`

OTS items have integration/usage design documentation parallel to system folders:

- Requirements: `docs/reqstream/ots/{ots-name}.yaml`
- Design: `docs/design/ots/{ots-name}.md`
- Verification: `docs/verification/ots/{ots-name}.md`

Shared Packages have integration/usage design documentation parallel to system and OTS folders:

- Requirements: `docs/reqstream/shared/{package-name}.yaml`
- Design: `docs/design/shared/{package-name}.md`
- Verification: `docs/verification/shared/{package-name}.md`

Review-sets: defined in `.reviewmark.yaml`

## References

- [SysML2Workbench releases](https://github.com/demaconsulting/SysML2Workbench/releases)
