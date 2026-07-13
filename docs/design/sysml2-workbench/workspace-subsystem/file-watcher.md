### FileWatcher

![WorkspaceSubsystem Structure](WorkspaceSubsystemView.svg)

#### Purpose

FileWatcher detects external workspace file-system changes and converts them
into bounded incremental reload requests so the workbench stays synchronized
with edits made outside the application.

#### Data Model

**WatchedRootPath**: `string` — workspace root currently monitored for external
changes.

**Watcher**: `FileSystemWatcher` — operating-system watcher configured for
recursive observation of the workspace root.

**DebounceWindow**: `TimeSpan` — minimum delay used to merge rapid change
bursts into a single reload batch.

**PendingChanges**: `IReadOnlySet<string>` — normalized paths queued for the
next incremental refresh cycle.

#### Key Methods

**StartWatching**: Begins monitoring the current workspace root.

- *Parameters*: `string rootPath` — folder to observe.
- *Returns*: `void` — watcher state is updated in place.
- *Preconditions*: `rootPath` exists and no incompatible watcher is currently
  active.
- *Postconditions*: File-system notifications are subscribed and queued into the
  local debounce buffer.

**QueueChange**: Records a changed path from an operating-system event.

- *Parameters*: `string path` — file or folder path reported by the platform.
- *Returns*: `void` — the change is added to the pending set.
- *Preconditions*: Monitoring has already started.
- *Postconditions*: Duplicate or bursty notifications for the same path
  collapse into a single pending change entry.

**FlushPendingChanges**: Dispatches the current batch to the workspace reload
pipeline.

- *Parameters*: `None` — operates on the accumulated pending changes.
- *Returns*: `IReadOnlyList<string>` — normalized paths requiring reload.
- *Preconditions*: Monitoring has started.
- *Postconditions*: Returned paths are removed from `PendingChanges` and are
  ready for `WorkspaceModel.ReloadFiles`.

#### Error Handling

FileWatcher handles duplicate, missing, and transiently locked files locally by
retaining the change notification and letting a later reload attempt decide
whether the workspace can be refreshed. Platform-level watcher startup failures
or disposal errors are propagated because they prevent the subsystem from
honoring the live-reload design. Logging of watcher faults is best-effort and
does not replace propagation to the shell.

#### Dependencies

- **WorkspaceModel** — applies the incremental reload once a stable change batch
  is available.
- **RollingFileLogger** — records watcher startup failures and repeated reload
  faults.
- **Avalonia** — provides dispatcher integration when reload notifications must
  be marshaled back to the UI thread.
- **WorkspaceSubsystem** — owns lifecycle and configuration for the watcher.

#### Callers

- **WorkspaceSubsystem**
- **MainWindowShell**
