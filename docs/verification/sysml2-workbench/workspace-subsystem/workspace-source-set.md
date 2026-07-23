### WorkspaceSourceSet

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/WorkspaceSourceSetTests.cs` exercise
`WorkspaceSourceSet` directly. The suite adds and removes file and folder sources against temporary directories and
asserts the resulting `Sources` list and `Resolve()` output. The scenario list below follows the authoritative
mappings in `docs/reqstream/sysml2-workbench/workspace-subsystem/workspace-source-set.yaml` and describes the
implemented tests in present tense.

#### Test Environment

Tests run under the standard .NET test runner with temporary folders and files created per test. No external
services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/WorkspaceSourceSetTests.cs`
  that correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/workspace-subsystem/workspace-source-set.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**AddFile_SamePathTwice_ReturnsSameSourceAndDoesNotDuplicate**: Adding the same normalized file path twice returns
the original source instance both times and does not add a second entry to `Sources`. Verified by
`WorkspaceSourceSetTests.AddFile_SamePathTwice_ReturnsSameSourceAndDoesNotDuplicate`.

**AddFolder_SamePathTwice_ReturnsSameSourceAndDoesNotDuplicate**: Adding the same normalized folder path twice
returns the original source instance both times and does not add a second entry to `Sources`. Verified by
`WorkspaceSourceSetTests.AddFolder_SamePathTwice_ReturnsSameSourceAndDoesNotDuplicate`.

**AddFolder_MissingFolder_ThrowsDirectoryNotFoundException**: Adding a folder path that does not exist throws
`DirectoryNotFoundException` rather than silently registering an unusable source. Verified by
`WorkspaceSourceSetTests.AddFolder_MissingFolder_ThrowsDirectoryNotFoundException`.

**Sources_PreservesRegistrationOrder**: `Sources` reflects sources in the exact order they were registered,
regardless of kind. Verified by `WorkspaceSourceSetTests.Sources_PreservesRegistrationOrder`.

**RemoveSource_RegisteredThenUnknownId_ReturnsTrueThenFalse**: Removing a registered source id returns `true` and
drops it from `Sources`; removing that same id again, or any unknown id, returns `false` without throwing.
Verified by `WorkspaceSourceSetTests.RemoveSource_RegisteredThenUnknownId_ReturnsTrueThenFalse`.

**ClearSources_WithRegisteredSources_RemovesAllAndResolveReturnsEmpty**: Calling `ClearSources` after registering
both a file and a folder source empties `Sources`, and a subsequent `Resolve()` produces empty `MergedFiles` and
attribution maps. Verified by
`WorkspaceSourceSetTests.ClearSources_WithRegisteredSources_RemovesAllAndResolveReturnsEmpty`.

**Resolve_ZeroSources_ReturnsEmptyResolution**: Resolving a `WorkspaceSourceSet` with zero registered sources
produces an empty `MergedFiles` list and empty attribution maps, with no exception thrown. Verified by
`WorkspaceSourceSetTests.Resolve_ZeroSources_ReturnsEmptyResolution`.

**Resolve_FolderSource_DiscoversAllSysmlFilesRecursively**: Resolving a folder source discovers every `.sysml` file
nested under it, including files in subdirectories, using the default glob options. Verified by
`WorkspaceSourceSetTests.Resolve_FolderSource_DiscoversAllSysmlFilesRecursively`.

**Resolve_FileOverlappingFolder_DedupesAndFirstRegisteredSourceWinsAttribution**: When a file explicitly added as a
`File` source also falls under a registered `Folder` source, resolving the set contains that file exactly once in
`MergedFiles`, attributed in `FileToSourceId` to whichever of the two sources was registered first. Verified by
`WorkspaceSourceSetTests.Resolve_FileOverlappingFolder_DedupesAndFirstRegisteredSourceWinsAttribution`.

**Resolve_NestedFolderOverlap_DedupesAndFirstRegisteredSourceWinsAttribution**: When two registered folder sources
overlap (one nested inside the other), resolving the set contains each shared file exactly once, attributed to
whichever folder source was registered first. Verified by
`WorkspaceSourceSetTests.Resolve_NestedFolderOverlap_DedupesAndFirstRegisteredSourceWinsAttribution`.
