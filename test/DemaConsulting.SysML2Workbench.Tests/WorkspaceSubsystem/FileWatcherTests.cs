using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.WorkspaceSubsystem;

/// <summary>
///     Unit tests for <see cref="FileWatcher" />.
/// </summary>
public sealed class FileWatcherTests : IDisposable
{
    /// <summary>
    ///     Temporary workspace root folder created fresh for each test and removed on disposal.
    /// </summary>
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static WorkspaceSource FolderSource(string path)
    {
        return new WorkspaceSource(Guid.NewGuid().ToString("N"), WorkspaceSourceKind.Folder, Path.GetFullPath(path));
    }

    private static WorkspaceSource FileSource(string path)
    {
        return new WorkspaceSource(Guid.NewGuid().ToString("N"), WorkspaceSourceKind.File, Path.GetFullPath(path));
    }

    /// <summary>
    ///     Validates that an externally reported file change is recorded as a pending path and is returned by a
    ///     flush once its debounce window has elapsed.
    /// </summary>
    [Fact]
    public void ExternalFileChange_RaisesAffectedPathEvent()
    {
        // Arrange: a watcher with a fake clock so the debounce window can be advanced deterministically
        var now = DateTimeOffset.UtcNow;
        var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50), () => now);
        watcher.WatchSource(FolderSource(_tempRoot));
        var changedPath = Path.Combine(_tempRoot, "Model.sysml");

        // Act: simulate an external change notification, then advance the clock past the debounce window
        watcher.QueueChange(changedPath);
        Assert.Contains(changedPath, watcher.PendingChanges);
        now = now.AddMilliseconds(100);
        var flushed = watcher.FlushPendingChanges();

        // Assert: the changed path is surfaced by the flush and removed from the pending set
        Assert.Contains(changedPath, flushed);
        Assert.DoesNotContain(changedPath, watcher.PendingChanges);
    }

    /// <summary>
    ///     Validates that repeated notifications for the same path within the debounce window collapse into a
    ///     single reload trigger instead of one trigger per notification.
    /// </summary>
    [Fact]
    public void NotificationBurst_CoalescesIntoSingleReloadTrigger()
    {
        // Arrange: a watcher with a fake clock
        var now = DateTimeOffset.UtcNow;
        var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50), () => now);
        watcher.WatchSource(FolderSource(_tempRoot));
        var changedPath = Path.Combine(_tempRoot, "Model.sysml");

        // Act: raise a burst of notifications for the same path in quick succession
        watcher.QueueChange(changedPath);
        now = now.AddMilliseconds(10);
        watcher.QueueChange(changedPath);
        now = now.AddMilliseconds(10);
        watcher.QueueChange(changedPath);

        // Advance past the debounce window measured from the *last* notification and flush
        now = now.AddMilliseconds(60);
        var flushed = watcher.FlushPendingChanges();

        // Assert: the burst collapsed into exactly one reload-trigger entry for the path
        Assert.Single(flushed);
        Assert.Equal(changedPath, flushed[0]);
    }

    /// <summary>
    ///     Validates that a path whose most recent notification is still within the debounce window is retained
    ///     for a later flush instead of being surfaced prematurely.
    /// </summary>
    [Fact]
    public void FlushPendingChanges_WithinDebounceWindow_RetainsPathForLaterFlush()
    {
        // Arrange: a watcher with a fake clock and a change just registered
        var now = DateTimeOffset.UtcNow;
        var watcher = new FileWatcher(TimeSpan.FromSeconds(1), () => now);
        watcher.WatchSource(FolderSource(_tempRoot));
        var changedPath = Path.Combine(_tempRoot, "Model.sysml");
        watcher.QueueChange(changedPath);

        // Act: flush immediately, well before the debounce window elapses
        var flushed = watcher.FlushPendingChanges();

        // Assert: nothing is surfaced yet, and the path remains pending
        Assert.Empty(flushed);
        Assert.Contains(changedPath, watcher.PendingChanges);
    }

    /// <summary>
    ///     Validates that queuing a change before any source is watched (or after the last source has been
    ///     unwatched) is silently ignored rather than throwing, since zero watched sources is now a first-class
    ///     valid state (an empty workspace) and a change notification can legitimately race an unwatch.
    /// </summary>
    [Fact]
    public void QueueChange_WithNoWatchedSources_IsIgnored()
    {
        // Arrange: a watcher that has never started monitoring any source
        var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50));

        // Act: queuing does not throw
        watcher.QueueChange(Path.Combine(_tempRoot, "Model.sysml"));

        // Assert: nothing was recorded, and flushing is likewise a harmless no-op
        Assert.Empty(watcher.PendingChanges);
        Assert.Empty(watcher.FlushPendingChanges());
    }

    /// <summary>
    ///     Regression test: reproduces a change notification for a source's watcher arriving after that source has
    ///     already been unwatched (e.g. the user removed the last remaining workspace source between the OS event
    ///     firing and the dispatcher-marshalled callback actually running). Previously this threw an unhandled
    ///     <see cref="InvalidOperationException" /> on the dispatcher; it must now be a harmless no-op.
    /// </summary>
    [Fact]
    public void QueueChange_AfterSourceUnwatchedToZero_DoesNotThrow()
    {
        // Arrange: watch a single folder source, then unwatch it, simulating the watcher count dropping to zero
        // while a change notification for that same source is still in flight
        var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50));
        var source = FolderSource(_tempRoot);
        watcher.WatchSource(source);
        watcher.UnwatchSource(source.Id);

        // Act: a stale change notification for the now-unwatched source arrives
        var exception = Record.Exception(() => watcher.QueueChange(Path.Combine(_tempRoot, "Model.sysml")));

        // Assert: no exception, and the stale notification was not recorded
        Assert.Null(exception);
        Assert.Empty(watcher.PendingChanges);
        Assert.Empty(watcher.FlushPendingChanges());
    }

    /// <summary>
    ///     Validates that <see cref="FileWatcher.WatchSource" /> tracks each distinct source id independently, so
    ///     watching two different sources results in both ids being reported as watched.
    /// </summary>
    [Fact]
    public void WatchSource_TwoDistinctSources_BothTrackedIndependently()
    {
        // Arrange
        var folderA = FolderSource(_tempRoot);
        var otherRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
        try
        {
            var folderB = FolderSource(otherRoot);
            var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50));

            // Act
            watcher.WatchSource(folderA);
            watcher.WatchSource(folderB);

            // Assert
            Assert.Equal(2, watcher.WatchedSourceIds.Count);
            Assert.Contains(folderA.Id, watcher.WatchedSourceIds);
            Assert.Contains(folderB.Id, watcher.WatchedSourceIds);
        }
        finally
        {
            Directory.Delete(otherRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Validates that <see cref="FileWatcher.UnwatchSource" /> stops monitoring only the given source,
    ///     leaving other currently watched sources - and their pending changes - untouched: a change queued
    ///     under folder A before A is unwatched must not be discarded just because A stopped being watched, and
    ///     folder B's own pending state must be unaffected by the operation.
    /// </summary>
    [Fact]
    public void UnwatchSource_RemovesOnlyThatWatcher_OthersContinueReporting()
    {
        // Arrange: two watched folder sources, each with a pending change queued against it
        var folderA = FolderSource(_tempRoot);
        var otherRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
        try
        {
            var folderB = FolderSource(otherRoot);
            var now = DateTimeOffset.UtcNow;
            var watcher = new FileWatcher(TimeSpan.FromSeconds(1), () => now);
            watcher.WatchSource(folderA);
            watcher.WatchSource(folderB);

            var pathUnderA = Path.Combine(folderA.Path, "A.sysml");
            var pathUnderB = Path.Combine(folderB.Path, "B.sysml");
            watcher.QueueChange(pathUnderA);
            watcher.QueueChange(pathUnderB);

            // Act: unwatch only source A
            var removed = watcher.UnwatchSource(folderA.Id);

            // Assert: A is no longer watched, B still is, and neither pending change was discarded
            Assert.True(removed);
            Assert.DoesNotContain(folderA.Id, watcher.WatchedSourceIds);
            Assert.Contains(folderB.Id, watcher.WatchedSourceIds);
            Assert.Contains(pathUnderA, watcher.PendingChanges);
            Assert.Contains(pathUnderB, watcher.PendingChanges);
        }
        finally
        {
            Directory.Delete(otherRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Validates that <see cref="FileWatcher.UnwatchSource" /> returns <see langword="false" /> for a source
    ///     id that is not currently watched, rather than throwing.
    /// </summary>
    [Fact]
    public void UnwatchSource_UnknownSourceId_ReturnsFalse()
    {
        // Arrange
        var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50));

        // Act
        var removed = watcher.UnwatchSource("unknown-source-id");

        // Assert
        Assert.False(removed);
    }

    /// <summary>
    ///     Validates that calling <see cref="FileWatcher.WatchSource" /> a second time for the same source id
    ///     retargets that one source instead of throwing, and does not disturb any other watched source.
    /// </summary>
    [Fact]
    public void WatchSource_CalledTwiceForSameSourceId_RetargetsWithoutThrowing()
    {
        // Arrange: watch a folder source, then build a second WorkspaceSource with the same id but a different
        // path, simulating a source whose path was retargeted while its id stayed stable.
        var rootA = _tempRoot;
        var rootB = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
        try
        {
            var sourceId = Guid.NewGuid().ToString("N");
            var sourceA = new WorkspaceSource(sourceId, WorkspaceSourceKind.Folder, Path.GetFullPath(rootA));
            var sourceB = new WorkspaceSource(sourceId, WorkspaceSourceKind.Folder, Path.GetFullPath(rootB));
            var watcher = new FileWatcher(TimeSpan.FromSeconds(1));
            watcher.WatchSource(sourceA);

            // Act: retarget - must not throw, despite the source never being explicitly unwatched
            var exception = Record.Exception(() => watcher.WatchSource(sourceB));

            // Assert: no exception, and the source id is still reported as watched exactly once
            Assert.Null(exception);
            Assert.Single(watcher.WatchedSourceIds);
            Assert.Contains(sourceId, watcher.WatchedSourceIds);
        }
        finally
        {
            Directory.Delete(rootB, recursive: true);
        }
    }

    /// <summary>
    ///     Validates that a real operating-system file-system event raised under one watched folder source is
    ///     detected as a pending change, while a change under a second, independently watched folder source does
    ///     not get attributed to the first - proving each source genuinely has its own isolated watch scope.
    /// </summary>
    /// <remarks>
    ///     Uses the real system clock and real <see cref="System.IO.FileSystemWatcher" /> instances (no fake
    ///     clock, unlike this file's other tests), and polls with a bounded timeout since OS file-system
    ///     notifications are delivered asynchronously and are not deterministic in timing.
    /// </remarks>
    [Fact]
    public async Task WatchSource_TwoFolders_ChangeUnderOneIsNotAttributedToTheOther()
    {
        // Arrange: watch two independent folder sources
        var rootA = _tempRoot;
        var rootB = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
        try
        {
            var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50));
            watcher.WatchSource(FolderSource(rootA));
            watcher.WatchSource(FolderSource(rootB));

            // Act: write a real file under root B only
            var changedPathUnderB = Path.Combine(rootB, "Model.sysml");
            await File.WriteAllTextAsync(changedPathUnderB, "part def Vehicle;", TestContext.Current.CancellationToken);

            // Poll with a bounded timeout: real FileSystemWatcher notifications are delivered on an OS callback
            // thread, asynchronously and non-deterministically with respect to this polling loop.
            var deadline = DateTime.UtcNow.AddSeconds(5);
            var detected = false;
            while (DateTime.UtcNow < deadline && !detected)
            {
                detected = watcher.PendingChanges.Any(path =>
                    string.Equals(path, changedPathUnderB, StringComparison.OrdinalIgnoreCase));

                if (!detected)
                {
                    await Task.Delay(50, TestContext.Current.CancellationToken);
                }
            }

            // Assert: the real OS-level change under B was detected, and nothing under root A was ever written,
            // so no path under A can appear as a false-positive pending change.
            Assert.True(detected, $"Expected '{changedPathUnderB}' to be reported as a pending change under root B.");
            Assert.DoesNotContain(watcher.PendingChanges, path => path.StartsWith(rootA, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(rootB, recursive: true);
        }
    }

    /// <summary>
    ///     Regression test: reproduces many real, concurrently-arriving <see cref="System.IO.FileSystemWatcher" />
    ///     callbacks (via <see cref="ImmediateUiDispatcher" />, which runs them synchronously on their own OS
    ///     callback thread rather than marshaling onto a UI thread) racing repeated reads via
    ///     <see cref="FileWatcher.PendingChanges" /> and <see cref="FileWatcher.FlushPendingChanges" /> from the
    ///     test thread. Previously, concurrent unsynchronized access to the internal pending-changes dictionary
    ///     could corrupt its state and crash the whole process rather than merely throwing a recoverable
    ///     exception; this must now complete cleanly under sustained concurrent load.
    /// </summary>
    [Fact]
    public async Task ConcurrentRealFileSystemChangesAndReads_DoesNotCorruptState()
    {
        // Arrange: a real, immediately-dispatched watcher so OS callbacks run on background threads
        var watcher = new FileWatcher(TimeSpan.FromMilliseconds(10));
        watcher.WatchSource(FolderSource(_tempRoot));

        // Act: concurrently generate a burst of real file-system events while continuously reading and
        // flushing from the test thread, for a short but sustained window.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var writerTask = Task.Run(async () =>
        {
            var counter = 0;
            while (!cts.IsCancellationRequested)
            {
                var path = Path.Combine(_tempRoot, $"File{counter++ % 20}.sysml");
                try
                {
                    await File.WriteAllTextAsync(path, "part def Widget;", CancellationToken.None);
                }
                catch (IOException)
                {
                    // Transient sharing violation writing the same rotating file names under load - benign.
                }
            }
        }, CancellationToken.None);

        var exception = await Record.ExceptionAsync(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                _ = watcher.PendingChanges.Count;
                _ = watcher.FlushPendingChanges();
                await Task.Delay(5, CancellationToken.None);
            }
        });

        await writerTask;

        // Assert: no exception surfaced from either the reader or writer side under concurrent load
        Assert.Null(exception);
    }
}
