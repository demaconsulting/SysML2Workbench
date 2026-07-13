## Rendering

SysML2Workbench uses `DemaConsulting.Rendering` as the final diagram-production
stage that turns layout results into SVG suitable for display in the Avalonia
UI.

### Purpose

`DemaConsulting.Rendering` was chosen because it already provides the SVG-
oriented rendering behavior needed by the existing SysML2Tools output pipeline.
Reusing it avoids building a second renderer inside the workbench and preserves
consistency with the batch CLI diagrams users already know.

### Features Used

- **SVG emission** — `DemaConsulting.Rendering.Svg.SvgRenderer` (an
  `IRenderer`) produces the self-contained SVG markup displayed by the
  workbench.
- **Rendering primitives** — the `.Abstractions`/`.Layout` packages supply the
  line, text, and shape rendering behavior used internally by
  `DiagramRenderer.RenderWorkspace` when it lays out and renders a view.

### Integration Pattern

`DemaConsulting.SysML2Tools.Core` only pulls in `DemaConsulting.Rendering.Layout`
as a transitive dependency, so the workbench adds
`DemaConsulting.Rendering.Svg` explicitly to obtain SVG output.
`DiagramRenderer.RenderWorkspace` accepts an `IRenderer` instance
(`SvgRenderer`) directly and returns finished `RenderOutput` SVG data - there is
no separate `LayoutGraph` object that LayoutInvoker passes across a rendering
boundary; layout and rendering happen together inside the single
`RenderWorkspace` call. The package is consumed as a direct library reference
with no separate initialization phase beyond constructing a reusable
`SvgRenderer` instance. Disposal is limited to normal .NET object lifetime
management for the `RenderOutput` stream read while transferring SVG text into
the canvas host.
