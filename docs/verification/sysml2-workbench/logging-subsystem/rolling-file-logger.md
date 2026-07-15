### RollingFileLogger

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/LoggingSubsystem/RollingFileLoggerTests.cs` exercise
`RollingFileLogger` directly. The suite writes and rotates real log files in temporary folders, then asserts
timestamp formatting, exception-detail capture, flush safety, and constructor guards. The scenario list below follows the
authoritative mappings in `docs/reqstream/sysml2-workbench/logging-subsystem/rolling-file-logger.yaml` and describes the
implemented tests in present tense.

#### Test Environment

Tests run under the standard .NET test runner with temporary local log directories and real file I/O. No network or
external services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/LoggingSubsystem/RollingFileLoggerTests.cs` that
  correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/logging-subsystem/rolling-file-logger.yaml` using the real paths and collaborators
  described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**LogEvent_WritesTimestampedEntry**: Logging an event writes a timestamped entry to the active log file. Verified by
`RollingFileLoggerTests.LogEvent_WritesTimestampedEntry`.

**RetentionLimit_RotatesLogFiles**: Once the active file exceeds the configured size threshold, further logging rotates
it into an archive file and prunes archives beyond the retained count. Verified by
`RollingFileLoggerTests.RetentionLimit_RotatesLogFiles`.
