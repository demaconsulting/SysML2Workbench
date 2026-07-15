# Introduction

This document defines the planned verification design for SysML2Workbench. Phase 0 does not
include a checked-in automated test project yet, so this collection records the intended test
strategy, target test locations, and acceptance evidence for each software item before the
verification code is implemented.

## Purpose

This document describes how SysML2Workbench will be verified at the system, subsystem, unit,
and OTS-integration levels. It gives reviewers a single place to confirm the intended test
scope, test boundaries, and evidence model that will be used to demonstrate requirement
coverage and readiness for formal review.

## Scope

Local items:

- **SysML2Workbench**: planned system verification for the Phase 0 read-only desktop viewer.
- **WorkspaceSubsystem, ViewCatalogSubsystem, ViewBuilderSubsystem, LayoutRenderingSubsystem,
  DiagnosticsPanelSubsystem, LoggingSubsystem, and AppShellSubsystem**: planned subsystem
  integration verification.
- **WorkspaceModel, FileWatcher, DiagnosticsAggregator, ViewCatalogPresenter,
  ViewDefinitionModel, SysmlSnippetGenerator, LayoutInvoker, SvgCanvasHost,
  DiagnosticsListView, RollingFileLogger, and MainWindowShell**: planned unit verification.

OTS items:

- **SysML2Tools**: planned integration verification of parsing, semantic resolution, and view
  layout services used by SysML2Workbench.
- **Rendering**: planned integration verification of SVG generation and rendering primitives.
- **Avalonia**: planned integration verification of the desktop UI framework and headless UI
  test host behavior.
- **XUnit**: planned verification of the repository test harness configuration and execution
  behavior.

Out of scope: the test projects themselves as software items, build-pipeline configuration,
future editing features outside Phase 0, and the internal implementation details of OTS
software items.

## Folder Layout

The repository does not yet contain the planned verification projects. When implemented, the
verification code will follow this structure:

- **test/** - planned root for automated verification projects and shared test assets
  - **DemaConsulting.SysML2Workbench.Tests/** - planned xUnit v3 project for system,
    subsystem, and unit verification of local software items
  - **OtsSoftwareTests/** - planned repo-level xUnit v3 project for OTS integration tests
    when vendor evidence alone is insufficient

## Companion Artifact Structure

Local items have parallel artifacts in:

- Requirements: `docs/reqstream/{system-name}.yaml`,
  `docs/reqstream/{system-name}[/{subsystem-name}...]/{item}.yaml`
- Design: `docs/design/{system-name}.md`,
  `docs/design/{system-name}[/{subsystem-name}...]/{item}.md`
- Verification: `docs/verification/{system-name}.md`,
  `docs/verification/{system-name}[/{subsystem-name}...]/{item}.md`
- Source: `src/{SystemName}[/{SubsystemName}...]/{Item}.cs`
- Tests: `test/{SystemName}.Tests[/{SubsystemName}...]/{Item}Tests.cs`

OTS items have integration and usage artifacts parallel to system folders:

- Requirements: `docs/reqstream/ots/{ots-name}.yaml`
- Design: `docs/design/ots/{ots-name}.md`
- Verification: `docs/verification/ots/{ots-name}.md`
- Tests: `test/OtsSoftwareTests/{OtsName}Tests.cs`

Review sets are defined in `.reviewmark.yaml`.

## References

- [IEC 62304:2006+AMD1:2015 Medical device software - Software life cycle processes](https://webstore.iec.ch/en/publication/22762)
- [SysML2Workbench releases](https://github.com/demaconsulting/SysML2Workbench/releases)
