### DiagnosticsAggregator

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/DiagnosticsAggregatorTests.cs` exercise
`DiagnosticsAggregator` directly. The suite aggregates in-memory diagnostic snapshots and asserts combined ordering plus
pre-rebuild visibility behavior. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/workspace-subsystem/diagnostics-aggregator.yaml` and describes the implemented tests in
present tense.

#### Test Environment

Tests run under the standard .NET test runner with in-memory diagnostics snapshots only. No file-system, network, or
external services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/DiagnosticsAggregatorTests.cs`
  that correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/workspace-subsystem/diagnostics-aggregator.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**WorkspaceState_CombinesAllFileDiagnostics**: Diagnostics recorded for several different files are combined into one
workspace-level aggregate. Verified by `DiagnosticsAggregatorTests.WorkspaceState_CombinesAllFileDiagnostics`.

**WorkspaceState_PublishesDeterministicDiagnosticOrder**: The published diagnostic order is deterministic across
repeated aggregation passes, ordered by file path, then line, then column. Verified by
`DiagnosticsAggregatorTests.WorkspaceState_PublishesDeterministicDiagnosticOrder`.
