### FileWatcher

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/FileWatcherTests.cs` exercise `FileWatcher`
directly. The suite drives the unit with deterministic timestamps and queued change notifications so debounce and guard-
path behavior remain repeatable. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/workspace-subsystem/file-watcher.yaml` and describes the implemented tests in present
tense.

#### Test Environment

Tests run under the standard .NET test runner with deterministic timestamps and manually queued file-change
notifications. No real OS watcher, network, or external services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/FileWatcherTests.cs` that
  correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/workspace-subsystem/file-watcher.yaml` using the real paths and collaborators
  described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**ExternalFileChange_RaisesAffectedPathEvent**: An externally reported file change is recorded as a pending path and is
returned by a flush once its debounce window has elapsed. Verified by
`FileWatcherTests.ExternalFileChange_RaisesAffectedPathEvent`.

**NotificationBurst_CoalescesIntoSingleReloadTrigger**: Repeated notifications for the same path within the debounce
window collapse into a single reload trigger instead of one trigger per notification. Verified by
`FileWatcherTests.NotificationBurst_CoalescesIntoSingleReloadTrigger`.
