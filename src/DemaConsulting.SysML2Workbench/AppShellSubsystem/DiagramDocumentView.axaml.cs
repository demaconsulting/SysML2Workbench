using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Svg.Skia;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia view for one open diagram tab's surface. Keeps the pan/zoom pointer handling as direct
///     code-behind (rather than bound commands) since it manipulates transient render transforms and the tab's
///     own canvas viewport state on every pointer move, which is not a natural fit for data binding.
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
        DiagramBorder.PointerCaptureLost += OnDiagramPointerCaptureLost;

        DataContextChanged += OnDataContextChanged;

        if (Design.IsDesignMode)
        {
            DataContext = new DiagramDocumentViewModel(DesignTimeShellFactory.Create(), "design-preview");
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

            // Always rebind to this view instance rather than only the first ever attached (`??=`): Dock can
            // recreate/reattach a tab's DiagramDocumentView while its DiagramDocumentViewModel persists (for
            // example when two custom-preview tabs are created in quick succession), which would otherwise leave
            // the clipboard service anchored to a now-detached, stale view - causing TopLevel.GetTopLevel to
            // resolve null and the clipboard write to silently no-op.
            _viewModel.ClipboardService = new AvaloniaClipboardService(this);
            LoadCurrentDiagram();
        }
    }

    private async void OnCopyAsSysmlMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.CopyAsSysmlAsync();
    }

    private void OnDiagramChanged(object? sender, EventArgs e)
    {
        LoadCurrentDiagram();
    }

    private void OnDiagramPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel is null || !_viewModel.Canvas.IsContentLoaded)
        {
            return;
        }

        var factor = e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
        _viewModel.Canvas.SetZoom(_viewModel.Canvas.ZoomLevel * factor);
        ApplyCanvasTransform();
        e.Handled = true;
    }

    /// <summary>
    ///     Begins a left-button pan gesture. The right button is deliberately excluded: it opens the diagram's
    ///     context menu (the "Copy as SysML" item) instead, and a right-button press was previously starting a
    ///     pan here too - since opening the context menu on release consumes that pointer release before
    ///     <see cref="OnDiagramPointerReleased" /> could observe it, panning was left stuck on until the next
    ///     unrelated pointer press, making the diagram appear to drag itself after every right-click.
    /// </summary>
    private void OnDiagramPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || !_viewModel.Canvas.IsContentLoaded || !e.GetCurrentPoint(DiagramBorder).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isPanning = true;
        _lastPointerPosition = e.GetPosition(DiagramBorder);
    }

    private void OnDiagramPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_viewModel is null || !_isPanning || !_viewModel.Canvas.IsContentLoaded)
        {
            return;
        }

        var position = e.GetPosition(DiagramBorder);
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;

        _viewModel.Canvas.PanViewport(delta);
        ApplyCanvasTransform();
    }

    private void OnDiagramPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
    }

    /// <summary>
    ///     Defensive backstop: if pointer capture is lost mid-pan for any reason other than a normal release
    ///     (for example another control or popup stealing capture), panning must still stop rather than staying
    ///     stuck on until the next pointer press.
    /// </summary>
    private void OnDiagramPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isPanning = false;
    }

    /// <summary>
    ///     Loads this tab's currently active diagram SVG into the on-screen image control.
    /// </summary>
    private void LoadCurrentDiagram()
    {
        if (_viewModel is null || _viewModel.Canvas.CurrentSvg is null)
        {
            return;
        }

        DiagramImage.Source = new SvgImage { Source = SvgSource.LoadFromSvg(_viewModel.Canvas.CurrentSvg) };
        ApplyCanvasTransform();
    }

    /// <summary>
    ///     Reflects this tab's canvas host's current zoom and pan state onto the on-screen render transform.
    /// </summary>
    private void ApplyCanvasTransform()
    {
        if (_viewModel is null)
        {
            return;
        }

        _diagramScaleTransform.ScaleX = _viewModel.Canvas.ZoomLevel;
        _diagramScaleTransform.ScaleY = _viewModel.Canvas.ZoomLevel;
        _diagramTranslateTransform.X = _viewModel.Canvas.ViewportOffset.X;
        _diagramTranslateTransform.Y = _viewModel.Canvas.ViewportOffset.Y;
    }
}
