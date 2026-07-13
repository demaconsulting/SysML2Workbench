using DemaConsulting.Rendering.Abstractions;
using DemaConsulting.Rendering.Svg;
using DemaConsulting.SysML2Tools.Rendering;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;

namespace DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;

/// <summary>
///     LayoutInvoker is the adapter between the workbench's selected view state and the SysML2Tools layout and
///     rendering engine, producing the SVG text that will be displayed by <see cref="SvgCanvasHost" />.
/// </summary>
/// <remarks>
///     Deviation from the original design sketch: the drafted <c>BuildPredefinedLayout</c>/<c>BuildCustomLayout</c>
///     (returning a <c>LayoutGraph</c>) plus a separate <c>RenderToSvg</c> step do not match the real
///     SysML2Tools API. <see cref="DemaConsulting.SysML2Tools.Rendering.DiagramRenderer.RenderWorkspace" /> fuses
///     layout and rendering into a single call that returns finished <c>RenderOutput</c> SVG bytes directly - the
///     library's <c>ILayoutStrategy</c>/<c>LayoutTree</c> types are internal plumbing this unit never touches.
///     This unit therefore exposes <see cref="RenderPredefinedView" /> and <see cref="RenderCustomView" />, both
///     returning ready-to-display SVG text directly, with no intermediate layout graph exposed to callers.
///     <para>
///         For custom views, <c>DiagramRenderer.SynthesizeDynamicView</c> only accepts a single
///         <c>targetQualifiedName</c>, which cannot express the architecture's required multi-target
///         <c>expose</c> semantics. Instead, this unit manually constructs a
///         <see cref="DemaConsulting.SysML2Tools.Semantic.Model.SysmlViewNode" /> - mirroring the same
///         construction upstream's internal dynamic-view synthesizer performs, per its own published design
///         documentation - with one <see cref="ExposeMember" />/<see cref="SysmlEdge" /> pair per selected
///         target. The node is added to a shallow clone of the workspace (new <c>SysmlWorkspace</c> sharing
///         <c>Files</c>/<c>StdlibNames</c>/<c>Index</c> but with an independent <c>Declarations</c> dictionary)
///         so the ephemeral preview node never mutates the live loaded workspace - verified empirically that
///         <c>AddDeclaration</c> on the clone leaves the original workspace's <c>Declarations</c> untouched.
///     </para>
/// </remarks>
public sealed class LayoutInvoker
{
    /// <summary>
    ///     Render options applied when the caller does not supply its own.
    /// </summary>
    private static readonly RenderOptions DefaultRenderOptions = new(Themes.Light);

    /// <summary>
    ///     SysML2Tools layout-and-render facade. Instantiated once and reused; this type is stateless per call.
    /// </summary>
    private readonly DiagramRenderer _diagramRenderer = new();

    /// <summary>
    ///     SVG output renderer supplied to <see cref="DiagramRenderer.RenderWorkspace" />.
    /// </summary>
    private readonly IRenderer _svgRenderer = new SvgRenderer();

    /// <summary>
    ///     Generates SVG for a catalog-selected predefined view.
    /// </summary>
    /// <param name="workspace">Current semantic workspace to render from.</param>
    /// <param name="view">Selected predefined view descriptor.</param>
    /// <param name="options">Render options. Defaults to the light theme at natural scale.</param>
    /// <returns>SVG markup for display.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workspace" /> or <paramref name="view" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the view could not be rendered.</exception>
    public string RenderPredefinedView(SysmlWorkspace workspace, ViewDescriptor view, RenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(view);

        // DiagramRenderer.RenderWorkspace filters by the view's simple Name, not its QualifiedName; if two
        // distinct views in different packages share a Name this cannot disambiguate them, matching the
        // upstream CLI's own `--view <name>` behavior - a documented, non-workbench-specific limitation
        var outputs = _diagramRenderer.RenderWorkspace(workspace, _svgRenderer, options ?? DefaultRenderOptions, view.Name);
        if (outputs.Count == 0)
        {
            throw new InvalidOperationException($"Predefined view '{view.QualifiedName}' could not be rendered.");
        }

        return ReadSvgText(outputs[0]);
    }

    /// <summary>
    ///     Generates SVG for a GUI-defined custom view.
    /// </summary>
    /// <param name="workspace">Current semantic workspace to render from.</param>
    /// <param name="definition">Normalized custom-view state. Must be ready to render.</param>
    /// <param name="options">Render options. Defaults to the light theme at natural scale.</param>
    /// <returns>SVG markup for preview display.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workspace" /> or <paramref name="definition" /> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <paramref name="definition" /> is not ready to render, or when rendering produced no output.
    /// </exception>
    public string RenderCustomView(SysmlWorkspace workspace, ViewDefinitionModel definition, RenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(definition);
        if (!definition.IsReadyToRender)
        {
            throw new InvalidOperationException("The custom view definition needs a view kind and at least one expose target before it can be rendered.");
        }

        var previewNode = BuildPreviewViewNode(definition);

        // Clone so the ephemeral preview node is visible to RenderWorkspace without ever mutating the live,
        // shared workspace instance owned by WorkspaceModel
        var previewWorkspace = new SysmlWorkspace
        {
            Files = workspace.Files,
            StdlibNames = workspace.StdlibNames,
            Index = workspace.Index,
            Declarations = new Dictionary<string, SysmlNode>(workspace.Declarations),
        };
        previewWorkspace.AddDeclaration(previewNode.QualifiedName!, previewNode);

        var outputs = _diagramRenderer.RenderWorkspace(previewWorkspace, _svgRenderer, options ?? DefaultRenderOptions, previewNode.Name);
        if (outputs.Count == 0)
        {
            throw new InvalidOperationException("The custom view could not be rendered.");
        }

        return ReadSvgText(outputs[0]);
    }

    /// <summary>
    ///     Manually constructs an ephemeral <see cref="SysmlViewNode" /> representing a custom view's preview,
    ///     mirroring the node shape SysML2Tools' own internal dynamic-view synthesizer builds.
    /// </summary>
    /// <param name="definition">Normalized custom-view state.</param>
    /// <returns>A view node with one expose member and edge per selected target.</returns>
    private static SysmlViewNode BuildPreviewViewNode(ViewDefinitionModel definition)
    {
        // The '$' prefix is illegal in real SysML identifiers, guaranteeing no collision with model-declared
        // names - the same convention SysML2Tools' own synthesizer uses for its generated view names
        var previewName = $"$WorkbenchPreview_{Guid.NewGuid():N}";

        return new SysmlViewNode
        {
            Name = previewName,
            QualifiedName = previewName,
            RenderTargetName = definition.ViewKind!.Value.ToRenderTargetName(),
            ExposeMembers = definition.ExposeTargets.Select(target => new ExposeMember(target, null)).ToList(),
            ResolvedEdges = definition.ExposeTargets
                .Select(target => new SysmlEdge(previewName, target, SysmlEdgeKind.Expose))
                .ToList<SysmlEdge>(),
            FilterExpressionText = definition.FilterExpression,
        };
    }

    /// <summary>
    ///     Reads the full text of a rendered SVG output stream.
    /// </summary>
    /// <param name="output">Render output produced by <see cref="DiagramRenderer.RenderWorkspace" />.</param>
    /// <returns>Complete SVG markup.</returns>
    private static string ReadSvgText(RenderOutput output)
    {
        using var reader = new StreamReader(output.Data);
        return reader.ReadToEnd();
    }
}
