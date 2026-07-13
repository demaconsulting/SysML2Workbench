### MainWindowShell

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/MainWindowShellTests.cs` exercise
`MainWindowShell` directly. The suite drives the shell with temporary workspaces, real collaborator units, and local log
files so region synchronization, tab management, and failure guards are verified. The scenario list below follows the
authoritative mappings in `docs/reqstream/sysml2-workbench/app-shell-subsystem/main-window-shell.yaml` and describes the
implemented tests in present tense.

#### Test Environment

Tests run under the standard .NET test runner with temporary workspace and log directories plus real collaborator units.
No external services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/MainWindowShellTests.cs` that
  correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/app-shell-subsystem/main-window-shell.yaml` using the real paths and collaborators
  described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**Startup_ArrangesPrimaryWorkspaceAndDiagramRegions**: Opening a workspace arranges the primary workspace, diagram, and
diagnostics regions: the catalog, diagnostics list, and canvas host all reflect the freshly loaded workspace. Verified
by `MainWindowShellTests.Startup_ArrangesPrimaryWorkspaceAndDiagramRegions`.

**OpenViews_ManagesTabbedPresentation**: Opening tabs manages tabbed presentation: selecting a predefined view and then
previewing a custom view each open a distinct tab, without duplicating an already-open tab. Verified by
`MainWindowShellTests.OpenViews_ManagesTabbedPresentation`.

**SessionStateChanges_SynchronizeVisibleRegions**: Reloading the workspace after an external change resynchronizes
visible shell regions (diagnostics and catalog) and resets stale active-view state. Verified by
`MainWindowShellTests.SessionStateChanges_SynchronizeVisibleRegions`.
