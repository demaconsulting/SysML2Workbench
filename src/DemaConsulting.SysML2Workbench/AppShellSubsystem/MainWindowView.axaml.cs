using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the main application window. All region-specific orchestration and
///     validation logic is delegated to <see cref="MainWindowShell" /> (via the four panel view models this
///     class composes into a Dock layout); this class only builds that layout, wires the File and View menus,
///     and coordinates workspace-open refresh across the panels. The View menu lets a user restore a Tool panel
///     that was closed through Dock's own chrome, reusing the same long-lived panel view model instance so any
///     in-progress panel state survives the close/restore cycle.
/// </summary>
public partial class MainWindowView : Window
{
    private readonly MainWindowShell _shell;
    private readonly PredefinedViewsToolViewModel _predefinedViewsViewModel;
    private readonly CustomViewBuilderToolViewModel _customViewBuilderViewModel;
    private readonly DiagnosticsToolViewModel _diagnosticsViewModel;
    private readonly DiagramDocumentViewModel _diagramViewModel;
    private readonly WorkbenchDockFactory _dockFactory;

    /// <summary>
    ///     Parameterless constructor required by the Avalonia XAML previewer/designer. Not used at runtime.
    /// </summary>
    public MainWindowView()
        : this(DesignTimeShellFactory.Create())
    {
    }

    /// <summary>
    ///     Creates the main window bound to a real, composed shell, and builds the Dock layout hosting the four
    ///     Phase-0 panels over it.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public MainWindowView(MainWindowShell shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));

        InitializeComponent();

        _diagramViewModel = new DiagramDocumentViewModel(_shell) { Id = "Diagram", Title = "Diagram" };
        _predefinedViewsViewModel = new PredefinedViewsToolViewModel(_shell, _diagramViewModel) { Id = "PredefinedViews", Title = "Predefined Views" };
        _customViewBuilderViewModel = new CustomViewBuilderToolViewModel(_shell, _diagramViewModel) { Id = "CustomViewBuilder", Title = "Custom View Builder" };
        _diagnosticsViewModel = new DiagnosticsToolViewModel(_shell) { Id = "Diagnostics", Title = "Diagnostics" };

        var factory = new WorkbenchDockFactory(_predefinedViewsViewModel, _customViewBuilderViewModel, _diagnosticsViewModel, _diagramViewModel);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        WorkbenchDockControl.Layout = (IDock)layout;
        _dockFactory = factory;

        OpenWorkspaceMenuItem.Click += OnOpenWorkspaceClick;

        // Each View-menu item's DataContext is explicitly set to its panel view model (distinct from this
        // window's own DataContext) so the one-way IsChecked binding above resolves against the panel's
        // IsOpen property without requiring any change to the window's binding context.
        PredefinedViewsMenuItem.DataContext = _predefinedViewsViewModel;
        CustomViewBuilderMenuItem.DataContext = _customViewBuilderViewModel;
        DiagnosticsMenuItem.DataContext = _diagnosticsViewModel;
    }

    /// <summary>
    ///     Handles the View menu's "Predefined Views" click by showing or focusing that panel.
    /// </summary>
    private void OnPredefinedViewsMenuItemClick(object? sender, RoutedEventArgs e)
    {
        ShowOrFocusPanel(_predefinedViewsViewModel);
    }

    /// <summary>
    ///     Handles the View menu's "Custom View Builder" click by showing or focusing that panel.
    /// </summary>
    private void OnCustomViewBuilderMenuItemClick(object? sender, RoutedEventArgs e)
    {
        ShowOrFocusPanel(_customViewBuilderViewModel);
    }

    /// <summary>
    ///     Handles the View menu's "Diagnostics" click by showing or focusing that panel.
    /// </summary>
    private void OnDiagnosticsMenuItemClick(object? sender, RoutedEventArgs e)
    {
        ShowOrFocusPanel(_diagnosticsViewModel);
    }

    /// <summary>
    ///     Restores <paramref name="tool" /> to its original dock if it was hidden by a prior close (via
    ///     <see cref="WorkbenchDockFactory" />'s <c>HideToolsOnClose</c> setting, a safe no-op if it is not
    ///     currently hidden), then makes it the active and focused dockable in its owning dock. This never hides
    ///     an already-open panel: clicking a View-menu item for a visible panel simply (re)focuses it.
    /// </summary>
    /// <param name="tool">The panel to show or bring into focus, reusing its existing long-lived instance.</param>
    private void ShowOrFocusPanel(Tool tool)
    {
        _dockFactory.RestoreDockable(tool);
        _dockFactory.SetActiveDockable(tool);

        if (tool.Owner is IDock ownerDock)
        {
            _dockFactory.SetFocusedDockable(ownerDock, tool);
        }
    }

    private async void OnOpenWorkspaceClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open SysML2 Workspace",
            AllowMultiple = false,
        });

        var folderPath = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }

        try
        {
            await _shell.OpenWorkspaceAsync(folderPath);
            _predefinedViewsViewModel.RefreshFromWorkspace();
            _customViewBuilderViewModel.RefreshFromWorkspace();
            _diagnosticsViewModel.RefreshFromWorkspace();
        }
        catch (Exception ex)
        {
            _customViewBuilderViewModel.StatusMessage = $"Failed to open workspace: {ex.Message}";
        }
    }
}
