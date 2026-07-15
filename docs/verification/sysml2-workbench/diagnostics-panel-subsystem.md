## DiagnosticsPanelSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/DiagnosticsPanelSubsystemTests.cs` exercise
DiagnosticsPanelSubsystem's unit (DiagnosticsListView). The suite binds aggregated diagnostics and verifies refresh
behavior through the real list-view unit. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/diagnostics-panel-subsystem.yaml` and describes the implemented tests in present tense.

### Test Environment

Tests run under the standard .NET test runner with in-memory aggregated diagnostics. No UI host, network, or external
services are required.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/DiagnosticsPanelSubsystemTests.cs` that correspond
  to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/diagnostics-panel-subsystem.yaml` using the real paths and collaborators described
  above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

### Test Scenarios

**BindDiagnostics_ShowsDiagnosticDetails**: Binding an aggregated diagnostics snapshot shows each diagnostic's detail.
Verified by `DiagnosticsPanelSubsystemTests.BindDiagnostics_ShowsDiagnosticDetails`.

**DiagnosticsChanged_RefreshesList**: A changed diagnostics aggregate refreshes the visible list. Verified by
`DiagnosticsPanelSubsystemTests.DiagnosticsChanged_RefreshesList`.
