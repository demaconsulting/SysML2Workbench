using DemaConsulting.SysML2Tools.Parser;

namespace DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;

/// <summary>
///     DiagnosticsListView adapts aggregated workspace diagnostics into a selectable, filterable list suitable
///     for the diagnostics panel in the main window.
/// </summary>
public sealed class DiagnosticsListView
{
    /// <summary>
    ///     Full diagnostics snapshot most recently bound, before filters are applied.
    /// </summary>
    private IReadOnlyList<SysmlDiagnostic> _boundDiagnostics = [];

    /// <summary>
    ///     Diagnostics currently shown after filtering.
    /// </summary>
    public IReadOnlyList<SysmlDiagnostic> VisibleDiagnostics { get; private set; } = [];

    /// <summary>
    ///     Currently highlighted diagnostic entry, or <see langword="null" /> when nothing is selected.
    /// </summary>
    public SysmlDiagnostic? SelectedDiagnostic { get; private set; }

    /// <summary>
    ///     Enabled severities used to limit the visible list. Defaults to all severities enabled.
    /// </summary>
    public IReadOnlySet<DiagnosticSeverity> SeverityFilter { get; private set; } =
        new HashSet<DiagnosticSeverity> { DiagnosticSeverity.Error, DiagnosticSeverity.Warning, DiagnosticSeverity.Info };

    /// <summary>
    ///     Optional free-text filter applied to diagnostic messages or file paths.
    /// </summary>
    public string? SearchText { get; private set; }

    /// <summary>
    ///     Replaces the list contents with a new aggregate snapshot.
    /// </summary>
    /// <param name="diagnostics">Latest workspace diagnostics, ordered deterministically by the caller.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diagnostics" /> is null.</exception>
    public void BindDiagnostics(IReadOnlyList<SysmlDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        _boundDiagnostics = diagnostics;
        ApplyFilters(SeverityFilter, SearchText);
    }

    /// <summary>
    ///     Recomputes the visible list from current filter state.
    /// </summary>
    /// <param name="severities">Enabled severities.</param>
    /// <param name="searchText">Optional text filter, matched case-insensitively against message or file path.</param>
    /// <returns>Filtered diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="severities" /> is null.</exception>
    public IReadOnlyList<SysmlDiagnostic> ApplyFilters(IReadOnlySet<DiagnosticSeverity> severities, string? searchText)
    {
        ArgumentNullException.ThrowIfNull(severities);

        SeverityFilter = severities;
        SearchText = searchText;

        var filtered = _boundDiagnostics.Where(d => severities.Contains(d.Severity));
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(d =>
                d.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || d.FilePath.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        VisibleDiagnostics = filtered.ToList();

        // A selection that is no longer visible after filtering is cleared rather than left dangling, matching
        // the documented error handling policy of reducing state locally without throwing
        if (SelectedDiagnostic is { } selected && !VisibleDiagnostics.Contains(selected))
        {
            SelectedDiagnostic = null;
        }

        return VisibleDiagnostics;
    }

    /// <summary>
    ///     Marks one diagnostic as active.
    /// </summary>
    /// <param name="diagnostic">Diagnostic chosen by the user. Must be present in <see cref="VisibleDiagnostics" />.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="diagnostic" /> is not currently visible.</exception>
    public void SelectDiagnostic(SysmlDiagnostic diagnostic)
    {
        if (!VisibleDiagnostics.Contains(diagnostic))
        {
            throw new ArgumentException("The diagnostic must be present in the currently visible list.", nameof(diagnostic));
        }

        SelectedDiagnostic = diagnostic;
    }
}
