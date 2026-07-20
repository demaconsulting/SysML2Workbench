### FileWatcher

![WorkspaceSubsystem Structure](WorkspaceSubsystemView.svg)

#### Purpose

FileWatcher detects external workspace file-system changes and converts them
into bounded incremental reload requests so the workbench stays synchronized
with edits made outside the application.

#### Data Model

**WatchedSourceIds**: `IReadOnlySet<string>` — identifiers of every
`WorkspaceSource` currently monitored for external changes.

**Watchers**: `Dictionary<string, FileSystemWatcher>` — one operating-system
watcher per watched source, keyed by source id. A `Folder`-kind source gets a
single recursive watcher rooted at the folder; a `File`-kind source gets a
single non-recursive watcher rooted at the file's containing directory, with
its `Filter` set to that file's name so only changes to that specific file are
observed.

**DebounceWindow**: `TimeSpan` — minimum delay used to merge rapid change
bursts, across all active watchers, into a single reload batch.

**PendingChanges**: `IReadOnlySet<string>` — normalized paths queued for the
next incremental refresh cycle, merged across every currently watched source.

#### Key Methods

**WatchSource**: Begins monitoring the given source, retargeting an
already-watched source with the same id if one exists.

- *Parameters*: `WorkspaceSource source` — the file or folder source to
  observe.
- *Returns*: `void` — watcher state is updated in place.
- *Preconditions*: The source's folder (or, for a file source, its containing
  directory) exists.
- *Postconditions*: A dedicated `FileSystemWatcher` for `source.Id` is created
  and subscribed - recursive for a `Folder` source, filtered to the file's
  name for a `File` source. If a watcher already existed for this exact
  `source.Id`, it is disposed and replaced, but other still-watched sources'
  watchers and pending change state are left untouched - unlike a global
  retarget, watching or re-watching one source never disturbs any other
  independently watched source.

**UnwatchSource**: Stops monitoring a previously watched source.

- *Parameters*: `string sourceId` — identifier of the source to stop watching.
- *Returns*: `bool` — whether a matching watcher was found and disposed.
- *Postconditions*: The matching watcher, if any, is disposed and removed from
  `WatchedSourceIds`. Unlike the old single-watcher `StartWatching` retarget
  behavior, this does **not** clear `PendingChanges` - pending changes queued
  by other still-watched sources must survive an unrelated source being
  removed.

**QueueChange**: Records a changed path from an operating-system event.

- *Parameters*: `string path` — file or folder path reported by the platform.
- *Returns*: `void` — the change is added to the pending set.
- *Preconditions*: At least one source is currently watched.
- *Postconditions*: Duplicate or bursty notifications for the same path
  collapse into a single pending change entry, regardless of which watched
  source's watcher raised the underlying event.

**FlushPendingChanges**: Dispatches the current batch to the workspace reload
pipeline.

- *Parameters*: `None` — operates on the accumulated pending changes.
- *Returns*: `IReadOnlyList<string>` — normalized paths requiring reload,
  merged across every watcher that reported a change since the last flush.
- *Preconditions*: Monitoring has started for at least one source.
- *Postconditions*: Returned paths are removed from `PendingChanges` and are
  ready for `WorkspaceModel.ReloadFiles`.

#### Error Handling

FileWatcher handles duplicate, missing, and transiently locked files locally by
retaining the change notification and letting a later reload attempt decide
whether the workspace can be refreshed. Platform-level watcher startup failures
or disposal errors are propagated because they prevent the subsystem from
honoring the live-reload design. A change reported for one watched source is
never merged into or attributed to another watched source's isolation
boundary - each `FileSystemWatcher` only reports events from its own root
(and, for file sources, its own `Filter`). Logging of watcher faults is
best-effort and does not replace propagation to the shell.

Every `FileSystemWatcher` raises its `Changed`/`Created`/`Deleted`/`Renamed`
events on an operating-system callback thread, independent of whatever thread
calls `WatchSource`/`UnwatchSource`/`FlushPendingChanges`/`PendingChanges`.
Production usage marshals those callbacks onto the UI thread first, making
this uncontended in practice, but a caller that supplies (or defaults to) an
immediate, non-marshaling dispatcher runs them synchronously on the OS
callback thread itself. `FileWatcher` therefore guards all reads and writes of
its internal pending-changes and watcher-registry state with a single
internal lock, so a change notification racing a concurrent call from another
thread can never corrupt that state.

#### Dependencies

- **WorkspaceModel** — applies the incremental reload once a stable change batch
  is available.
- **WorkspaceSourceSet** — supplies the `WorkspaceSource` instances that define
  each watcher's scope.
- **RollingFileLogger** — records watcher startup failures and repeated reload
  faults.
- **Avalonia** — provides dispatcher integration when reload notifications must
  be marshaled back to the UI thread.
- **WorkspaceSubsystem** — owns lifecycle and configuration for the watcher.

#### Callers

- **WorkspaceSubsystem**
- **MainWindowShell**
