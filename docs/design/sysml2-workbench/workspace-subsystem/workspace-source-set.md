### WorkspaceSourceSet

![WorkspaceSubsystem Structure](WorkspaceSubsystemView.svg)

#### Purpose

WorkspaceSourceSet owns the ordered collection of file and folder sources the
user has added to the workspace and resolves them into the merged, deduplicated
file list and per-source attribution that WorkspaceModel loads and the
Workspace panel displays as a tree.

#### Data Model

**Sources**: `IReadOnlyList<WorkspaceSource>` — ordered, as added, file/folder
sources currently registered with the workspace.

**WorkspaceSource**: `record(string Id, WorkspaceSourceKind Kind, string Path)`
— a single registered source. `Id` is a stable `Guid`-derived string that
survives reorder/refresh and is used as the file-watcher key and the Workspace
panel's tree-node key. `Path` is absolute (via `Path.GetFullPath`) and further
corrected, segment by segment, to its actual on-disk casing (see `AddFile`/
`AddFolder`), so every identity/dedupe comparison elsewhere in this unit can
safely use an ordinal (case-sensitive) comparison.

**WorkspaceSourceKind**: `enum { File, Folder }` — distinguishes a single-file
source from a recursively-globbed folder source.

**WorkspaceLoadOptions**: `record(IReadOnlyList<string> GlobPatterns, IReadOnlyList<string> FileExtensions)`
— glob patterns and extension filters applied uniformly to every `Folder`
source's discovery pass. `WorkspaceLoadOptions.Default` is `(["**/*.sysml"], [])`
and is not configurable per source.

**WorkspaceSourceResolution**: `record(IReadOnlyList<string> MergedFiles, IReadOnlyDictionary<string, string> FileToSourceId, IReadOnlyDictionary<string, IReadOnlyList<string>> SourceIdToFiles)`
— the deduplicated union of every source's files (`MergedFiles`), the owning
source id per file (`FileToSourceId`, first-registered-source-wins on
overlap), and the per-source file list used to build the Workspace panel's
tree (`SourceIdToFiles`; a `File` source maps to a singleton list containing
itself, a `Folder` source maps to its discovered files).

#### Key Methods

**AddFile**: Registers a single file as a workspace source.

- *Parameters*: `string path` — file path to add.
- *Returns*: `WorkspaceSource` — the newly created source, or the existing
  `File`-kind source if the exact same normalized path is already registered.
- *Preconditions*: None — existence is not enforced at registration time.
- *Postconditions*: Idempotent: adding the same normalized path twice returns
  the original source rather than creating a duplicate entry. Path identity is
  compared ordinally (case-sensitively) against each source's already-corrected
  on-disk casing, so this is correct on both case-insensitive filesystems
  (Windows, default macOS) and case-sensitive ones (Linux, case-sensitive
  APFS) alike.

**AddFolder**: Registers a folder as a workspace source.

- *Parameters*: `string path` — folder path to add.
- *Returns*: `WorkspaceSource` — the newly created source, or the existing
  `Folder`-kind source if the exact same normalized path is already
  registered.
- *Preconditions*: `path` exists as a directory.
- *Postconditions*: Idempotent, identically to `AddFile`. Throws
  `DirectoryNotFoundException` if `path` does not exist.

**RemoveSource**: Removes a previously registered source.

- *Parameters*: `string sourceId` — id of the source to remove.
- *Returns*: `bool` — whether a matching source was found and removed.
- *Postconditions*: `Sources` no longer contains an entry with `sourceId`.
  Removing an unknown id is a no-op that returns `false` rather than
  throwing.

**Resolve**: Computes the merged, deduplicated file resolution across every
registered source.

- *Parameters*: `None` — operates on the current `Sources`.
- *Returns*: `WorkspaceSourceResolution` — merged files, per-file attribution,
  and per-source file lists.
- *Postconditions*: Every `Folder` source is globbed with
  `WorkspaceLoadOptions.Default`; every `File` source contributes itself.
  Files are unioned in source-registration order; on overlap (a file inside a
  registered folder, or a folder nested inside another registered folder) the
  first-registered source silently wins attribution in `FileToSourceId` — no
  error and no visible "overlap" flag is produced. File identity for both
  `FileToSourceId` and `SourceIdToFiles` keys is compared ordinally
  (case-sensitively), matching each discovered file's actual on-disk casing
  (as `GlobFileCollector` and the already-corrected `WorkspaceSource.Path`
  both preserve it), so two files differing only by case on a case-sensitive
  filesystem are never conflated. A zero-source set resolves
  to `MergedFiles = []` and empty maps; this is a valid, non-error result.

#### Error Handling

WorkspaceSourceSet does not treat overlapping sources as an error condition:
overlap is deduplicated silently and attributed to whichever source was
registered first, which is a display-only tie-break rather than a workspace
fault. `AddFolder` propagates `DirectoryNotFoundException` for a missing
folder because a source that can never resolve any files is a caller mistake
that should be visible immediately rather than silently producing an empty
source. `AddFile` does not perform existence checks, since a `File` source
representing a not-yet-created file is a legitimate, if unusual, registration
that simply contributes zero files until the file appears.

#### Dependencies

- **SysML2Tools** — supplies `GlobFileCollector`, used to discover files under
  each `Folder` source with `WorkspaceLoadOptions.Default`.
- **WorkspaceModel** — consumes `Resolve()`'s output as the file list to load.

#### Callers

- **MainWindowShell** — owns the single `WorkspaceSourceSet` instance, mutates
  it via Add/Remove, and re-resolves it before every `WorkspaceModel` load or
  reload and before rebuilding the Workspace panel's per-source file lists.
