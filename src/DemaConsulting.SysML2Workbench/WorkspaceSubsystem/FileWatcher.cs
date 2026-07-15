namespace DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

/// <summary>
///     Seam for marshaling callbacks onto whatever thread a UI framework requires. Production code posts through
///     an Avalonia dispatcher; tests use <see cref="ImmediateUiDispatcher" /> to run synchronously.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    ///     Runs <paramref name="action" /> on the target thread, or immediately if no marshaling is required.
    /// </summary>
    /// <param name="action">Callback to execute.</param>
    void Post(Action action);
}

/// <summary>
///     An <see cref="IUiDispatcher" /> that runs every action synchronously on the calling thread.
/// </summary>
/// <remarks>
///     Used by default outside of an Avalonia application (for example in unit tests) so that
///     <see cref="FileWatcher" /> does not depend on a live UI dispatcher to be constructed or exercised.
/// </remarks>
public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
    }
}

/// <summary>
///     FileWatcher detects external workspace file-system changes and converts them into bounded incremental
///     reload requests so the workbench stays synchronized with edits made outside the application.
/// </summary>
/// <remarks>
///     Deviation from the original design sketch: instead of relying on a real wall-clock timer to implement the
///     debounce window, this unit accepts an injectable <see cref="Func{TResult}" /> clock. This keeps
///     debounce-coalescing behavior deterministic and fast to test without sleeping real time; production code
///     uses the real UTC clock by default.
/// </remarks>
public sealed class FileWatcher : IDisposable
{
    /// <summary>
    ///     Normalized paths queued for the next incremental refresh cycle, mapped to the timestamp of their most
    ///     recent change notification.
    /// </summary>
    private readonly Dictionary<string, DateTimeOffset> _pending = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Clock used to timestamp and evaluate the debounce window. Injectable for deterministic tests.
    /// </summary>
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>
    ///     Dispatcher used to marshal <see cref="FileSystemWatcher" /> callbacks; production usage marshals onto
    ///     the UI thread.
    /// </summary>
    private readonly IUiDispatcher _dispatcher;

    /// <summary>
    ///     Operating-system watcher configured for recursive observation of the workspace root.
    /// </summary>
    private FileSystemWatcher? _watcher;

    /// <summary>
    ///     Initializes a new <see cref="FileWatcher" />.
    /// </summary>
    /// <param name="debounceWindow">Minimum delay used to merge rapid change bursts into a single reload batch.</param>
    /// <param name="clock">Clock used to timestamp changes. Defaults to the real UTC clock.</param>
    /// <param name="dispatcher">Dispatcher used to marshal watcher callbacks. Defaults to an immediate dispatcher.</param>
    public FileWatcher(TimeSpan debounceWindow, Func<DateTimeOffset>? clock = null, IUiDispatcher? dispatcher = null)
    {
        DebounceWindow = debounceWindow;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _dispatcher = dispatcher ?? new ImmediateUiDispatcher();
    }

    /// <summary>
    ///     Workspace root currently monitored for external changes, or <see langword="null" /> before
    ///     <see cref="StartWatching" /> is called.
    /// </summary>
    public string? WatchedRootPath { get; private set; }

    /// <summary>
    ///     Minimum delay used to merge rapid change bursts into a single reload batch.
    /// </summary>
    public TimeSpan DebounceWindow { get; }

    /// <summary>
    ///     Normalized paths queued for the next incremental refresh cycle.
    /// </summary>
    public IReadOnlySet<string> PendingChanges => new HashSet<string>(_pending.Keys, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Begins monitoring the given workspace root. Calling this while already watching a (possibly
    ///     different) root retargets monitoring to <paramref name="rootPath" /> instead of throwing: the
    ///     previous <see cref="FileSystemWatcher" /> is disposed and any pending change state accumulated
    ///     against the previous root is discarded, so no stale path can leak into the newly watched root's
    ///     pending set.
    /// </summary>
    /// <param name="rootPath">Folder to observe.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rootPath" /> is null or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="rootPath" /> does not exist.</exception>
    public void StartWatching(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Workspace root folder was not found: {rootPath}");
        }

        _watcher?.Dispose();
        _watcher = null;
        _pending.Clear();

        WatchedRootPath = Path.GetFullPath(rootPath);
        _watcher = new FileSystemWatcher(WatchedRootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
        };
        _watcher.Changed += (_, e) => _dispatcher.Post(() => QueueChange(e.FullPath));
        _watcher.Created += (_, e) => _dispatcher.Post(() => QueueChange(e.FullPath));
        _watcher.Deleted += (_, e) => _dispatcher.Post(() => QueueChange(e.FullPath));
        _watcher.Renamed += (_, e) => _dispatcher.Post(() => QueueChange(e.FullPath));
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    ///     Records a changed path from an operating-system event.
    /// </summary>
    /// <param name="path">File or folder path reported by the platform.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when monitoring has not started.</exception>
    public void QueueChange(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (WatchedRootPath is null)
        {
            throw new InvalidOperationException("StartWatching must be called before changes can be queued.");
        }

        // Overwriting the timestamp for an already-pending path is what collapses bursty duplicate
        // notifications into a single pending entry while still resetting its debounce window
        _pending[path] = _clock();
    }

    /// <summary>
    ///     Dispatches the current batch to the workspace reload pipeline.
    /// </summary>
    /// <remarks>
    ///     Only paths whose most recent change notification is at least <see cref="DebounceWindow" /> old are
    ///     returned; paths still within their debounce window remain pending for a later flush.
    /// </remarks>
    /// <returns>Normalized paths requiring reload.</returns>
    /// <exception cref="InvalidOperationException">Thrown when monitoring has not started.</exception>
    public IReadOnlyList<string> FlushPendingChanges()
    {
        if (WatchedRootPath is null)
        {
            throw new InvalidOperationException("StartWatching must be called before changes can be flushed.");
        }

        var now = _clock();
        var ready = _pending
            .Where(kvp => now - kvp.Value >= DebounceWindow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var path in ready)
        {
            _pending.Remove(path);
        }

        return ready;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
