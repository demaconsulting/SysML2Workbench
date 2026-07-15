### DiagnosticsListView

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/DiagnosticsPanelSubsystem/DiagnosticsListViewTests.cs` exercise
`DiagnosticsListView` directly. The suite applies filters and selection changes to in-memory diagnostics so visibility,
search, and guard paths are all exercised. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/diagnostics-panel-subsystem/diagnostics-list-view.yaml` and describes the implemented
tests in present tense.

#### Test Environment

Tests run under the standard .NET test runner with in-memory diagnostics snapshots, filters, and selection state only.
No external services are required.

#### Acceptance Criteria

- All implemented tests in
  `test/DemaConsulting.SysML2Workbench.Tests/DiagnosticsPanelSubsystem/DiagnosticsListViewTests.cs` that correspond to
  the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/diagnostics-panel-subsystem/diagnostics-list-view.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**BindDiagnostics_ShowsSeverityMessageAndLocation**: Binding a diagnostics snapshot shows each entry's severity,
message, and location. Verified by `DiagnosticsListViewTests.BindDiagnostics_ShowsSeverityMessageAndLocation`.

**DiagnosticsChanged_RefreshesVisibleList**: Changing the aggregated diagnostics refreshes the visible list. Verified by
`DiagnosticsListViewTests.DiagnosticsChanged_RefreshesVisibleList`.
