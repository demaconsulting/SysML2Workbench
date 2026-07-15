## WorkspaceSubsystem

![WorkspaceSubsystem Structure](WorkspaceSubsystemView.svg)

### Overview

WorkspaceSubsystem owns the live folder-backed model workspace. Its boundary
starts at a user-selected root folder and ends at a normalized in-memory
representation of the discovered files, resolved imports, and aggregated
diagnostics that other subsystems can safely consume. It contains
WorkspaceModel, FileWatcher, and DiagnosticsAggregator. View selection,
rendering, and window composition are outside this subsystem and consume its
outputs through in-process interfaces.

### Interfaces

**Workspace Lifecycle API**: In-process operations for opening, refreshing, and
querying the current workspace.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Accepts a root path and reload requests, returns normalized
  workspace state, and exposes file-level parse and diagnostic results to other
  subsystems.
- *Constraints*: Must present a consistent snapshot to callers and must keep
  partial reload failures localized to the affected files.

**File System Notification Stream**: The operating-system change feed for files
under the opened workspace root.

- *Type*: File system watcher.
- *Role*: Consumer.
- *Contract*: Consumes create, change, rename, and delete notifications and
  converts them into incremental reload requests.
- *Constraints*: Notifications may arrive out of order or in bursts and must be
  debounced before triggering reparses.

**Diagnostics Feed**: The subsystem output used by UI consumers to display
current workspace health.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Exposes an aggregated list of `SysmlDiagnostic` instances plus
  enough file context for display and future navigation features.
- *Constraints*: Must remain available even when the workspace contains syntax
  or reference-resolution failures.

### Design

1. WorkspaceModel performs initial discovery, tracks the known file set, and
   stores per-file parse and semantic state.
2. FileWatcher subscribes to operating-system notifications for the opened root
   and normalizes noisy change bursts into discrete reload requests.
3. WorkspaceModel reparses only the affected files, re-evaluates import
   relationships, and publishes the updated file state.
4. DiagnosticsAggregator collects the per-file `SysmlDiagnostic` results and
   publishes a stable workspace-wide view for AppShellSubsystem and
   DiagnosticsPanelSubsystem.
5. The subsystem reports workspace changes as structured state rather than by
   directly manipulating UI controls, keeping presentation concerns outside the
   boundary.
