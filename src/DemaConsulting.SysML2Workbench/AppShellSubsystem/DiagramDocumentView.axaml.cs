using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Svg.Skia;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia view for the single, always-open diagram surface. Keeps the pan/zoom pointer handling as
///     direct code-behind (rather than bound commands) since it manipulates transient render transforms and the
///     shell's canvas viewport state on every pointer move, which is not a natural fit for data binding.
/// </summary>
public partial class DiagramDocumentView : UserControl
{
    private readonly ScaleTransform _diagramScaleTransform = new(1, 1);
    private readonly TranslateTransform _diagramTranslateTransform = new(0, 0);
    private DiagramDocumentViewModel? _viewModel;
    private bool _isPanning;
    private Point _lastPointerPosition;

    /// <summary>
    ///     Constructor used both at runtime (by Dock's view locator) and by the Avalonia XAML previewer/designer.
    /// </summary>
    public DiagramDocumentView()
    {
        InitializeComponent();

        DiagramImage.RenderTransform = new TransformGroup { Children = { _diagramScaleTransform, _diagramTranslateTransform } };

        DiagramBorder.PointerWheelChanged += OnDiagramPointerWheelChanged;
        DiagramBorder.PointerPressed += OnDiagramPointerPressed;
        DiagramBorder.PointerMoved += OnDiagramPointerMoved;
        DiagramBorder.PointerReleased += OnDiagramPointerReleased;

        DataContextChanged += OnDataContextChanged;

        if (Design.IsDesignMode)
        {
            DataContext = new DiagramDocumentViewModel(DesignTimeShellFactory.Create());
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.DiagramChanged -= OnDiagramChanged;
        }

        _viewModel = DataContext as DiagramDocumentViewModel;

        if (_viewModel is not null)
        {
            _viewModel.DiagramChanged += OnDiagramChanged;
            LoadCurrentDiagram();
        }
    }

    private void OnDiagramChanged(object? sender, EventArgs e)
    {
        LoadCurrentDiagram();
    }

    private void OnDiagramPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel is null || !_viewModel.Shell.Canvas.IsContentLoaded)
        {
            return;
        }

        var factor = e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
        _viewModel.Shell.Canvas.SetZoom(_viewModel.Shell.Canvas.ZoomLevel * factor);
        ApplyCanvasTransform();
        e.Handled = true;
    }

    private void OnDiagramPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || !_viewModel.Shell.Canvas.IsContentLoaded)
        {
            return;
        }

        _isPanning = true;
        _lastPointerPosition = e.GetPosition(DiagramBorder);
    }

    private void OnDiagramPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_viewModel is null || !_isPanning || !_viewModel.Shell.Canvas.IsContentLoaded)
        {
            return;
        }

        var position = e.GetPosition(DiagramBorder);
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;

        _viewModel.Shell.Canvas.PanViewport(delta);
        ApplyCanvasTransform();
    }

    private void OnDiagramPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
    }

    /// <summary>
    ///     Loads the shell's currently active diagram SVG into the on-screen image control.
    /// </summary>
    private void LoadCurrentDiagram()
    {
        if (_viewModel is null || _viewModel.Shell.Canvas.CurrentSvg is null)
        {
            return;
        }

        DiagramImage.Source = new SvgImage { Source = SvgSource.LoadFromSvg(_viewModel.Shell.Canvas.CurrentSvg) };
        ApplyCanvasTransform();
    }

    /// <summary>
    ///     Reflects the shell canvas host's current zoom and pan state onto the on-screen render transform.
    /// </summary>
    private void ApplyCanvasTransform()
    {
        if (_viewModel is null)
        {
            return;
        }

        _diagramScaleTransform.ScaleX = _viewModel.Shell.Canvas.ZoomLevel;
        _diagramScaleTransform.ScaleY = _viewModel.Shell.Canvas.ZoomLevel;
        _diagramTranslateTransform.X = _viewModel.Shell.Canvas.ViewportOffset.X;
        _diagramTranslateTransform.Y = _viewModel.Shell.Canvas.ViewportOffset.Y;
    }
}
