# SysML2Workbench Architecture

## Purpose

SysML2Workbench is a cross-platform desktop application for viewing SysML v2
textual models. It renders the same nested block diagrams (General,
Interconnection, State Transition, Action Flow, Sequence, and Grid views)
produced by the SysML2Tools CLI, in an interactive windowed viewer, and adds
the ability to construct custom, ad-hoc views through a GUI rather than
hand-written SysML syntax. It is aimed at individual SysML v2 modelers and at
teams collaborating on models stored in local git repositories (the
application itself has no knowledge of git).

## Scope

**Included (Phase 0 — read-only viewer):**

- Opening a folder as a multi-file workspace, with glob-based file discovery
  and `import` resolution mirroring the existing CLI behavior.
- Live file-system watching of the workspace, with incremental reload and
  re-render when files change externally (e.g., another editor, `git pull`).
- Browsing and rendering all predefined view kinds defined in the model.
- A GUI-driven custom view builder: choose a view type, multi-select target
  elements/packages (SysML v2 `expose` semantics support multiple exposed
  elements per view), and apply a filter expression, rendered live.
- Generating copy-pasteable SysML `view ... expose ...` syntax from a
  GUI-built custom view, so it can be promoted into a permanent model file.
- Pan/zoom navigation over rendered SVG diagrams.
- A diagnostics panel listing parser and reference-resolution diagnostics
  (`SysmlDiagnostic`) for the whole workspace.
- A local rolling log file for user-attachable bug reports.

**Explicitly excluded (deferred to future phases/sessions):**

- Any git awareness or version-control operations within the app.
- Click-to-navigate between diagnostics/diagram and source text.
- Live text editing (AvaloniaEdit-based) of `.sysml` files.
- Structural/graphical editing with round-trip back to concrete syntax.
- Telemetry/crash reporting and authentication/authorization (no
  network/multi-user surface exists).
- Performance engineering for very large models — layout and rendering
  performance is the responsibility of the underlying SysML2Tools and
  DemaConsulting.Rendering libraries, not this system.

## Technology Stack

- **Language/Framework**: C#, .NET, Avalonia 12 (UI), xUnit v3 (tests).
- **Rendering**: DemaConsulting.Rendering (SVG output), displayed via an
  Avalonia SVG-rendering control — no native `DrawingContext` renderer at
  this stage, in order to reuse the existing, well-tested rendering library
  as-is.
- **Model/Layout engine**: DemaConsulting.SysML2Tools.Language (parser,
  semantic model, diagnostics), DemaConsulting.SysML2Tools.Stdlib (embedded
  standard library), DemaConsulting.SysML2Tools.Core (view strategies /
  `LayoutGraph` layout engine).
- **Distribution/infrastructure**: desktop-only (Windows/Linux/macOS); no
  cloud or server infrastructure. Packaging model (self-contained vs.
  framework-dependent) is undecided (see Open Concerns).
- **Project structure**: shared UI project (`DemaConsulting.SysML2Workbench`)
  plus a desktop platform head (`DemaConsulting.SysML2Workbench.Desktop`),
  keeping the door open for a future Avalonia Browser (WASM) head without
  restructuring, even though no such head is planned yet.

## Software Structure

```text
SysML2Workbench (System)
├── WorkspaceSubsystem — folder-based workspace, live file watching, glob/import resolution
│   ├── WorkspaceModel (Unit) — in-memory folder/file tree + per-file parse state
│   ├── FileWatcher (Unit) — detects external file changes, triggers incremental reload
│   └── DiagnosticsAggregator (Unit) — collects SysmlDiagnostic across the workspace
├── ViewCatalogSubsystem — renders the six predefined view kinds
│   └── ViewCatalogPresenter (Unit) — lists/selects available views defined in the model
├── ViewBuilderSubsystem — GUI for constructing custom views
│   ├── ViewDefinitionModel (Unit) — view type + multi-target expose selection + filter expr (session-only)
│   └── SysmlSnippetGenerator (Unit) — emits copy-pasteable `view ... expose ...` SysML text
├── LayoutRenderingSubsystem — turns a selected view into pixels
│   ├── LayoutInvoker (Unit) — wraps SysML2Tools.Core view strategies → LayoutGraph
│   └── SvgCanvasHost (Unit) — DemaConsulting.Rendering SVG output hosted in Avalonia SVG control, pan/zoom
├── DiagnosticsPanelSubsystem — UI list of diagnostics (structured for future click-to-navigate)
│   └── DiagnosticsListView (Unit)
├── LoggingSubsystem
│   └── RollingFileLogger (Unit)
└── AppShellSubsystem — window, navigation, tabbed views
    ├── MainWindowShell (Unit)
    └── Desktop (platform head / entry point project)
```

## Architectural Decisions

- **Thin shell over SysML2Tools**: SysML2Workbench does not reimplement
  parsing, semantic resolution, or layout — it is purely a consumer of the
  already-published SysML2Tools and DemaConsulting.Rendering packages. This
  keeps the engine independently testable and lets Workbench focus on
  UI/UX.
- **Shared UI project + platform head split**: chosen even though only a
  desktop head exists today, to keep a future Avalonia Browser (WASM) head
  possible without restructuring the codebase.
- **SVG rendering via Avalonia SVG control** (not a native `DrawingContext`
  renderer): reuses the mature, well-tested DemaConsulting.Rendering
  library as-is, trading some interactivity/hit-testing sophistication for
  significantly less new code and risk.
- **Folder/workspace model from day one, not single-file**: mirrors the
  CLI's glob-based multi-file input model, since real SysML v2 models are
  rarely single-file.
- **Live file-system watching, not one-time glob snapshot**: the workspace
  model must represent folders/files as a live, watched structure so
  external changes (another editor, `git pull`) trigger incremental
  reload/re-render, rather than requiring a manual re-open.
- **Custom views are ephemeral, with text export as the persistence path**:
  GUI-built custom views are not saved as their own file format; instead,
  the system generates copy-pasteable SysML `view ... expose ...` syntax so
  users can promote a view into a permanent model file using standard SysML
  v2 syntax. This avoids introducing a second, tool-specific view
  definition format alongside the real language.
- **Multi-target expose selection**: since SysML v2 views support multiple
  `expose` statements (including sets/collections and renaming via `as`),
  the ViewBuilder GUI allows multi-selecting several elements/packages as
  targets for one custom view, matching the language's actual capability.
- **Performance is delegated**: model/diagram scale and rendering
  performance are treated as a concern of the SysML2Tools and
  DemaConsulting.Rendering libraries; Workbench is not architected around
  large-model performance engineering.
- **No git awareness**: despite team usage being via local git repositories,
  the application has no git integration at this time — it only reads/
  watches the local file system.
- **No security/auth surface**: as a local desktop app operating solely on
  local files with no network or multi-user surface, no authentication,
  authorization, or untrusted-input hardening is architected for at this
  time.
- **Extensibility for later phases**: subsystem boundaries (particularly
  WorkspaceSubsystem/DiagnosticsAggregator and DiagnosticsPanelSubsystem)
  are structured so that future phases — click-to-navigate diagnostics,
  AvaloniaEdit-based text editing, and structural/graphical editing — can
  be added without a major restructure, even though they are out of scope
  now.

## Open Concerns

1. 🟢 **LOW** Distribution model: self-contained single-file executables
   per OS vs. framework-dependent deployment (requiring a shared .NET
   runtime) is undecided. Low architectural risk either way, but affects
   installer/packaging tooling choice and should be settled before release
   packaging work begins.
