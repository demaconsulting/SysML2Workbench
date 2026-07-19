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
    ///     Operating-system watchers, one per currently watched source, keyed by <see cref="WorkspaceSource.Id" />.
    /// </summary>
    private readonly Dictionary<string, FileSystemWatcher> _watchersBySourceId = new(StringComparer.Ordinal);

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
    ///     Identifiers of every source currently being monitored for external changes.
    /// </summary>
    public IReadOnlySet<string> WatchedSourceIds => _watchersBySourceId.Keys.ToHashSet(StringComparer.Ordinal);

    /// <summary>
    ///     Minimum delay used to merge rapid change bursts into a single reload batch.
    /// </summary>
    public TimeSpan DebounceWindow { get; }

    /// <summary>
    ///     Normalized paths queued for the next incremental refresh cycle.
    /// </summary>
    public IReadOnlySet<string> PendingChanges => new HashSet<string>(_pending.Keys, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Begins monitoring the given source. Calling this again for a source whose id is already watched
    ///     retargets that one source instead of throwing: its previous <see cref="FileSystemWatcher" /> is
    ///     disposed and replaced, without disturbing any other currently watched source or its pending change
    ///     state. A <see cref="WorkspaceSourceKind.Folder" /> source is watched recursively; a
    ///     <see cref="WorkspaceSourceKind.File" /> source is watched non-recursively on its containing directory,
    ///     filtered to that file's name.
    /// </summary>
    /// <param name="source">Source to observe.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> is null.</exception>
    /// <exception cref="DirectoryNotFoundException">
    ///     Thrown when the source's folder path (or, for a file source, its containing directory) does not exist.
    /// </exception>
    public void WatchSource(WorkspaceSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (_watchersBySourceId.Remove(source.Id, out var previous))
        {
            previous.Dispose();
        }

        var watcher = source.Kind == WorkspaceSourceKind.Folder
            ? CreateFolderWatcher(source.Path)
            : CreateFileWatcher(source.Path);

        watcher.Changed += (_, e) => _dispatcher.Post(() => QueueChange(e.FullPath));
        watcher.Created += (_, e) => _dispatcher.Post(() => QueueChange(e.FullPath));
        watcher.Deleted += (_, e) => _dispatcher.Post(() => QueueChange(e.FullPath));
        watcher.Renamed += (_, e) => _dispatcher.Post(() => QueueChange(e.FullPath));
        watcher.EnableRaisingEvents = true;

        _watchersBySourceId[source.Id] = watcher;
    }

    /// <summary>
    ///     Stops monitoring the given source, if it is currently watched.
    /// </summary>
    /// <remarks>
    ///     Deliberately does not clear <see cref="_pending" />: pending changes contributed by other still-watched
    ///     sources must survive an unrelated source being removed, since each source's watcher is now independent
    ///     rather than one global watcher covering the whole workspace.
    /// </remarks>
    /// <param name="sourceId">Identifier of the source to stop watching.</param>
    /// <returns><see langword="true" /> when a watcher was found and removed; otherwise <see langword="false" />.</returns>
    public bool UnwatchSource(string sourceId)
    {
        if (!_watchersBySourceId.Remove(sourceId, out var watcher))
        {
            return false;
        }

        watcher.Dispose();
        return true;
    }

    /// <summary>
    ///     Creates a recursive watcher covering an entire folder.
    /// </summary>
    /// <param name="folderPath">Folder to observe.</param>
    /// <returns>A configured, not-yet-enabled watcher.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="folderPath" /> does not exist.</exception>
    private static FileSystemWatcher CreateFolderWatcher(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Workspace folder was not found: {folderPath}");
        }

        return new FileSystemWatcher(folderPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
        };
    }

    /// <summary>
    ///     Creates a non-recursive watcher scoped to a single file via its containing directory and a name filter.
    /// </summary>
    /// <param name="filePath">File to observe.</param>
    /// <returns>A configured, not-yet-enabled watcher.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the file's containing directory does not exist.</exception>
    private static FileSystemWatcher CreateFileWatcher(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Containing folder for workspace file was not found: {filePath}");
        }

        return new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = false,
            Filter = Path.GetFileName(filePath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
        };
    }

    /// <summary>
    ///     Records a changed path from an operating-system event.
    /// </summary>
    /// <remarks>
    ///     A source's OS-level watcher callback runs on a background thread and is only marshalled onto
    ///     <see cref="_dispatcher" /> via a posted continuation, so there is always a small window between the
    ///     event firing and this method actually running. If the owning source is unwatched (its
    ///     <see cref="FileSystemWatcher" /> disposed and removed) during that window - most commonly by the user
    ///     removing the last remaining workspace source - the posted callback can run after
    ///     <see cref="_watchersBySourceId" /> has already dropped to zero. Since zero watched sources is now a
    ///     first-class, fully supported state (an empty workspace), this is treated as a harmless, moot
    ///     notification and silently ignored rather than as programmer misuse.
    /// </remarks>
    /// <param name="path">File or folder path reported by the platform.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is null or whitespace.</exception>
    public void QueueChange(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (_watchersBySourceId.Count == 0)
        {
            return;
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
    ///     returned; paths still within their debounce window remain pending for a later flush. Zero watched
    ///     sources (an empty workspace) is a valid, first-class state, so this simply returns an empty list in
    ///     that case rather than throwing.
    /// </remarks>
    /// <returns>Normalized paths requiring reload.</returns>
    public IReadOnlyList<string> FlushPendingChanges()
    {
        if (_watchersBySourceId.Count == 0)
        {
            return [];
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
        foreach (var watcher in _watchersBySourceId.Values)
        {
            watcher.Dispose();
        }

        _watchersBySourceId.Clear();
    }
}
