## WorkspaceSubsystem

![WorkspaceSubsystem Structure](WorkspaceSubsystemView.svg)

### Overview

WorkspaceSubsystem owns the live folder-and-file-backed model workspace. Its
boundary starts at an ordered set of user-added file and folder sources and
ends at a normalized in-memory representation of the merged, deduplicated
files, resolved imports, and aggregated diagnostics that other subsystems can
safely consume. It contains WorkspaceSourceSet, WorkspaceModel, FileWatcher,
and DiagnosticsAggregator. View selection, rendering, and window composition
are outside this subsystem and consume its outputs through in-process
interfaces.

### Interfaces

**Workspace Lifecycle API**: In-process operations for opening, refreshing, and
querying the current workspace.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Accepts additions and removals of individual file/folder
  sources, resolves them into a merged file set, reloads on request, and
  exposes file-level parse and diagnostic results to other subsystems. A
  zero-source workspace is a valid, non-error state rather than a precondition
  failure.
- *Constraints*: Must present a consistent snapshot to callers and must keep
  partial reload failures localized to the affected files.

**File System Notification Stream**: The operating-system change feed for files
under the opened workspace sources.

- *Type*: File system watcher.
- *Role*: Consumer.
- *Contract*: Consumes create, change, rename, and delete notifications for
  every currently watched source (one recursive watcher per folder source,
  one filtered watcher per file source) and converts them into incremental
  reload requests.
- *Constraints*: Notifications may arrive out of order or in bursts and must be
  debounced before triggering reparses; a change under one source must never
  be attributed to another watched source.

**Diagnostics Feed**: The subsystem output used by UI consumers to display
current workspace health.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Exposes an aggregated list of `SysmlDiagnostic` instances plus
  enough file context for display and future navigation features.
- *Constraints*: Must remain available even when the workspace contains syntax
  or reference-resolution failures, and even when zero sources are open.

### Design

1. WorkspaceSourceSet maintains the ordered list of file and folder sources the
   user has added, and resolves them on demand into a merged, deduplicated
   file list with per-file and per-source attribution.
2. WorkspaceModel loads that resolution, tracks the known file set, and stores
   per-file parse and semantic state.
3. FileWatcher subscribes to operating-system notifications scoped to each
   individually watched source and normalizes noisy change bursts into
   discrete reload requests.
4. WorkspaceModel reparses against a freshly re-resolved file set, re-evaluates
   import relationships, and publishes the updated file state.
5. DiagnosticsAggregator collects the per-file `SysmlDiagnostic` results and
   publishes a stable workspace-wide view for AppShellSubsystem and
   DiagnosticsPanelSubsystem.
6. The subsystem reports workspace changes as structured state rather than by
   directly manipulating UI controls, keeping presentation concerns outside the
   boundary.
