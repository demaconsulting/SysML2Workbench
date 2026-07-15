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
        watcher.StartWatching(_tempRoot);
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
        watcher.StartWatching(_tempRoot);
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
        watcher.StartWatching(_tempRoot);
        var changedPath = Path.Combine(_tempRoot, "Model.sysml");
        watcher.QueueChange(changedPath);

        // Act: flush immediately, well before the debounce window elapses
        var flushed = watcher.FlushPendingChanges();

        // Assert: nothing is surfaced yet, and the path remains pending
        Assert.Empty(flushed);
        Assert.Contains(changedPath, watcher.PendingChanges);
    }

    /// <summary>
    ///     Validates that queuing a change before monitoring has started is rejected rather than silently
    ///     accepted.
    /// </summary>
    [Fact]
    public void QueueChange_BeforeStartWatching_ThrowsInvalidOperationException()
    {
        // Arrange: a watcher that has never started monitoring
        var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50));

        // Act / Assert: queuing throws instead of silently doing nothing
        Assert.Throws<InvalidOperationException>(() => watcher.QueueChange(Path.Combine(_tempRoot, "Model.sysml")));
    }

    /// <summary>
    ///     Validates that calling <see cref="FileWatcher.StartWatching" /> a second time with a different root
    ///     retargets monitoring instead of throwing, and discards any pending state accumulated against the
    ///     previously watched root so it cannot leak into the newly watched root's pending set.
    /// </summary>
    [Fact]
    public void StartWatching_CalledTwice_RetargetsToNewRootAndDiscardsPendingChanges()
    {
        // Arrange: watch root A and queue a pending change against it
        var rootA = _tempRoot;
        var rootB = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var watcher = new FileWatcher(TimeSpan.FromSeconds(1), () => now);
            watcher.StartWatching(rootA);
            var changedPathUnderA = Path.Combine(rootA, "Model.sysml");
            watcher.QueueChange(changedPathUnderA);
            Assert.Contains(changedPathUnderA, watcher.PendingChanges);

            // Act: retarget to root B - must not throw, despite root A never being explicitly stopped
            var exception = Record.Exception(() => watcher.StartWatching(rootB));

            // Assert: no exception, watched root updated to B, and A's pending change did not leak into B
            Assert.Null(exception);
            Assert.Equal(Path.GetFullPath(rootB), watcher.WatchedRootPath);
            Assert.Empty(watcher.PendingChanges);
            Assert.DoesNotContain(changedPathUnderA, watcher.PendingChanges);
        }
        finally
        {
            Directory.Delete(rootB, recursive: true);
        }
    }

    /// <summary>
    ///     Validates that after retargeting from root A to root B, a real operating-system file-system event
    ///     raised under B is actually detected, proving the underlying <see cref="FileSystemWatcher" /> was truly
    ///     rebuilt against the new root rather than merely having <see cref="FileWatcher.WatchedRootPath" />
    ///     relabeled.
    /// </summary>
    /// <remarks>
    ///     Uses the real system clock and a real <see cref="FileSystemWatcher" /> (no fake clock, unlike this
    ///     file's other tests), and polls with a bounded timeout since OS file-system notifications are
    ///     delivered asynchronously and are not deterministic in timing.
    /// </remarks>
    [Fact]
    public async Task StartWatching_RetargetedRoot_DetectsRealFileChangeUnderNewRoot()
    {
        // Arrange: watch root A, then retarget to root B
        var rootA = _tempRoot;
        var rootB = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;
        try
        {
            var watcher = new FileWatcher(TimeSpan.FromMilliseconds(50));
            watcher.StartWatching(rootA);
            watcher.StartWatching(rootB);

            // Act: write a real file under the newly targeted root B
            var changedPathUnderB = Path.Combine(rootB, "Model.sysml");
            await File.WriteAllTextAsync(changedPathUnderB, "part def Vehicle;", TestContext.Current.CancellationToken);

            // Poll with a bounded timeout: real FileSystemWatcher notifications are asynchronous, and the
            // underlying pending dictionary is not thread-safe against the watcher's own OS-callback thread, so
            // reads are tolerant of transient enumeration exceptions.
            var deadline = DateTime.UtcNow.AddSeconds(5);
            var detected = false;
            while (DateTime.UtcNow < deadline && !detected)
            {
                try
                {
                    detected = watcher.PendingChanges.Any(path =>
                        string.Equals(path, changedPathUnderB, StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    // Transient enumeration failure racing the watcher's own background callback - retry.
                }

                if (!detected)
                {
                    await Task.Delay(50, TestContext.Current.CancellationToken);
                }
            }

            // Assert: the real OS-level change under the new root B was detected
            Assert.True(detected, $"Expected '{changedPathUnderB}' to be reported as a pending change under the retargeted root.");
        }
        finally
        {
            Directory.Delete(rootB, recursive: true);
        }
    }
}
