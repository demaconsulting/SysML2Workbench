### SvgCanvasHost

![LayoutRenderingSubsystem Structure](LayoutRenderingSubsystemView.svg)

#### Purpose

SvgCanvasHost owns the presentation state for the active diagram and displays
rendered SVG inside the Avalonia UI with pan and zoom interactions. It is
deliberately Avalonia-control-agnostic (referencing only `Avalonia.Point`) so
its pan/zoom bookkeeping can be unit tested without booting the Avalonia UI
thread; the actual `.axaml`/`.axaml.cs` view hosts an
`Svg.Controls.Skia.Avalonia` `SvgImage`/`Image` control and binds to this
class's state.

#### Data Model

**CurrentSvg**: `string?` — SVG markup currently loaded into the canvas.

**ZoomLevel**: `double` — current scale factor applied to the diagram, clamped
to `[MinZoom, MaxZoom]` = `[0.1, 8.0]`.

**ViewportOffset**: `Point` — current pan offset for the visible canvas
viewport.

**IsContentLoaded**: `bool` — indicates whether a renderable SVG payload is
currently available.

#### Key Methods

**LoadSvg**: Replaces the visible diagram with new SVG content.

- *Parameters*: `string svg` — rendered markup for the selected view;
  `bool resetViewport = true` — whether to reset zoom/pan to their defaults
  (callers re-rendering the same view after an incremental reload may pass
  `false` to preserve the user's current pan/zoom).
- *Returns*: `void` — canvas state updates in place.
- *Preconditions*: `svg` is non-empty.
- *Postconditions*: `CurrentSvg` and `IsContentLoaded` reflect the new diagram
  and the viewport is reset or preserved per `resetViewport`.

**SetZoom**: Adjusts the current zoom factor.

- *Parameters*: `double zoomLevel` — requested scale factor.
- *Returns*: `void` — zoom state updates in place.
- *Preconditions*: Content is loaded.
- *Postconditions*: `ZoomLevel` is clamped to `[0.1, 8.0]` and set to the
  accepted value.

**PanViewport**: Moves the visible region across the diagram.

- *Parameters*: `Point delta` — requested offset change.
- *Returns*: `void` — viewport position updates in place.
- *Preconditions*: Content is loaded.
- *Postconditions*: `ViewportOffset` reflects the accepted pan movement.

**Clear**: Discards the currently displayed diagram and resets zoom and pan to
their defaults.

- *Parameters*: none.
- *Returns*: `void` — canvas state updates in place.
- *Preconditions*: none.
- *Postconditions*: `CurrentSvg` is `null`, `IsContentLoaded` is `false`, and
  `ZoomLevel`/`ViewportOffset` are reset to their defaults, mirroring
  `LoadSvg`'s `resetViewport: true` behavior. Intended for callers whose
  current configuration no longer corresponds to any renderable content (for
  example `ViewBuilderDialogViewModel.RenderPreview` after an edit makes its
  definition invalid), so a stale, previously-rendered diagram is never left
  on screen.

#### Error Handling

SvgCanvasHost handles out-of-range zoom requests locally by clamping them to
`[MinZoom, MaxZoom]`. `SetZoom`/`PanViewport` throw `InvalidOperationException`
when called before any content is loaded, and `LoadSvg` throws
`ArgumentException` for a null or empty SVG payload, because the unit cannot
present the requested diagram.

#### Dependencies

- **LayoutInvoker** — supplies SVG markup for the active diagram.
- **Avalonia** — provides `Point` and, in the real UI, the control tree, input
  events, and application dispatcher.
- **Svg.Controls.Skia.Avalonia** — the real `.axaml` view's `SvgImage` control
  that renders the SVG markup held by this class.
- **MainWindowShell** — hosts the canvas and forwards user interaction
  commands.

#### Callers

- **MainWindowShell**
