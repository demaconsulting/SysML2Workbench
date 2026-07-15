using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.WorkspaceSubsystem;

/// <summary>
///     Unit tests for <see cref="DiagnosticsAggregator" />.
/// </summary>
public sealed class DiagnosticsAggregatorTests
{
    /// <summary>
    ///     Validates that diagnostics recorded for several different files are combined into one workspace-level
    ///     aggregate.
    /// </summary>
    [Fact]
    public void WorkspaceState_CombinesAllFileDiagnostics()
    {
        // Arrange: diagnostics for two distinct files
        var aggregator = new DiagnosticsAggregator();
        aggregator.ReplaceFileDiagnostics("A.sysml", [new SysmlDiagnostic("A.sysml", 1, 1, DiagnosticSeverity.Warning, "warn-a")]);
        aggregator.ReplaceFileDiagnostics("B.sysml", [new SysmlDiagnostic("B.sysml", 2, 3, DiagnosticSeverity.Error, "error-b")]);

        // Act: rebuild the aggregate
        var combined = aggregator.RebuildAggregate();

        // Assert: both files' diagnostics are present in the combined collection
        Assert.Equal(2, combined.Count);
        Assert.Contains(combined, d => d.FilePath == "A.sysml" && d.Message == "warn-a");
        Assert.Contains(combined, d => d.FilePath == "B.sysml" && d.Message == "error-b");
        Assert.Equal(1, aggregator.SeverityCounts["Warning"]);
        Assert.Equal(1, aggregator.SeverityCounts["Error"]);
    }

    /// <summary>
    ///     Validates that the published diagnostic order is deterministic across repeated aggregation passes,
    ///     ordered by file path, then line, then column.
    /// </summary>
    [Fact]
    public void WorkspaceState_PublishesDeterministicDiagnosticOrder()
    {
        // Arrange: diagnostics deliberately inserted out of order
        var aggregator = new DiagnosticsAggregator();
        aggregator.ReplaceFileDiagnostics("B.sysml", [new SysmlDiagnostic("B.sysml", 5, 1, DiagnosticSeverity.Warning, "b-later")]);
        aggregator.ReplaceFileDiagnostics("A.sysml",
        [
            new SysmlDiagnostic("A.sysml", 10, 1, DiagnosticSeverity.Warning, "a-line10"),
            new SysmlDiagnostic("A.sysml", 2, 5, DiagnosticSeverity.Error, "a-line2-col5"),
            new SysmlDiagnostic("A.sysml", 2, 1, DiagnosticSeverity.Error, "a-line2-col1"),
        ]);

        // Act: rebuild the aggregate twice
        var first = aggregator.RebuildAggregate();
        var second = aggregator.RebuildAggregate();

        // Assert: both passes produce the same, file/line/column-ordered sequence
        var expectedOrder = new[] { "a-line2-col1", "a-line2-col5", "a-line10", "b-later" };
        Assert.Equal(expectedOrder, first.Select(d => d.Message));
        Assert.Equal(expectedOrder, second.Select(d => d.Message));
    }

    /// <summary>
    ///     Validates that consumers retrieving the aggregate before any aggregation pass see an empty, non-null
    ///     collection rather than an exception.
    /// </summary>
    [Fact]
    public void GetVisibleDiagnostics_BeforeRebuild_ReturnsEmptyCollection()
    {
        // Arrange: a freshly constructed aggregator
        var aggregator = new DiagnosticsAggregator();

        // Act: request the visible diagnostics without ever rebuilding
        var visible = aggregator.GetVisibleDiagnostics();

        // Assert: an empty, safe collection is returned
        Assert.Empty(visible);
    }
}
