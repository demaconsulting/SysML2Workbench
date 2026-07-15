### WorkspaceModel

![WorkspaceSubsystem Structure](WorkspaceSubsystemView.svg)

#### Purpose

WorkspaceModel is the authoritative in-memory representation of the opened
workspace, including discovered files, resolved imports, and the parse or
semantic state needed by view selection, rendering, and diagnostics.

#### Data Model

**RootPath**: `string` — absolute path to the workspace folder currently
loaded.

**Files**: `IReadOnlyDictionary<string, WorkspaceFileState>` — maps normalized
file paths to the latest known parse result, semantic model, and file metadata.

**ImportGraph**: `IReadOnlyDictionary<string, IReadOnlyList<string>>` — tracks
the direct import relationships between files so incremental reload can
invalidate only the affected dependents.

**LoadOptions**: `WorkspaceLoadOptions` — captures glob patterns, standard
library inclusion, and reload behavior that must remain consistent for the life
of the loaded workspace.

#### Key Methods

**LoadWorkspace**: Creates a workspace snapshot from a root folder.

- *Parameters*: `string rootPath` — folder to scan for `.sysml` content.
- *Returns*: `WorkspaceSnapshot` — normalized state for the full discovered
  workspace.
- *Preconditions*: `rootPath` exists and is accessible to the current process.
- *Postconditions*: `RootPath`, `Files`, and `ImportGraph` are synchronized to
  the discovered content, even if some files contain diagnostics.

The method discovers candidate files, loads the SysML standard library inputs
needed by the parser pipeline, parses each file, resolves imports, and records
diagnostics per file before publishing the initial snapshot.

**ReloadFiles**: Incrementally refreshes a subset of files.

- *Parameters*: `IReadOnlyList<string> changedPaths` — normalized files
  affected by an external change.
- *Returns*: `WorkspaceSnapshot` — updated workspace state after recomputation.
- *Preconditions*: A workspace has already been loaded and the paths belong to
  the current root or to known imported files.
- *Postconditions*: Updated file entries replace stale ones and impacted import
  relationships are recomputed.

**GetSemanticWorkspace**: Returns the current semantic view for downstream
consumers.

- *Parameters*: `None` — uses the currently loaded state.
- *Returns*: `SemanticWorkspace` — the workspace representation consumed by
  view and layout operations.
- *Preconditions*: A workspace has been loaded.
- *Postconditions*: Callers receive a read-only snapshot that remains
  internally consistent for the duration of their operation.

#### Error Handling

WorkspaceModel treats malformed files, unresolved imports, and semantic
failures as diagnostic-producing states rather than fatal conditions. Invalid
file contents are captured inside the affected `WorkspaceFileState` and
surfaced to DiagnosticsAggregator. Root-folder access failures, unrecoverable
I/O exceptions, or parser initialization failures are propagated to the caller
because they prevent a coherent workspace from being established.

#### Dependencies

- **SysML2Tools** — supplies parsing, semantic analysis, standard library
  loading, and view discovery primitives.
- **WorkspaceSubsystem** — defines the boundary within which the model is owned
  and refreshed.
- **FileWatcher** — supplies the changed file set that drives incremental
  reload.
- **DiagnosticsAggregator** — consumes the per-file diagnostics emitted by the
  model.

#### Callers

- **WorkspaceSubsystem**
- **FileWatcher**
- **ViewCatalogPresenter**
- **ViewDefinitionModel**
- **LayoutInvoker**
- **DiagnosticsAggregator**
- **MainWindowShell**
