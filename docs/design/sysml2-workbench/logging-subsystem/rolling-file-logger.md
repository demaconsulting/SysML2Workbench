### RollingFileLogger

![LoggingSubsystem Structure](LoggingSubsystemView.svg)

#### Purpose

RollingFileLogger writes application events and failure details to a bounded
set of local files so users can attach recent logs to bug reports without
unbounded disk growth.

#### Data Model

**LogDirectory**: `string` — folder containing the active and archived log
files.

**BaseFileName**: `string` — stable prefix used to name each rotated log file.

**MaximumFileSizeBytes**: `long` — threshold that triggers rotation of the
active log.

**RetainedFileCount**: `int` — number of historical log files preserved before
older files are deleted.

#### Key Methods

**Log**: Appends one event to the active log file.

- *Parameters*: `LogLevel level` — severity; `string message` — event text;
  `Exception? exception` — optional failure context.
- *Returns*: `void` — the event is written or an error is propagated.
- *Preconditions*: Logging has been initialized with a writable directory.
- *Postconditions*: The event is durably appended to the active file or a
  failure is reported to the caller.

**RotateIfNeeded**: Ensures the active file obeys the retention policy.

- *Parameters*: `None` — inspects current file state.
- *Returns*: `void` — file set is updated in place.
- *Preconditions*: The active file path is known.
- *Postconditions*: File sizes remain within policy and stale files beyond
  retention are removed.

**Flush**: Forces buffered output to disk.

- *Parameters*: `None` — operates on the current writer.
- *Returns*: `void` — buffered content is synchronized.
- *Preconditions*: Logging has been initialized.
- *Postconditions*: Previously accepted log entries are committed to the file
  system.

#### Error Handling

RollingFileLogger handles transient file contention and directory-creation
races locally when a retry can succeed without reordering records. Persistent
write failures, permission errors, or rotation failures are propagated because
the caller may need to notify the user that diagnostic evidence could not be
captured. The logger never drops exceptions silently.

#### Dependencies

- **LoggingSubsystem** — owns lifecycle and policy configuration for the
  logger.
- **MainWindowShell** — may surface the log location to users.
- **WorkspaceSubsystem** — reports watcher or reload failures for persistence.
- **System.IO** — provides the local file system primitives used for append and
  rotation.

#### Callers

- **WorkspaceSubsystem**
- **ViewCatalogSubsystem**
- **ViewBuilderSubsystem**
- **LayoutRenderingSubsystem**
- **DiagnosticsPanelSubsystem**
- **AppShellSubsystem**
