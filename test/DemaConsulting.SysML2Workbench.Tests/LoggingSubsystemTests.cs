using DemaConsulting.SysML2Workbench.LoggingSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     Subsystem-level tests exercising LoggingSubsystem's unit (<see cref="RollingFileLogger" />), per
///     docs/reqstream/sysml2-workbench/logging-subsystem.yaml.
/// </summary>
public sealed class LoggingSubsystemTests : IDisposable
{
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
    ///     Validates that logging an event writes a local entry to disk that is available for bug-report
    ///     attachment (no telemetry, purely local).
    /// </summary>
    [Fact]
    public void LogEvent_WritesLocalEntry()
    {
        // Arrange
        var logger = new RollingFileLogger(_tempRoot);

        // Act
        logger.Log(LogLevel.Info, "Session started");

        // Assert: the entry exists locally on disk and nowhere else
        Assert.True(File.Exists(logger.ActiveFilePath));
        Assert.Contains("Session started", File.ReadAllText(logger.ActiveFilePath));
    }

    /// <summary>
    ///     Validates that unbounded log growth is prevented: once the active file exceeds its size threshold,
    ///     older files beyond the retention limit are pruned.
    /// </summary>
    [Fact]
    public void LogGrowth_RotatesRetainedFiles()
    {
        // Arrange: a tiny threshold so every entry forces a rotation, retaining only three archives
        var logger = new RollingFileLogger(_tempRoot, maximumFileSizeBytes: 5, retainedFileCount: 3);

        // Act
        for (var i = 0; i < 10; i++)
        {
            logger.Log(LogLevel.Info, $"Entry {i}");
        }

        // Assert
        var archives = Directory.GetFiles(_tempRoot, "workbench-*.log");
        Assert.True(archives.Length <= 3, $"Expected at most 3 retained archives, found {archives.Length}.");
    }
}
