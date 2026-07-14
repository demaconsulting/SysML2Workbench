using CommunityToolkit.Mvvm.ComponentModel;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Dock tool view model backing the "Predefined Views" panel. Holds no rendering logic itself; it only
///     exposes <see cref="MainWindowShell" />-derived state that <see cref="PredefinedViewsToolView" /> binds to,
///     and forwards a successful selection to the shell before notifying <see cref="DiagramViewModel" /> that the
///     diagram surface needs to repaint.
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

    /// <summary>
    ///     Creates the predefined-views tool view model.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    /// <param name="diagramViewModel">Diagram document view model to notify after a successful selection.</param>
    public PredefinedViewsToolViewModel(MainWindowShell shell, DiagramDocumentViewModel diagramViewModel)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        DiagramViewModel = diagramViewModel ?? throw new ArgumentNullException(nameof(diagramViewModel));
    }

    /// <summary>
    ///     Diagram document view model to refresh after a predefined view is successfully selected.
    /// </summary>
    public DiagramDocumentViewModel DiagramViewModel { get; }

    /// <summary>
    ///     Refreshes the available-views list from current shell state after a workspace open or reload.
    /// </summary>
    public void RefreshFromWorkspace()
    {
        AvailableViews = _shell.ViewCatalog.AvailableViews;
        SelectedView = null;
        StatusMessage = null;
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
            DiagramViewModel.RaiseDiagramChanged();
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to render '{value.DisplayName}': {ex.Message}";
        }
    }
}
