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

    [ObservableProperty]
    private string? _emptyStateMessage;

    [ObservableProperty]
    private bool _hasEmptyStateMessage;

    /// <summary>
    ///     Creates the diagnostics tool view model.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public DiagnosticsToolViewModel(MainWindowShell shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _shell.SourcesChanged += (_, _) => RefreshFromWorkspace();
        RefreshFromWorkspace();
    }

    /// <summary>
    ///     Refreshes the diagnostics list from current shell state after a workspace open or reload.
    /// </summary>
    /// <remarks>
    ///     When there are zero diagnostics to show, distinguishes why: a zero-source workspace shows
    ///     "No diagnostics - workspace is empty", while a loaded workspace with no actual problems shows the
    ///     differently worded "No issues found" - these are deliberately phrased differently so a user cannot
    ///     mistake an empty workspace for a clean one.
    /// </remarks>
    public void RefreshFromWorkspace()
    {
        VisibleDiagnostics = _shell.Diagnostics.VisibleDiagnostics;
        EmptyStateMessage = VisibleDiagnostics.Count switch
        {
            0 when _shell.CurrentWorkspace.Sources.Count == 0 => "No diagnostics - workspace is empty",
            0 => "No issues found",
            _ => null,
        };
        HasEmptyStateMessage = EmptyStateMessage is not null;
    }
}
