using DemaConsulting.SysML2Tools.Parser;

namespace DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

/// <summary>
///     DiagnosticsAggregator collects file-level parser and semantic diagnostics from the workspace and
///     publishes a stable workspace-wide view that UI consumers can display without understanding the
///     underlying load pipeline.
/// </summary>
public sealed class DiagnosticsAggregator
{
    /// <summary>
    ///     Grouped diagnostics keyed by normalized file path.
    /// </summary>
    private readonly Dictionary<string, IReadOnlyList<SysmlDiagnostic>> _byFile = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Flattened diagnostic list sorted for deterministic presentation.
    /// </summary>
    public IReadOnlyList<SysmlDiagnostic> OrderedDiagnostics { get; private set; } = [];

    /// <summary>
    ///     Summary counts, keyed by <see cref="DiagnosticSeverity" /> name, used by the UI to show workspace
    ///     health at a glance.
    /// </summary>
    public IReadOnlyDictionary<string, int> SeverityCounts { get; private set; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>
    ///     Timestamp of the last successful aggregation pass, or <see langword="null" /> before the first
    ///     aggregation.
    /// </summary>
    public DateTimeOffset? LastUpdatedUtc { get; private set; }

    /// <summary>
    ///     Grouped diagnostics keyed by normalized file path.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<SysmlDiagnostic>> DiagnosticsByFile => _byFile;

    /// <summary>
    ///     Updates the diagnostics for one file.
    /// </summary>
    /// <param name="path">Normalized file path.</param>
    /// <param name="diagnostics">Latest diagnostics for that file.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path" /> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diagnostics" /> is null.</exception>
    public void ReplaceFileDiagnostics(string path, IReadOnlyList<SysmlDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(diagnostics);

        _byFile[path] = diagnostics;
    }

    /// <summary>
    ///     Replaces the entire grouped diagnostic state from a workspace-wide diagnostic list, grouping entries
    ///     by their <see cref="SysmlDiagnostic.FilePath" />.
    /// </summary>
    /// <param name="diagnostics">Flat, workspace-wide diagnostics to group and store.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diagnostics" /> is null.</exception>
    public void ReplaceWorkspaceDiagnostics(IReadOnlyList<SysmlDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        _byFile.Clear();
        foreach (var group in diagnostics.GroupBy(d => d.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            _byFile[group.Key] = group.ToList();
        }
    }

    /// <summary>
    ///     Recomputes the flattened workspace diagnostic view.
    /// </summary>
    /// <remarks>
    ///     Diagnostics are ordered deterministically by file path, then line, then column, so repeated
    ///     aggregation passes over unchanged input always produce the same presentation order.
    /// </remarks>
    /// <returns>Ordered diagnostics ready for presentation.</returns>
    public IReadOnlyList<SysmlDiagnostic> RebuildAggregate()
    {
        var ordered = _byFile.Values
            .SelectMany(list => list)
            .OrderBy(d => d.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(d => d.Line)
            .ThenBy(d => d.Column)
            .ToList();

        OrderedDiagnostics = ordered;
        SeverityCounts = ordered
            .GroupBy(d => d.Severity.ToString(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        LastUpdatedUtc = DateTimeOffset.UtcNow;

        return OrderedDiagnostics;
    }

    /// <summary>
    ///     Returns the current aggregate for consumers.
    /// </summary>
    /// <returns>Read-only diagnostics snapshot that will not mutate during iteration.</returns>
    public IReadOnlyList<SysmlDiagnostic> GetVisibleDiagnostics()
    {
        return OrderedDiagnostics;
    }
}
