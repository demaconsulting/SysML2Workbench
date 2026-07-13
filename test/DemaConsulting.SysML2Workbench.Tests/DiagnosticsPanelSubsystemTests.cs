using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     Subsystem-level tests exercising DiagnosticsPanelSubsystem's unit (<see cref="DiagnosticsListView" />),
///     per docs/reqstream/sysml2-workbench/diagnostics-panel-subsystem.yaml.
/// </summary>
public sealed class DiagnosticsPanelSubsystemTests
{
    /// <summary>
    ///     Validates that binding an aggregated diagnostics snapshot shows each diagnostic's detail.
    /// </summary>
    [Fact]
    public void BindDiagnostics_ShowsDiagnosticDetails()
    {
        // Arrange
        var view = new DiagnosticsListView();
        var diagnostics = new[]
        {
            new SysmlDiagnostic("Sample.sysml", 3, 7, DiagnosticSeverity.Error, "Unresolved reference 'Foo'"),
        };

        // Act
        view.BindDiagnostics(diagnostics);

        // Assert
        var shown = Assert.Single(view.VisibleDiagnostics);
        Assert.Equal("Sample.sysml", shown.FilePath);
        Assert.Equal(3, shown.Line);
        Assert.Equal(7, shown.Column);
        Assert.Equal("Unresolved reference 'Foo'", shown.Message);
    }

    /// <summary>
    ///     Validates that a changed diagnostics aggregate refreshes the visible list.
    /// </summary>
    [Fact]
    public void DiagnosticsChanged_RefreshesList()
    {
        // Arrange
        var view = new DiagnosticsListView();
        view.BindDiagnostics([new SysmlDiagnostic("A.sysml", 1, 1, DiagnosticSeverity.Warning, "First pass")]);

        // Act: the workspace reloads and produces a different diagnostic set
        view.BindDiagnostics([
            new SysmlDiagnostic("B.sysml", 2, 2, DiagnosticSeverity.Error, "Second pass"),
            new SysmlDiagnostic("C.sysml", 3, 3, DiagnosticSeverity.Info, "Third pass"),
        ]);

        // Assert
        Assert.Equal(2, view.VisibleDiagnostics.Count);
        Assert.DoesNotContain(view.VisibleDiagnostics, d => d.Message == "First pass");
    }
}
