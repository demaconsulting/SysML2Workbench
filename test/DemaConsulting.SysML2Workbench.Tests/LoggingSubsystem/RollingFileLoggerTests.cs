using DemaConsulting.SysML2Workbench.LoggingSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.LoggingSubsystem;

/// <summary>
///     Unit tests for <see cref="RollingFileLogger" />.
/// </summary>
public sealed class RollingFileLoggerTests : IDisposable
{
    /// <summary>
    ///     Temporary log directory created fresh for each test and removed on disposal.
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
    ///     Validates that logging an event writes a timestamped entry to the active log file.
    /// </summary>
    [Fact]
    public void LogEvent_WritesTimestampedEntry()
    {
        // Arrange
        var logger = new RollingFileLogger(_tempRoot);

        // Act
        logger.Log(LogLevel.Info, "Workspace opened");

        // Assert: the active file exists and contains a timestamped, leveled entry
        Assert.True(File.Exists(logger.ActiveFilePath));
        var content = File.ReadAllText(logger.ActiveFilePath);
        Assert.Contains("[Info]", content);
        Assert.Contains("Workspace opened", content);

        // Assert: the entry begins with a parseable ISO-8601 timestamp
        var firstToken = content.Split(' ')[0];
        Assert.True(DateTimeOffset.TryParse(firstToken, out _));
    }

    /// <summary>
    ///     Validates that logging captures optional exception details alongside the message.
    /// </summary>
    [Fact]
    public void LogEvent_WithException_IncludesExceptionDetails()
    {
        // Arrange
        var logger = new RollingFileLogger(_tempRoot);
        var exception = new InvalidOperationException("boom");

        // Act
        logger.Log(LogLevel.Error, "Reload failed", exception);

        // Assert
        var content = File.ReadAllText(logger.ActiveFilePath);
        Assert.Contains("Reload failed", content);
        Assert.Contains("boom", content);
    }

    /// <summary>
    ///     Validates that once the active file exceeds the configured size threshold, further logging rotates it
    ///     into an archive file and prunes archives beyond the retained count.
    /// </summary>
    [Fact]
    public void RetentionLimit_RotatesLogFiles()
    {
        // Arrange: a tiny size threshold so a single entry immediately triggers rotation, and a retention
        // policy of only two archived files
        var logger = new RollingFileLogger(_tempRoot, maximumFileSizeBytes: 10, retainedFileCount: 2);

        // Act: write enough entries to force several rotations
        for (var i = 0; i < 5; i++)
        {
            logger.Log(LogLevel.Info, $"Event {i}");
        }

        // Assert: no more than the retained archive count remains, plus the still-active file
        var archives = Directory.GetFiles(_tempRoot, "workbench-*.log");
        Assert.True(archives.Length <= 2, $"Expected at most 2 archived files, found {archives.Length}.");
    }

    /// <summary>
    ///     Validates that flushing does not throw even when no entry has yet been written.
    /// </summary>
    [Fact]
    public void Flush_BeforeAnyLogEntry_DoesNotThrow()
    {
        // Arrange
        var logger = new RollingFileLogger(_tempRoot);

        // Act / Assert
        var exception = Record.Exception(logger.Flush);
        Assert.Null(exception);
    }

    /// <summary>
    ///     Validates that construction rejects a non-positive maximum file size.
    /// </summary>
    [Fact]
    public void Constructor_NonPositiveMaximumFileSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RollingFileLogger(_tempRoot, maximumFileSizeBytes: 0));
    }
}
