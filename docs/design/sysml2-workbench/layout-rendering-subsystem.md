## LayoutRenderingSubsystem

![LayoutRenderingSubsystem Structure](LayoutRenderingSubsystemView.svg)

### Overview

LayoutRenderingSubsystem turns a selected predefined or custom view into pixels
visible in the application window. Its boundary begins with a normalized view
selection or custom-view definition and ends with an SVG payload displayed
through the UI canvas. It contains LayoutInvoker and SvgCanvasHost. Workspace
loading and view selection remain outside the subsystem; this subsystem only
translates already chosen view content into a displayed diagram.

### Interfaces

**Render Request API**: In-process operation for rendering a selected view.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Accepts a predefined view descriptor or custom-view definition
  and returns a rendered SVG representation suitable for immediate display.
- *Constraints*: Render requests must tolerate workspace diagnostics, must
  preserve the user's current selection context, and should not mutate
  workspace state.

**Layout Engine Integration**: The semantic-to-rendered-SVG boundary implemented
through the SysML2Tools and DemaConsulting.Rendering packages.

- *Type*: In-process .NET API.
- *Role*: Consumer.
- *Contract*: Consumes the semantic workspace and a view name, and invokes
  `DemaConsulting.SysML2Tools.Rendering.DiagramRenderer.RenderWorkspace` -
  which fuses layout and SVG rendering into a single call - to obtain
  finished SVG output. There is no intermediate `LayoutGraph` or public
  layout-strategy registry; layout is internal plumbing this subsystem never
  touches directly.
- *Constraints*: The local subsystem must treat layout generation as delegated
  logic and must not fork or replace the OTS layout algorithms.

**Diagram Canvas API**: The view-model-to-control boundary for displaying SVG
output.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Loads rendered SVG into the canvas host and exposes pan, zoom,
  and reset operations for the active diagram.
- *Constraints*: Presentation state must be reset or preserved deliberately
  when the rendered content changes.

### Design

1. LayoutInvoker receives the chosen predefined view descriptor or custom-view
   definition from AppShellSubsystem.
2. The invoker obtains the semantic workspace snapshot from WorkspaceModel and
   calls `DiagramRenderer.RenderWorkspace` - passing an `IRenderer`
   (`DemaConsulting.Rendering.Svg.SvgRenderer`) - which performs layout and SVG
   rendering together and returns finished SVG output directly; for
   GUI-authored custom views with multiple `expose` targets, the invoker first
   builds an ephemeral `SysmlViewNode` on a shallow clone of the workspace,
   since the library's single-target `SynthesizeDynamicView` helper cannot
   express multi-target exposure.
3. SvgCanvasHost loads the SVG into the Avalonia diagram surface and manages
   pan, zoom, and viewport reset behavior.
4. Failures at render time are surfaced back through AppShellSubsystem and
   recorded by LoggingSubsystem without corrupting workspace state.
