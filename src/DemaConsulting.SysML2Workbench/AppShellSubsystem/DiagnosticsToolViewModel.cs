using CommunityToolkit.Mvvm.ComponentModel;
using DemaConsulting.SysML2Tools.Parser;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Dock tool view model backing the "Diagnostics" panel. Exposes the shell's currently visible diagnostics
///     list for <see cref="DiagnosticsToolView" /> to bind to.
/// </summary>
public partial class DiagnosticsToolViewModel : Dock.Model.Mvvm.Controls.Tool
{
    private readonly MainWindowShell _shell;

    [ObservableProperty]
    private IReadOnlyList<SysmlDiagnostic> _visibleDiagnostics = [];

    /// <summary>
    ///     Creates the diagnostics tool view model.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public DiagnosticsToolViewModel(MainWindowShell shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    /// <summary>
    ///     Refreshes the diagnostics list from current shell state after a workspace open or reload.
    /// </summary>
    public void RefreshFromWorkspace()
    {
        VisibleDiagnostics = _shell.Diagnostics.VisibleDiagnostics;
    }
}
