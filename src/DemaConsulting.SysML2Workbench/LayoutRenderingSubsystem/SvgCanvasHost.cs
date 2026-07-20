using Avalonia;

namespace DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;

/// <summary>
///     SvgCanvasHost owns the presentation state for the active diagram: the loaded SVG markup, the current zoom
///     factor, and the current pan offset. It is deliberately Avalonia-control-agnostic (referencing only
///     <see cref="Point" />) so its pan/zoom bookkeeping logic can be unit tested without booting the Avalonia UI
///     thread; the thin <c>.axaml</c>/<c>.axaml.cs</c> wrapper that hosts the actual
///     <c>Svg.Controls.Skia.Avalonia</c> control binds to this class's state.
/// </summary>
public sealed class SvgCanvasHost
{
    /// <summary>
    ///     Smallest zoom factor accepted by <see cref="SetZoom" />.
    /// </summary>
    public const double MinZoom = 0.1;

    /// <summary>
    ///     Largest zoom factor accepted by <see cref="SetZoom" />.
    /// </summary>
    public const double MaxZoom = 8.0;

    /// <summary>
    ///     SVG markup currently loaded into the canvas, or <see langword="null" /> when nothing has been loaded yet.
    /// </summary>
    public string? CurrentSvg { get; private set; }

    /// <summary>
    ///     Current scale factor applied to the diagram.
    /// </summary>
    public double ZoomLevel { get; private set; } = 1.0;

    /// <summary>
    ///     Current pan offset for the visible canvas viewport.
    /// </summary>
    public Point ViewportOffset { get; private set; } = default;

    /// <summary>
    ///     Indicates whether a renderable SVG payload is currently available.
    /// </summary>
    public bool IsContentLoaded => CurrentSvg is not null;

    /// <summary>
    ///     Replaces the visible diagram with new SVG content.
    /// </summary>
    /// <param name="svg">Rendered markup for the selected view.</param>
    /// <param name="resetViewport">
    ///     Whether to reset zoom to 1.0 and pan to the origin. Defaults to <see langword="true" />, matching the
    ///     expected behavior when switching between different views; callers re-rendering the same view after an
    ///     incremental reload may pass <see langword="false" /> to preserve the user's current pan/zoom.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="svg" /> is null or empty.</exception>
    public void LoadSvg(string svg, bool resetViewport = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(svg);

        CurrentSvg = svg;
        if (resetViewport)
        {
            ZoomLevel = 1.0;
            ViewportOffset = default;
        }
    }

    /// <summary>
    ///     Clears the visible diagram, discarding any currently loaded SVG content and resetting zoom and pan to
    ///     their defaults (mirroring <see cref="LoadSvg" />'s <c>resetViewport: true</c> behavior). Intended for
    ///     callers whose current configuration no longer corresponds to any renderable content - for example an
    ///     edited definition that has become invalid - so a stale, previously-rendered diagram is never left on
    ///     screen alongside on-screen state it no longer describes.
    /// </summary>
    public void Clear()
    {
        CurrentSvg = null;
        ZoomLevel = 1.0;
        ViewportOffset = default;
    }

    /// <summary>
    ///     Adjusts the current zoom factor.
    /// </summary>
    /// <param name="zoomLevel">Requested scale factor.</param>
    /// <exception cref="InvalidOperationException">Thrown when no content is loaded.</exception>
    public void SetZoom(double zoomLevel)
    {
        if (!IsContentLoaded)
        {
            throw new InvalidOperationException("A diagram must be loaded before zoom can be adjusted.");
        }

        // Out-of-range requests are clamped locally rather than rejected, per this unit's documented error
        // handling policy
        ZoomLevel = Math.Clamp(zoomLevel, MinZoom, MaxZoom);
    }

    /// <summary>
    ///     Moves the visible region across the diagram.
    /// </summary>
    /// <param name="delta">Requested offset change.</param>
    /// <exception cref="InvalidOperationException">Thrown when no content is loaded.</exception>
    public void PanViewport(Point delta)
    {
        if (!IsContentLoaded)
        {
            throw new InvalidOperationException("A diagram must be loaded before it can be panned.");
        }

        ViewportOffset += delta;
    }
}
