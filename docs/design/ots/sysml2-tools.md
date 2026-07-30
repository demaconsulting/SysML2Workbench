## SysML2Tools

SysML2Workbench uses the SysML2Tools family as its model-processing engine,
relying on published packages rather than local parser or layout
implementations.

### Purpose

The system uses `DemaConsulting.SysML2Tools.Language`,
`DemaConsulting.SysML2Tools.Stdlib`, and `DemaConsulting.SysML2Tools.Core`
because they already provide the parser, semantic model, diagnostics, standard
library handling, and view-to-layout translation that the workbench needs.
Reusing these packages keeps the workbench focused on workspace orchestration
and UI behavior instead of duplicating language infrastructure.

### Features Used

- **Language parsing** — loads `.sysml` files, produces syntax and semantic
  diagnostics, and exposes the semantic workspace used by the rest of the
  application.
- **Standard library loading** — injects the packaged SysML standard library
  content so local workspaces resolve built-in language concepts consistently.
- **View discovery** — enumerates predefined view usages from the semantic
  model for the catalog UI via `DiagramRenderer.GetViewIdentities`.
- **Layout and rendering** — `DiagramRenderer.RenderWorkspace` converts a
  named view usage directly into finished SVG output in one call; the
  workbench never sees an intermediate layout graph or a public layout
  strategy registry.
- **Element querying** — `QueryEngine` runs the verb-scoped queries the Query
  dialog exposes, including connection-aware impact analysis: an opt-in that
  traverses connector (`connect`/`bind`) relationships undirected in addition
  to the resolved reference edges the impact walk follows by default. The
  library also reports per-entry traversal metadata (depth, the resolved
  relation kind that reached the entry, and the far endpoint behind a
  connection roll-up) which the dialog surfaces directly.

### Integration Pattern

SysML2Workbench consumes the SysML2Tools packages through local adapter units
rather than by letting UI code call the packages directly. WorkspaceModel uses
`GlobFileCollector`, `StdlibProvider`, and `WorkspaceLoader` during initial
load and incremental reload. ViewCatalogPresenter reads discovered view
usages from the semantic workspace via `DiagramRenderer.GetViewIdentities`.
LayoutInvoker calls `DiagramRenderer.RenderWorkspace` to render a predefined
view by name; for GUI-authored custom views (which require multiple `expose`
targets that the library's single-target `SynthesizeDynamicView` helper
cannot express), LayoutInvoker instead constructs an ephemeral
`SysmlViewNode` directly and adds it to a shallow clone of the workspace
before rendering. Initialization is limited to constructing the required
workspace and render inputs; no separate service process or background
daemon is introduced.

QueryDialog is the one place where the workbench builds a library option
object directly rather than through an adapter unit: it composes a
`QueryOptions` from the dialog's form state and hands it to `QueryEngine`,
gating each verb-specific option (hierarchy direction, impact walk depth,
impact connection-awareness) on the selected verb so no other verb's option
payload is affected. Result entries are projected once into a flattened
row record for display, reading the library's machine-readable traversal
metadata rather than parsing its human-readable detail text.
