### LayoutInvoker

![LayoutRenderingSubsystem Structure](LayoutRenderingSubsystemView.svg)

#### Purpose

LayoutInvoker is the adapter between the workbench's selected view state and
the SysML2Tools layout-and-render engine, producing the finished SVG text that
will be displayed by SvgCanvasHost.

> **Deviation from the original design sketch**: the drafted
> `BuildPredefinedLayout`/`BuildCustomLayout` (returning a `LayoutGraph`) plus a
> separate `RenderToSvg` step do not match the real SysML2Tools API.
> `DemaConsulting.SysML2Tools.Rendering.DiagramRenderer.RenderWorkspace` fuses
> layout and rendering into a single call that returns finished `RenderOutput`
> SVG bytes directly - there is no public `LayoutGraph`/`ILayoutStrategy`
> registry; that plumbing is internal to SysML2Tools and never touched by this
> unit. LayoutInvoker therefore exposes `RenderPredefinedView` and
> `RenderCustomView`, both returning ready-to-display SVG text directly, with no
> intermediate layout graph exposed to callers.
>
> For custom views, `DiagramRenderer.SynthesizeDynamicView` only accepts a
> single `targetQualifiedName`, which cannot express the architecture's
> required multi-target `expose` semantics. Instead, this unit manually
> constructs a `SysmlViewNode` - mirroring the same construction upstream's
> internal dynamic-view synthesizer performs - with one `ExposeMember`/
> `SysmlEdge` pair per selected target, and adds it to a shallow clone of the
> workspace so the ephemeral preview node never mutates the live loaded
> workspace.

#### Data Model

**DiagramRenderer**: `DiagramRenderer` — SysML2Tools layout-and-render facade,
instantiated once and reused; stateless per call.

**SvgRenderer**: `IRenderer` — SVG output renderer (`DemaConsulting.Rendering.Svg.SvgRenderer`)
supplied to `DiagramRenderer.RenderWorkspace`.

**DefaultRenderOptions**: `RenderOptions` — caller-supplied policies (theme,
scale) applied when the caller does not supply its own.

#### Key Methods

**RenderPredefinedView**: Generates SVG for a catalog-selected view.

- *Parameters*: `SysmlWorkspace workspace`, `ViewDescriptor view`,
  `RenderOptions? options`.
- *Returns*: `string` — SVG markup ready for display.
- *Preconditions*: The workspace is current and the descriptor's `Name`
  resolves a view usage in it. `DiagramRenderer.RenderWorkspace` filters by the
  view's simple `Name`, not its `QualifiedName`, matching the upstream CLI's
  own `--view <name>` behavior.
- *Postconditions*: The returned SVG is self-contained for the current diagram
  and can be loaded by SvgCanvasHost.

**RenderCustomView**: Generates SVG for a GUI-authored custom view.

- *Parameters*: `SysmlWorkspace workspace`, `ViewDefinitionModel definition`,
  `RenderOptions? options`.
- *Returns*: `string` — SVG markup for preview display.
- *Preconditions*: `definition.IsReadyToRender` is true (a view kind and at
  least one expose target are set).
- *Postconditions*: An ephemeral `SysmlViewNode` (name prefixed `$WorkbenchPreview_`)
  is built and rendered against a shallow clone of the workspace; the live
  workspace's declarations are left untouched.

#### Error Handling

LayoutInvoker propagates workspace-resolution failures, unready custom
definitions, and empty render output as `InvalidOperationException` because
callers need to know that no trustworthy diagram was produced. It never
substitutes partial SVG for a failed render.

#### Dependencies

- **WorkspaceModel** — supplies the semantic workspace snapshot to render.
- **ViewCatalogPresenter** — provides predefined view descriptors.
- **ViewDefinitionModel** — provides custom-view definitions.
- **SysML2Tools** — supplies `DiagramRenderer` and the semantic model types
  (`SysmlWorkspace`, `SysmlViewNode`, `ExposeMember`, `SysmlEdge`).
- **Rendering** — supplies `IRenderer`/`SvgRenderer` for SVG output.

#### Callers

- **MainWindowShell**
- **SvgCanvasHost**
