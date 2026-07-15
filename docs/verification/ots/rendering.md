## Rendering

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/RenderingTests.cs` verify the SVG output produced by `DemaConsulting.Rendering.Svg.SvgRenderer` when invoked through `DemaConsulting.SysML2Tools.Rendering.DiagramRenderer.RenderWorkspace`, exercised with a representative rendered view, because the dependency is consumed directly at the rendering boundary.

### Test Scenarios

**RenderLayout_ProducesSvgDocument**: A representative view is rendered end to end and the dependency produces a well-formed, self-contained SVG document (opening `<svg` element and matching closing tag) that can be hosted by the application canvas.

**RenderLayout_PreservesDiagramPrimitives**: The same rendered SVG is inspected for the exposed model element's label, proving the dependency preserves the diagram primitives (element labels) the desktop shell's SVG canvas needs for interactive viewing, rather than losing model content during rendering.
