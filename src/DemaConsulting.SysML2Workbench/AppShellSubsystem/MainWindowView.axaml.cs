using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Svg.Skia;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the main application window. All orchestration and validation logic is
///     delegated to <see cref="MainWindowShell" />; this class only translates user gestures into shell calls
///     and reflects shell state back into the visible controls.
/// </summary>
public partial class MainWindowView : Window
{
    /// <summary>
    ///     Node kinds excluded from the expose-target picker, mirroring <see cref="ViewDefinitionModel" />'s own
    ///     validation rules so the user cannot select a target that would fail validation.
    /// </summary>
    private static readonly Type[] DisallowedExposeNodeTypes =
    [
        typeof(SysmlViewNode),
        typeof(SysmlViewpointNode),
        typeof(SysmlImportNode),
        typeof(SysmlMetadataNode),
        typeof(SysmlTransitionNode),
        typeof(SysmlConnectionNode),
    ];

    private readonly MainWindowShell _shell;
    private readonly ScaleTransform _diagramScaleTransform = new(1, 1);
    private readonly TranslateTransform _diagramTranslateTransform = new(0, 0);
    private bool _isPanning;
    private Point _lastPointerPosition;

    /// <summary>
    ///     Parameterless constructor required by the Avalonia XAML previewer/designer. Not used at runtime.
    /// </summary>
    public MainWindowView()
        : this(DesignTimeShellFactory.Create())
    {
    }

    /// <summary>
    ///     Creates the main window bound to a real, composed shell.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public MainWindowView(MainWindowShell shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));

        InitializeComponent();

        DiagramImage.RenderTransform = new TransformGroup { Children = { _diagramScaleTransform, _diagramTranslateTransform } };

        PredefinedViewsListBox.DisplayMemberBinding = new Binding(nameof(ViewDescriptor.DisplayName));
        ViewKindComboBox.ItemsSource = Enum.GetValues<ViewKind>();

        OpenWorkspaceMenuItem.Click += OnOpenWorkspaceClick;
        PredefinedViewsListBox.SelectionChanged += OnPredefinedViewSelectionChanged;
        PreviewCustomViewButton.Click += OnPreviewCustomViewClick;
        CopyAsSysmlButton.Click += OnCopyAsSysmlClick;

        DiagramBorder.PointerWheelChanged += OnDiagramPointerWheelChanged;
        DiagramBorder.PointerPressed += OnDiagramPointerPressed;
        DiagramBorder.PointerMoved += OnDiagramPointerMoved;
        DiagramBorder.PointerReleased += OnDiagramPointerReleased;
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
            RefreshWorkspaceBoundControls();
            SetBuilderStatus(null);
        }
        catch (Exception ex)
        {
            SetBuilderStatus($"Failed to open workspace: {ex.Message}");
        }
    }

    private void OnPredefinedViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PredefinedViewsListBox.SelectedItem is not ViewDescriptor descriptor)
        {
            return;
        }

        try
        {
            _shell.SelectPredefinedView(descriptor.QualifiedName);
            LoadCurrentDiagram();
            SetBuilderStatus(null);
        }
        catch (Exception ex)
        {
            SetBuilderStatus($"Failed to render '{descriptor.DisplayName}': {ex.Message}");
        }
    }

    private void OnPreviewCustomViewClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var definition = BuildDefinitionFromBuilderControls();
            _shell.PreviewCustomView(definition);
            LoadCurrentDiagram();
            SetBuilderStatus(null);
        }
        catch (Exception ex)
        {
            SetBuilderStatus($"Preview failed: {ex.Message}");
        }
    }

    private async void OnCopyAsSysmlClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var definition = BuildDefinitionFromBuilderControls();
            var snippet = _shell.ExportCustomViewSnippet(definition);

            var topLevel = GetTopLevel(this);
            if (topLevel?.Clipboard is not null)
            {
                await topLevel.Clipboard.SetTextAsync(snippet);
            }

            SetBuilderStatus("Copied SysML snippet to clipboard.");
        }
        catch (Exception ex)
        {
            SetBuilderStatus($"Export failed: {ex.Message}");
        }
    }

    private void OnDiagramPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!_shell.Canvas.IsContentLoaded)
        {
            return;
        }

        var factor = e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
        _shell.Canvas.SetZoom(_shell.Canvas.ZoomLevel * factor);
        ApplyCanvasTransform();
        e.Handled = true;
    }

    private void OnDiagramPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_shell.Canvas.IsContentLoaded)
        {
            return;
        }

        _isPanning = true;
        _lastPointerPosition = e.GetPosition(DiagramBorder);
    }

    private void OnDiagramPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || !_shell.Canvas.IsContentLoaded)
        {
            return;
        }

        var position = e.GetPosition(DiagramBorder);
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;

        _shell.Canvas.PanViewport(delta);
        ApplyCanvasTransform();
    }

    private void OnDiagramPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
    }

    /// <summary>
    ///     Refreshes the predefined view list, expose-target picker, and diagnostics list from current shell
    ///     state after a workspace open or reload.
    /// </summary>
    private void RefreshWorkspaceBoundControls()
    {
        PredefinedViewsListBox.ItemsSource = _shell.ViewCatalog.AvailableViews;
        DiagnosticsListBox.ItemsSource = _shell.Diagnostics.VisibleDiagnostics;

        ExposeTargetsListBox.ItemsSource = _shell.CurrentWorkspace is null
            ? []
            : _shell.CurrentWorkspace.Workspace.Declarations
                .Where(kvp => !_shell.CurrentWorkspace.Workspace.StdlibNames.Contains(kvp.Key))
                .Where(kvp => !DisallowedExposeNodeTypes.Contains(kvp.Value.GetType()))
                .Select(kvp => kvp.Key)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
    }

    /// <summary>
    ///     Builds a <see cref="ViewDefinitionModel" /> from the current custom-view builder control values.
    /// </summary>
    /// <returns>Normalized custom-view state.</returns>
    private ViewDefinitionModel BuildDefinitionFromBuilderControls()
    {
        var definition = new ViewDefinitionModel();

        if (ViewKindComboBox.SelectedItem is ViewKind viewKind)
        {
            definition.SetViewKind(viewKind);
        }

        var selectedTargets = ExposeTargetsListBox.SelectedItems?
            .Cast<string>()
            .ToList() ?? [];
        definition.SetExposeTargets(selectedTargets);

        definition.SetFilterExpression(FilterExpressionTextBox.Text);
        definition.SetDisplayName(DisplayNameTextBox.Text);

        return definition;
    }

    /// <summary>
    ///     Loads the shell's currently active diagram SVG into the on-screen image control.
    /// </summary>
    private void LoadCurrentDiagram()
    {
        if (_shell.Canvas.CurrentSvg is null)
        {
            return;
        }

        DiagramImage.Source = new SvgImage { Source = SvgSource.LoadFromSvg(_shell.Canvas.CurrentSvg) };
        ApplyCanvasTransform();
    }

    /// <summary>
    ///     Reflects the shell canvas host's current zoom and pan state onto the on-screen render transform.
    /// </summary>
    private void ApplyCanvasTransform()
    {
        _diagramScaleTransform.ScaleX = _shell.Canvas.ZoomLevel;
        _diagramScaleTransform.ScaleY = _shell.Canvas.ZoomLevel;
        _diagramTranslateTransform.X = _shell.Canvas.ViewportOffset.X;
        _diagramTranslateTransform.Y = _shell.Canvas.ViewportOffset.Y;
    }

    /// <summary>
    ///     Shows or clears the custom-view builder status message.
    /// </summary>
    /// <param name="message">Message to display, or <see langword="null" /> to clear it.</param>
    private void SetBuilderStatus(string? message)
    {
        BuilderStatusTextBlock.Text = message ?? string.Empty;
    }
}
