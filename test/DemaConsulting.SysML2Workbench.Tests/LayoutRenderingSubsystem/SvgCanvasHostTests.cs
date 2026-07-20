using Avalonia;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.LayoutRenderingSubsystem;

/// <summary>
///     Unit tests for <see cref="SvgCanvasHost" />.
/// </summary>
public sealed class SvgCanvasHostTests
{
    /// <summary>
    ///     Sample SVG payload used across tests.
    /// </summary>
    private const string SampleSvg = "<svg xmlns='http://www.w3.org/2000/svg'></svg>";

    /// <summary>
    ///     Validates that loading an SVG document displays the diagram canvas with default zoom and pan state.
    /// </summary>
    [Fact]
    public void LoadSvgDocument_DisplaysDiagramCanvas()
    {
        // Arrange
        var host = new SvgCanvasHost();

        // Act
        host.LoadSvg(SampleSvg);

        // Assert
        Assert.True(host.IsContentLoaded);
        Assert.Equal(SampleSvg, host.CurrentSvg);
        Assert.Equal(1.0, host.ZoomLevel);
        Assert.Equal(default, host.ViewportOffset);
    }

    /// <summary>
    ///     Validates that user interaction can both pan and zoom a loaded diagram, and that out-of-range zoom
    ///     requests are clamped rather than rejected.
    /// </summary>
    [Fact]
    public void UserInteraction_PansAndZoomsDiagram()
    {
        // Arrange
        var host = new SvgCanvasHost();
        host.LoadSvg(SampleSvg);

        // Act: zoom within range, then pan
        host.SetZoom(2.5);
        host.PanViewport(new Point(20, -10));

        // Assert
        Assert.Equal(2.5, host.ZoomLevel);
        Assert.Equal(new Point(20, -10), host.ViewportOffset);

        // Act: zoom far outside the supported range
        host.SetZoom(1000);

        // Assert: the zoom is clamped to the configured maximum
        Assert.Equal(SvgCanvasHost.MaxZoom, host.ZoomLevel);
    }

    /// <summary>
    ///     Validates that zoom and pan operations are rejected when no diagram has been loaded.
    /// </summary>
    [Fact]
    public void SetZoomOrPan_NoContentLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        var host = new SvgCanvasHost();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => host.SetZoom(1.5));
        Assert.Throws<InvalidOperationException>(() => host.PanViewport(new Point(1, 1)));
    }

    /// <summary>
    ///     Validates that clearing a loaded diagram discards its SVG content and resets zoom and pan to their
    ///     defaults, so a caller whose current configuration no longer corresponds to any renderable content can
    ///     blank the canvas rather than leaving a stale diagram on screen.
    /// </summary>
    [Fact]
    public void Clear_ContentLoaded_DiscardsSvgAndResetsViewport()
    {
        // Arrange
        var host = new SvgCanvasHost();
        host.LoadSvg(SampleSvg);
        host.SetZoom(2.5);
        host.PanViewport(new Point(20, -10));

        // Act
        host.Clear();

        // Assert
        Assert.False(host.IsContentLoaded);
        Assert.Null(host.CurrentSvg);
        Assert.Equal(1.0, host.ZoomLevel);
        Assert.Equal(default, host.ViewportOffset);
    }
}
