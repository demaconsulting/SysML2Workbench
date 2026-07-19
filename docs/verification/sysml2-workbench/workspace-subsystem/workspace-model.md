### WorkspaceModel

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/WorkspaceModelTests.cs` exercise `WorkspaceModel`
directly. The suite writes representative SysML files into temporary workspace folders and asserts the resulting
snapshot state and missing-folder failure handling. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/workspace-subsystem/workspace-model.yaml` and describes the implemented tests in
present tense.

#### Test Environment

Tests run under the standard .NET test runner with temporary workspace folders populated by representative SysML files.
No external services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/WorkspaceModelTests.cs` that
  correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/workspace-subsystem/workspace-model.yaml` using the real paths and collaborators
  described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**OpenWorkspace_BuildsTrackedFileTree**: Loading a workspace resolution builds a tracked file tree covering every
file in the resolution. Verified by `WorkspaceModelTests.LoadWorkspaceAsync_BuildsTrackedFileTree`.

**ResolveInputs_FindsDiscoveredAndImportedFiles**: Loading a resolution combines glob-discovered files with SysML
import resolution across those discovered files. Verified by
`WorkspaceModelTests.LoadWorkspaceAsync_FindsDiscoveredAndImportedFiles`.

**ReloadFile_UpdatesOnlyAffectedFileState**: Reloading the workspace only replaces the per-file state of files whose
diagnostics actually changed, leaving unaffected file state instances untouched. Verified by
`WorkspaceModelTests.ReloadFile_UpdatesOnlyAffectedFileState`.

**LoadWorkspaceAsync_EmptyResolution_ProducesValidStdlibOnlySnapshot**: Loading a zero-source, zero-file resolution
produces a valid, non-throwing, stdlib-only snapshot with no diagnostics rather than a special-cased empty
placeholder. Verified by `WorkspaceModelTests.LoadWorkspaceAsync_EmptyResolution_ProducesValidStdlibOnlySnapshot`.

**ReloadFilesAsync_AfterResolutionBecomesEmpty_ProducesValidEmptySnapshot**: Reloading against a resolution that has
since become empty (for example after every source was removed) produces the same valid, non-throwing, stdlib-only
snapshot as loading empty from scratch. Verified by
`WorkspaceModelTests.ReloadFilesAsync_AfterResolutionBecomesEmpty_ProducesValidEmptySnapshot`.
