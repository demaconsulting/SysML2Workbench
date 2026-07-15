## LoggingSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/LoggingSubsystemTests.cs` exercise LoggingSubsystem's unit
(RollingFileLogger). The suite writes real log files into temporary folders and verifies local persistence plus
retention-based rotation behavior. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/logging-subsystem.yaml` and describes the implemented tests in present tense.

### Test Environment

Tests run under the standard .NET test runner with temporary local log directories. No network or external services are
required.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/LoggingSubsystemTests.cs` that correspond to the
  scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/logging-subsystem.yaml` using the real paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

### Test Scenarios

**LogEvent_WritesLocalEntry**: Logging an event writes a local entry to disk that is available for bug-report attachment
(no telemetry, purely local). Verified by `LoggingSubsystemTests.LogEvent_WritesLocalEntry`.

**LogGrowth_RotatesRetainedFiles**: Unbounded log growth is prevented: once the active file exceeds its size threshold,
older files beyond the retention limit are pruned. Verified by `LoggingSubsystemTests.LogGrowth_RotatesRetainedFiles`.
