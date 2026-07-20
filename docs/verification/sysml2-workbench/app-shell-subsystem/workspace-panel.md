### WorkspacePanel

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/WorkspacePanelToolViewModelTests.cs` exercise
`WorkspacePanelToolViewModel` against a real `MainWindowShell` harness (matching the shell test project's
`CreateShell()` pattern), driving actual `AddFileSourceAsync`/`AddFolderSourceAsync`/`RemoveSourceAsync` calls rather
than mocks, so tree rebuilds and command wiring are verified end-to-end. The scenario list below follows the
authoritative mappings in `docs/reqstream/sysml2-workbench/app-shell-subsystem/workspace-panel.yaml` and describes
the implemented tests in present tense. Drag-and-drop acceptance in `WorkspacePanelToolView`'s code-behind and
`MainWindowView`'s code-behind is not independently unit-tested: both handlers only branch on `File.Exists`/
`Directory.Exists` for each dropped path and then call the exact same `MainWindowShell.AddFileSourceAsync`/
`AddFolderSourceAsync` methods already covered by `MainWindowShellTests`, so drag-and-drop is verified by code
review rather than a dedicated Avalonia UI-automation test.

#### Test Environment

Tests run under the standard .NET test runner with a real `MainWindowShell` constructed over temporary workspace
folders and files, plus real collaborator units. No external services are required. Drag-and-drop is not
independently unit-tested: both handlers only branch on `File.Exists`/`Directory.Exists` for each dropped path and
then call the exact same `MainWindowShell.AddFileSourceAsync`/`AddFolderSourceAsync` methods already covered by
`MainWindowShellTests`, so drag-and-drop is verified by code review rather than a dedicated Avalonia UI-automation
test. The double-click-to-view-source gesture added to `WorkspacePanelToolView`'s code-behind is likewise not
independently unit-tested: it is thin view-layer wiring that resolves the tapped node's `FilePath` and calls the
exact same `MainWindowShell.OpenSourceTextTab` already covered end-to-end by `MainWindowShellTests`, so it too is
verified by code review rather than a dedicated Avalonia UI-automation test - consistent with this codebase's
established convention that no view-layer gesture (drag-and-drop, pointer pan/zoom, and now double-click) has a
headless-Avalonia test in the main test project.

#### Acceptance Criteria

- All implemented tests in
  `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/WorkspacePanelToolViewModelTests.cs` that correspond
  to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/app-shell-subsystem/workspace-panel.yaml` using the real paths and collaborators
  described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**Construction_ZeroSources_ReportsEmptyTree**: Constructing the panel over a shell with zero workspace sources
produces an empty `RootNodes` list and `IsEmpty == true`. Verified by
`WorkspacePanelToolViewModelTests.Construction_ZeroSources_ReportsEmptyTree`.

**RebuildTree_FolderSource_ProducesSourceNodeWithFileChildren**: Adding a folder source and rebuilding produces a
`WorkspaceSourceNode` whose `Children` list contains one `WorkspaceFileNode` per file discovered under that folder.
Verified by `WorkspacePanelToolViewModelTests.RebuildTree_FolderSource_ProducesSourceNodeWithFileChildren`.

**RebuildTree_FolderSourceWithSubfolders_PreservesOnDiskHierarchy**: Adding a folder source whose discovered files
span multiple nested subfolders produces intermediate `WorkspaceFolderNode`s mirroring that on-disk hierarchy
(subfolders before files, each level sorted alphabetically) instead of flattening every file directly under the
source node. Verified by
`WorkspacePanelToolViewModelTests.RebuildTree_FolderSourceWithSubfolders_PreservesOnDiskHierarchy`.

**RebuildTree_FileSource_ProducesLeafSourceNodeWithNoChildren**: Adding a file source and rebuilding produces a
`WorkspaceSourceNode` with an empty `Children` list (a leaf, no expand arrow). Verified by
`WorkspacePanelToolViewModelTests.RebuildTree_FileSource_ProducesLeafSourceNodeWithNoChildren`.

**RebuildTree_OverlappingFileAndFolder_DedupeReflectedInTreeShape**: When a file source overlaps a folder source,
the overlapping file appears exactly once in the tree, as a child of whichever source `WorkspaceSourceSet.Resolve()`
attributed it to, not duplicated under both. Verified by
`WorkspacePanelToolViewModelTests.RebuildTree_OverlappingFileAndFolder_DedupeReflectedInTreeShape`.

**SourcesChanged_TriggersRebuild_AndRemovalRestoresEmptyState**: The panel rebuilds its tree automatically when
`MainWindowShell.SourcesChanged` fires, and removing the last remaining source restores `IsEmpty == true` with an
empty `RootNodes` list. Verified by
`WorkspacePanelToolViewModelTests.SourcesChanged_TriggersRebuild_AndRemovalRestoresEmptyState`.

**AddFileCommand_RaisesRequestAddFile**: Invoking the Add File command raises `RequestAddFile` so the Avalonia-aware
view can fulfill it with a real file picker. Verified by
`WorkspacePanelToolViewModelTests.AddFileCommand_RaisesRequestAddFile`.

**AddFolderCommand_RaisesRequestAddFolder**: Invoking the Add Folder command raises `RequestAddFolder` so the
Avalonia-aware view can fulfill it with a real folder picker. Verified by
`WorkspacePanelToolViewModelTests.AddFolderCommand_RaisesRequestAddFolder`.

**RemoveSelectedCommand_WithSourceNodeSelected_RemovesOwningSource**: Invoking Remove with a `WorkspaceSourceNode`
selected removes that node's source via `MainWindowShell.RemoveSourceAsync`. Verified by
`WorkspacePanelToolViewModelTests.RemoveSelectedCommand_WithSourceNodeSelected_RemovesOwningSource`.

**RemoveSelectedCommand_WithFileNodeSelected_RemovesOwningSource**: Invoking Remove with a `WorkspaceFileNode`
selected resolves and removes that file's owning source via `MainWindowShell.RemoveSourceAsync`. Verified by
`WorkspacePanelToolViewModelTests.RemoveSelectedCommand_WithFileNodeSelected_RemovesOwningSource`.

**RemoveSelectedCommand_NoSelection_IsNoOp**: Invoking Remove with no node selected does not call
`MainWindowShell.RemoveSourceAsync` and leaves the tree unchanged. Verified by
`WorkspacePanelToolViewModelTests.RemoveSelectedCommand_NoSelection_IsNoOp`.
