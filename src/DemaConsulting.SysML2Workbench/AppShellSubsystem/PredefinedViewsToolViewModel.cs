using CommunityToolkit.Mvvm.ComponentModel;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Dock tool view model backing the "Predefined Views" panel. Holds no rendering logic itself; it only
///     exposes <see cref="MainWindowShell" />-derived state that <see cref="PredefinedViewsToolView" /> binds to,
///     and forwards a successful selection to the shell. This view model deliberately tracks no diagram tab
///     itself: 100% of the "which tab, create-or-reuse, repaint, focus" decision is made centrally by
///     <see cref="MainWindowShell" /> and its <see cref="MainWindowShell.TabsChanged" /> notification, which the
///     Avalonia-aware composition root reconciles against Dock.
/// </summary>
public partial class PredefinedViewsToolViewModel : Dock.Model.Mvvm.Controls.Tool
{
    private readonly MainWindowShell _shell;

    [ObservableProperty]
    private IReadOnlyList<ViewDescriptor> _availableViews = [];

    [ObservableProperty]
    private ViewDescriptor? _selectedView;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isWorkspaceEmpty;

    /// <summary>
    ///     Creates the predefined-views tool view model.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public PredefinedViewsToolViewModel(MainWindowShell shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _shell.SourcesChanged += (_, _) => RefreshFromWorkspace();
        RefreshFromWorkspace();
    }

    /// <summary>
    ///     Refreshes the available-views list from current shell state after a workspace open or reload.
    /// </summary>
    public void RefreshFromWorkspace()
    {
        AvailableViews = _shell.ViewCatalog.AvailableViews;
        SelectedView = null;
        StatusMessage = null;
        IsWorkspaceEmpty = _shell.CurrentWorkspace.Sources.Count == 0;
    }

    partial void OnSelectedViewChanged(ViewDescriptor? value)
    {
        if (value is null)
        {
            return;
        }

        try
        {
            _shell.SelectPredefinedView(value.QualifiedName);
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to render '{value.DisplayName}': {ex.Message}";
        }
    }
}
