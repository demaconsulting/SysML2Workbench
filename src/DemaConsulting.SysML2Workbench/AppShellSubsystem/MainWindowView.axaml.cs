using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Dock.Model.Core;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the main application window. All region-specific orchestration and
///     validation logic is delegated to <see cref="MainWindowShell" /> (via the four panel view models this
///     class composes into a Dock layout); this class only builds that layout, wires the File menu, and
///     coordinates workspace-open refresh across the panels.
/// </summary>
public partial class MainWindowView : Window
{
    private readonly MainWindowShell _shell;
    private readonly PredefinedViewsToolViewModel _predefinedViewsViewModel;
    private readonly CustomViewBuilderToolViewModel _customViewBuilderViewModel;
    private readonly DiagnosticsToolViewModel _diagnosticsViewModel;
    private readonly DiagramDocumentViewModel _diagramViewModel;

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

        OpenWorkspaceMenuItem.Click += OnOpenWorkspaceClick;
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
