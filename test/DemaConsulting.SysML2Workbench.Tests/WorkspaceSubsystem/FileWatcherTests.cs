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
}
