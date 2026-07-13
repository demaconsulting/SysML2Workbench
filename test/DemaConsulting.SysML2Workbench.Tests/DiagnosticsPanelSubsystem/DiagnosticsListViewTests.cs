using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.DiagnosticsPanelSubsystem;

/// <summary>
///     Unit tests for <see cref="DiagnosticsListView" />.
/// </summary>
public sealed class DiagnosticsListViewTests
{
    private static readonly SysmlDiagnostic ErrorDiagnostic = new("a.sysml", 1, 1, DiagnosticSeverity.Error, "Missing semicolon");
    private static readonly SysmlDiagnostic WarningDiagnostic = new("b.sysml", 2, 3, DiagnosticSeverity.Warning, "Unused import");
    private static readonly SysmlDiagnostic InfoDiagnostic = new("c.sysml", 4, 5, DiagnosticSeverity.Info, "Consider renaming");

    /// <summary>
    ///     Validates that binding a diagnostics snapshot shows each entry's severity, message, and location.
    /// </summary>
    [Fact]
    public void BindDiagnostics_ShowsSeverityMessageAndLocation()
    {
        // Arrange
        var view = new DiagnosticsListView();

        // Act
        view.BindDiagnostics([ErrorDiagnostic, WarningDiagnostic]);

        // Assert: both entries are visible with their full detail intact
        Assert.Equal(2, view.VisibleDiagnostics.Count);
        Assert.Contains(view.VisibleDiagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Message == "Missing semicolon" && d.FilePath == "a.sysml" && d.Line == 1 && d.Column == 1);
        Assert.Contains(view.VisibleDiagnostics, d => d.Severity == DiagnosticSeverity.Warning && d.Message == "Unused import");
    }

    /// <summary>
    ///     Validates that changing the aggregated diagnostics refreshes the visible list.
    /// </summary>
    [Fact]
    public void DiagnosticsChanged_RefreshesVisibleList()
    {
        // Arrange: an initial snapshot is bound
        var view = new DiagnosticsListView();
        view.BindDiagnostics([ErrorDiagnostic]);
        Assert.Single(view.VisibleDiagnostics);

        // Act: a later aggregate snapshot replaces the first
        view.BindDiagnostics([WarningDiagnostic, InfoDiagnostic]);

        // Assert: the visible list reflects only the newest snapshot
        Assert.Equal(2, view.VisibleDiagnostics.Count);
        Assert.DoesNotContain(view.VisibleDiagnostics, d => d.Equals(ErrorDiagnostic));
    }

    /// <summary>
    ///     Validates that severity filtering narrows the visible list.
    /// </summary>
    [Fact]
    public void ApplyFilters_SeverityFilter_NarrowsVisibleList()
    {
        // Arrange
        var view = new DiagnosticsListView();
        view.BindDiagnostics([ErrorDiagnostic, WarningDiagnostic, InfoDiagnostic]);

        // Act: keep only errors
        var visible = view.ApplyFilters(new HashSet<DiagnosticSeverity> { DiagnosticSeverity.Error }, null);

        // Assert
        Assert.Single(visible);
        Assert.Equal(DiagnosticSeverity.Error, visible[0].Severity);
    }

    /// <summary>
    ///     Validates that free-text search filters diagnostics by message content.
    /// </summary>
    [Fact]
    public void ApplyFilters_SearchText_FiltersByMessage()
    {
        // Arrange
        var view = new DiagnosticsListView();
        view.BindDiagnostics([ErrorDiagnostic, WarningDiagnostic, InfoDiagnostic]);

        // Act
        var visible = view.ApplyFilters(view.SeverityFilter, "unused");

        // Assert
        Assert.Single(visible);
        Assert.Equal(WarningDiagnostic, visible[0]);
    }

    /// <summary>
    ///     Validates that selecting a visible diagnostic stores it as the active selection.
    /// </summary>
    [Fact]
    public void SelectDiagnostic_VisibleEntry_UpdatesSelection()
    {
        // Arrange
        var view = new DiagnosticsListView();
        view.BindDiagnostics([ErrorDiagnostic, WarningDiagnostic]);

        // Act
        view.SelectDiagnostic(WarningDiagnostic);

        // Assert
        Assert.Equal(WarningDiagnostic, view.SelectedDiagnostic);
    }

    /// <summary>
    ///     Validates that a selection that falls outside a newly applied filter is cleared rather than left
    ///     dangling.
    /// </summary>
    [Fact]
    public void ApplyFilters_SelectionFilteredOut_ClearsSelection()
    {
        // Arrange
        var view = new DiagnosticsListView();
        view.BindDiagnostics([ErrorDiagnostic, WarningDiagnostic]);
        view.SelectDiagnostic(WarningDiagnostic);

        // Act: filter down to only errors, hiding the current selection
        view.ApplyFilters(new HashSet<DiagnosticSeverity> { DiagnosticSeverity.Error }, null);

        // Assert
        Assert.Null(view.SelectedDiagnostic);
    }

    /// <summary>
    ///     Validates that selecting a diagnostic not present in the visible list is rejected.
    /// </summary>
    [Fact]
    public void SelectDiagnostic_NotVisible_ThrowsArgumentException()
    {
        // Arrange
        var view = new DiagnosticsListView();
        view.BindDiagnostics([ErrorDiagnostic]);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => view.SelectDiagnostic(WarningDiagnostic));
    }
}
