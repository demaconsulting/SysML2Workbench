### FileWatcher

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/FileWatcherTests.cs` exercise `FileWatcher`
directly. The suite drives the unit with deterministic timestamps and queued change notifications so debounce and guard-
path behavior remain repeatable. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/workspace-subsystem/file-watcher.yaml` and describes the implemented tests in present
tense.

#### Test Environment

Tests run under the standard .NET test runner. Most scenarios use deterministic timestamps and manually queued
file-change notifications and require no real OS watcher. The per-source isolation scenario
(`WatchSource_TwoFolders_ChangeUnderOneIsNotAttributedToTheOther`) uses real `FileSystemWatcher` instances over two
temporary folders to prove isolation end-to-end. No network or other external services are required.

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

**FlushPendingChanges_WithinDebounceWindow_RetainsPathForLaterFlush**: A pending change flushed before its debounce
window has elapsed is retained rather than discarded, and is returned by a later flush once the window elapses.
Verified by `FileWatcherTests.FlushPendingChanges_WithinDebounceWindow_RetainsPathForLaterFlush`.

**WatchSource_TwoDistinctSources_BothTrackedIndependently**: Watching two distinct sources tracks both independently
in `WatchedSourceIds`, and each source's watcher operates without disturbing the other. Verified by
`FileWatcherTests.WatchSource_TwoDistinctSources_BothTrackedIndependently`.

**WatchSource_TwoFolders_ChangeUnderOneIsNotAttributedToTheOther**: With two independently watched folder sources, a
real file-system change under one folder is reported as pending, while the other folder's watcher contributes
nothing for that change - direct proof that per-source watch scope is isolated. Verified by
`FileWatcherTests.WatchSource_TwoFolders_ChangeUnderOneIsNotAttributedToTheOther`.

**WatchSource_CalledTwiceForSameSourceId_RetargetsWithoutThrowing**: Calling `WatchSource` twice for the same source
id disposes and replaces that source's watcher without throwing and without disturbing any other still-watched
source. Verified by `FileWatcherTests.WatchSource_CalledTwiceForSameSourceId_RetargetsWithoutThrowing`.

**UnwatchSource_RemovesOnlyThatWatcher_OthersContinueReporting**: Unwatching one source disposes only that source's
watcher and removes it from `WatchedSourceIds`, while other still-watched sources continue reporting changes and
their previously queued pending state survives. Verified by
`FileWatcherTests.UnwatchSource_RemovesOnlyThatWatcher_OthersContinueReporting`.

**UnwatchSource_UnknownSourceId_ReturnsFalse**: Unwatching an id that is not currently watched returns `false`
without throwing. Verified by `FileWatcherTests.UnwatchSource_UnknownSourceId_ReturnsFalse`.

**QueueChange_BeforeWatchSource_ThrowsInvalidOperationException**: Queuing a change before any source has been
watched throws `InvalidOperationException` rather than silently accumulating unattributable pending state. Verified
by `FileWatcherTests.QueueChange_BeforeWatchSource_ThrowsInvalidOperationException`.
