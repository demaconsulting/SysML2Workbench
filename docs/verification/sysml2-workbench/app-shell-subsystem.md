## AppShellSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystemTests.cs` exercise AppShellSubsystem's unit
(MainWindowShell) composed with real units from every subsystem it depends on. The suite composes MainWindowShell with
real units from dependent subsystems and drives startup, refresh, and custom-view workflows through temporary
workspaces. The scenario list below follows the authoritative mappings in `docs/reqstream/sysml2-workbench/app-shell-
subsystem.yaml` and describes the implemented tests in present tense.

### Test Environment

Tests run under the standard .NET test runner with temporary workspace and log directories plus real subsystem units
composed through MainWindowShell. No external services are required.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystemTests.cs` that correspond to the
  scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/app-shell-subsystem.yaml` using the real paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

### Test Scenarios

**Startup_ShowsWorkspaceDiagramAndDiagnosticsRegions**: Starting a session with an opened workspace shows the
workspace's views, diagram, and diagnostics regions together. Verified by
`AppShellSubsystemTests.Startup_ShowsWorkspaceDiagramAndDiagnosticsRegions`.

**SessionChanges_SynchronizeShellState**: Session changes (external file edits followed by a refresh) synchronize shell
state across the catalog and diagnostics regions. Verified by
`AppShellSubsystemTests.SessionChanges_SynchronizeShellState`.

**CustomViewWorkflow_PreviewsAndExportsFromShell**: A custom view can be previewed and exported as SysML text from the
shell. Verified by `AppShellSubsystemTests.CustomViewWorkflow_PreviewsAndExportsFromShell`.
