### WorkspaceModel

![WorkspaceSubsystem Structure](WorkspaceSubsystemView.svg)

#### Purpose

WorkspaceModel is the authoritative in-memory representation of the opened
workspace, including discovered files, resolved imports, and the parse or
semantic state needed by view selection, rendering, and diagnostics.

#### Data Model

**Sources**: `IReadOnlyList<WorkspaceSource>` — the file and folder sources
that produced the currently loaded file set, as supplied by the caller's most
recent `LoadWorkspaceAsync` call.

**Files**: `IReadOnlyDictionary<string, WorkspaceFileState>` — maps normalized
file paths to the latest known parse result, semantic model, and file metadata.

**LoadOptions**: not owned by WorkspaceModel. Glob patterns and file-discovery
options are exclusively a `WorkspaceSourceSet` concern; WorkspaceModel loads
whatever merged file list a `WorkspaceSourceResolution` hands it and performs
no discovery of its own.

#### Key Methods

**LoadWorkspace**: Creates a workspace snapshot from a resolved set of
sources.

- *Parameters*: `IReadOnlyList<WorkspaceSource> sources` — the sources that
  produced the resolution, retained for display and re-resolution by callers;
  `WorkspaceSourceResolution resolution` — the merged, deduplicated file list
  (and its per-source attribution) to load.
- *Returns*: `WorkspaceSnapshot` — normalized state for the resolved file set.
- *Preconditions*: None beyond both parameters being non-null - a resolution
  with zero files (including one produced by a zero-source set) is valid.
- *Postconditions*: `Sources` and `Files` are synchronized to
  `resolution.MergedFiles`, even if some files contain diagnostics. A
  zero-file resolution produces a valid, non-throwing, stdlib-only workspace
  with no diagnostics, calling straight through to the underlying semantic
  loader rather than special-casing the empty case.

The method loads the SysML standard library inputs needed by the parser
pipeline, parses each file in `resolution.MergedFiles`, resolves imports, and
records diagnostics per file before publishing the snapshot. File discovery
itself (globbing folders, deduping overlaps) has already happened upstream in
`WorkspaceSourceSet.Resolve()` by the time this method is called.

**ReloadFiles**: Incrementally refreshes the workspace against the current or
a freshly supplied resolution.

- *Parameters*: `IReadOnlyList<string> changedPaths` — normalized files
  affected by an external change (used for logging/diagnostics context, not
  for narrowing which files are recomputed - see Error Handling);
  `WorkspaceSourceResolution? updatedResolution` — optional freshly
  recomputed resolution to adopt before reloading. When supplied, it replaces
  the resolution stored from the last `LoadWorkspaceAsync`/`ReloadFiles` call;
  when omitted, the stored resolution is reused unchanged.
- *Returns*: `WorkspaceSnapshot` — updated workspace state after
  recomputation.
- *Preconditions*: None - recomputing against an empty resolution (0 sources,
  0 files) is valid and produces the same stdlib-only snapshot as
  `LoadWorkspace`.
- *Postconditions*: Updated file entries replace stale ones; unaffected file
  entries retain their prior `LoadedUtc`. Callers that watch a folder source
  for external changes are expected to pass a freshly resolved
  `updatedResolution` (via `WorkspaceSourceSet.Resolve()`) so that a file
  created or deleted externally under a still-registered folder source is
  actually picked up - reloading against a stale, previously stored
  resolution would silently miss such changes, since discovery no longer
  happens inside `WorkspaceModel` itself.

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
surfaced to DiagnosticsAggregator. Since file discovery moved to
`WorkspaceSourceSet`, `WorkspaceModel` itself no longer touches the file
system to enumerate candidates - a missing folder source surfaces at
`WorkspaceSourceSet.AddFolder`/`Resolve` time, not here. Unrecoverable I/O
exceptions reading an individual file, or parser initialization failures, are
propagated to the caller because they prevent a coherent workspace from being
established.

#### Dependencies

- **SysML2Tools** — supplies parsing, semantic analysis, standard library
  loading, and view discovery primitives.
- **WorkspaceSubsystem** — defines the boundary within which the model is owned
  and refreshed.
- **WorkspaceSourceSet** — resolves the registered sources into the merged
  file list and per-source attribution that this unit loads.
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
