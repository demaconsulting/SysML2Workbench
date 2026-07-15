using System.Threading;

namespace DemaConsulting.SysML2Workbench.LoggingSubsystem;

/// <summary>
///     Severity of a single logged event.
/// </summary>
public enum LogLevel
{
    /// <summary>Routine operational information.</summary>
    Info,

    /// <summary>An unexpected but recoverable condition.</summary>
    Warning,

    /// <summary>A failure that prevented an operation from completing.</summary>
    Error,
}

/// <summary>
///     RollingFileLogger writes application events and failure details to a bounded set of local files so users
///     can attach recent logs to bug reports without unbounded disk growth. No telemetry or remote transmission
///     is performed - entries are written to local disk only.
/// </summary>
public sealed class RollingFileLogger
{
    /// <summary>
    ///     Guards concurrent writes from multiple threads.
    /// </summary>
    private readonly Lock _syncRoot = new();

    /// <summary>
    ///     Folder containing the active and archived log files.
    /// </summary>
    public string LogDirectory { get; }

    /// <summary>
    ///     Stable prefix used to name each rotated log file.
    /// </summary>
    public string BaseFileName { get; }

    /// <summary>
    ///     Threshold that triggers rotation of the active log.
    /// </summary>
    public long MaximumFileSizeBytes { get; }

    /// <summary>
    ///     Number of historical log files preserved before older files are deleted.
    /// </summary>
    public int RetainedFileCount { get; }

    /// <summary>
    ///     Full path of the currently active (append-target) log file.
    /// </summary>
    public string ActiveFilePath => Path.Combine(LogDirectory, $"{BaseFileName}.log");

    /// <summary>
    ///     Creates a logger that writes into <paramref name="logDirectory" />, creating the directory if needed.
    /// </summary>
    /// <param name="logDirectory">Folder to hold the active and archived log files.</param>
    /// <param name="baseFileName">Stable prefix used to name each rotated log file.</param>
    /// <param name="maximumFileSizeBytes">Threshold, in bytes, that triggers rotation of the active log.</param>
    /// <param name="retainedFileCount">Number of historical log files preserved before older files are deleted.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="logDirectory" /> or <paramref name="baseFileName" /> is null or whitespace,
    ///     or when <paramref name="maximumFileSizeBytes" /> or <paramref name="retainedFileCount" /> is not
    ///     positive.
    /// </exception>
    public RollingFileLogger(
        string logDirectory,
        string baseFileName = "workbench",
        long maximumFileSizeBytes = 1_000_000,
        int retainedFileCount = 5)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseFileName);
        if (maximumFileSizeBytes <= 0)
        {
            throw new ArgumentException("Maximum file size must be positive.", nameof(maximumFileSizeBytes));
        }

        if (retainedFileCount <= 0)
        {
            throw new ArgumentException("Retained file count must be positive.", nameof(retainedFileCount));
        }

        LogDirectory = logDirectory;
        BaseFileName = baseFileName;
        MaximumFileSizeBytes = maximumFileSizeBytes;
        RetainedFileCount = retainedFileCount;

        Directory.CreateDirectory(LogDirectory);
    }

    /// <summary>
    ///     Appends one event to the active log file, rotating first if the active file has grown beyond
    ///     <see cref="MaximumFileSizeBytes" />.
    /// </summary>
    /// <param name="level">Severity of the event.</param>
    /// <param name="message">Event text.</param>
    /// <param name="exception">Optional failure context.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="message" /> is null or whitespace.</exception>
    /// <exception cref="IOException">
    ///     Propagated when the entry cannot be durably written, since the caller may need to notify the user that
    ///     diagnostic evidence could not be captured.
    /// </exception>
    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        lock (_syncRoot)
        {
            RotateIfNeeded();

            var entry = FormatEntry(level, message, exception);
            File.AppendAllText(ActiveFilePath, entry);
        }
    }

    /// <summary>
    ///     Ensures the active file obeys the retention policy: rotating it to a timestamped archive file when it
    ///     exceeds <see cref="MaximumFileSizeBytes" />, and deleting the oldest archives beyond
    ///     <see cref="RetainedFileCount" />.
    /// </summary>
    public void RotateIfNeeded()
    {
        lock (_syncRoot)
        {
            var activeFile = new FileInfo(ActiveFilePath);
            if (!activeFile.Exists || activeFile.Length < MaximumFileSizeBytes)
            {
                return;
            }

            var archiveName = $"{BaseFileName}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.log";
            var archivePath = Path.Combine(LogDirectory, archiveName);
            File.Move(activeFile.FullName, archivePath, overwrite: true);

            PruneOldArchives();
        }
    }

    /// <summary>
    ///     Forces buffered output to disk. Because each <see cref="Log" /> call writes and closes the file
    ///     immediately, no additional buffered state exists to flush; this method exists to satisfy the
    ///     documented unit contract and is safe to call at any time, including before any entry has been written.
    /// </summary>
    public void Flush()
    {
        // No-op by design: File.AppendAllText fully commits and closes the file handle on every call
    }

    /// <summary>
    ///     Deletes the oldest archived log files beyond <see cref="RetainedFileCount" />.
    /// </summary>
    private void PruneOldArchives()
    {
        var archives = Directory
            .EnumerateFiles(LogDirectory, $"{BaseFileName}-*.log")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.Name, StringComparer.Ordinal)
            .ToList();

        foreach (var stale in archives.Skip(RetainedFileCount))
        {
            stale.Delete();
        }
    }

    /// <summary>
    ///     Formats one timestamped log entry line.
    /// </summary>
    /// <param name="level">Severity of the event.</param>
    /// <param name="message">Event text.</param>
    /// <param name="exception">Optional failure context.</param>
    /// <returns>Formatted, newline-terminated entry text.</returns>
    private static string FormatEntry(LogLevel level, string message, Exception? exception)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var line = $"{timestamp} [{level}] {message}";
        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        return line + Environment.NewLine;
    }
}
