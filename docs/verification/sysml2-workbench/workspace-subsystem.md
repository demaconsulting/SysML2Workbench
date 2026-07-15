## WorkspaceSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystemTests.cs` exercise WorkspaceSubsystem's units
(WorkspaceModel, FileWatcher, DiagnosticsAggregator) together. The suite uses real workspace files and queued watcher
changes to drive load, refresh, and diagnostic aggregation behavior across the subsystem boundary. The scenario list
below follows the authoritative mappings in `docs/reqstream/sysml2-workbench/workspace-subsystem.yaml` and describes the
implemented tests in present tense.

### Test Environment

Tests run under the standard .NET test runner with temporary workspace directories and direct file writes that drive
real load and refresh behavior. No UI host, network, or external services are required.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystemTests.cs` that correspond to the
  scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/workspace-subsystem.yaml` using the real paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

### Test Scenarios

**OpenWorkspace_BuildsWorkspaceState**: Opening a workspace builds a complete, discoverable workspace state: the file
tree, semantic workspace, and diagnostics are all populated together. Verified by
`WorkspaceSubsystemTests.OpenWorkspace_BuildsWorkspaceState`.

**ExternalChange_RefreshesWorkspaceState**: An external file change detected by FileWatcher results in WorkspaceModel
refreshing its state when the reload pipeline is driven from the watcher's flushed change set. Verified by
`WorkspaceSubsystemTests.ExternalChange_RefreshesWorkspaceState`.

**OpenWorkspace_ProducesUnifiedDiagnostics**: Opening a workspace produces one unified, deterministically ordered
diagnostic view spanning every file, via DiagnosticsAggregator. Verified by
`WorkspaceSubsystemTests.OpenWorkspace_ProducesUnifiedDiagnostics`.
