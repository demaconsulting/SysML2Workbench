namespace DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;

/// <summary>
///     The predefined SysML view kinds supported by the Phase 0 workbench.
/// </summary>
/// <remarks>
///     This mirrors the six view kinds named in the system architecture. SysML2Tools also recognizes a
///     <c>browser</c>/<c>asTreeDiagram</c> kind, which is intentionally excluded from Phase 0 scope.
/// </remarks>
public enum ViewKind
{
    /// <summary>
    ///     General nested block diagram (SysML2Tools render token <c>asGeneralDiagram</c>).
    /// </summary>
    General,

    /// <summary>
    ///     Interconnection diagram (SysML2Tools render token <c>asInterconnectionDiagram</c>).
    /// </summary>
    Interconnection,

    /// <summary>
    ///     State transition diagram (SysML2Tools render token <c>asStateTransitionDiagram</c>).
    /// </summary>
    StateTransition,

    /// <summary>
    ///     Action flow diagram (SysML2Tools render token <c>asActionFlowDiagram</c>).
    /// </summary>
    ActionFlow,

    /// <summary>
    ///     Sequence diagram (SysML2Tools render token <c>asSequenceDiagram</c>).
    /// </summary>
    Sequence,

    /// <summary>
    ///     Grid diagram (SysML2Tools render token <c>asGridDiagram</c>).
    /// </summary>
    Grid,
}

/// <summary>
///     Conversions between <see cref="ViewKind" /> and the real SysML2Tools string tokens it corresponds to.
/// </summary>
/// <remarks>
///     The `render` token (used inside generated SysML text and passed to
///     <see cref="DemaConsulting.SysML2Tools.Semantic.Model.SysmlViewNode.RenderTargetName" />) and the dynamic-view
///     synthesis token (passed as the <c>viewType</c> argument to
///     <see cref="DemaConsulting.SysML2Tools.Rendering.DiagramRenderer.SynthesizeDynamicView" />) are distinct
///     vocabularies confirmed against the real SysML2Tools implementation and its published design documentation.
/// </remarks>
public static class ViewKindExtensions
{
    /// <summary>
    ///     Converts a <see cref="ViewKind" /> to the SysML <c>render</c> body-statement token used both in
    ///     generated SysML text and as <c>SysmlViewNode.RenderTargetName</c>.
    /// </summary>
    /// <param name="kind">View kind to convert.</param>
    /// <returns>The corresponding render token, for example <c>asGeneralDiagram</c>.</returns>
    public static string ToRenderTargetName(this ViewKind kind)
    {
        return kind switch
        {
            ViewKind.General => "asGeneralDiagram",
            ViewKind.Interconnection => "asInterconnectionDiagram",
            ViewKind.StateTransition => "asStateTransitionDiagram",
            ViewKind.ActionFlow => "asActionFlowDiagram",
            ViewKind.Sequence => "asSequenceDiagram",
            ViewKind.Grid => "asGridDiagram",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported view kind."),
        };
    }

    /// <summary>
    ///     Converts a <see cref="ViewKind" /> to the dynamic-view synthesis <c>viewType</c> token accepted by
    ///     <see cref="DemaConsulting.SysML2Tools.Rendering.DiagramRenderer.SynthesizeDynamicView" />.
    /// </summary>
    /// <param name="kind">View kind to convert.</param>
    /// <returns>The corresponding synthesis token, for example <c>general</c>.</returns>
    public static string ToSynthesisToken(this ViewKind kind)
    {
        return kind switch
        {
            ViewKind.General => "general",
            ViewKind.Interconnection => "interconnection",
            ViewKind.StateTransition => "state",
            ViewKind.ActionFlow => "action",
            ViewKind.Sequence => "sequence",
            ViewKind.Grid => "grid",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported view kind."),
        };
    }

    /// <summary>
    ///     Attempts to recognize a SysML <c>render</c> body-statement token as one of the six Phase 0-supported
    ///     view kinds.
    /// </summary>
    /// <param name="renderTargetName">
    ///     The render token to inspect, for example a view node's <c>RenderTargetName</c>. May be null.
    /// </param>
    /// <returns>
    ///     The matching <see cref="ViewKind" />, or <see langword="null" /> when the token is null, empty, or
    ///     names a rendering style outside Phase 0 scope (for example <c>asTreeDiagram</c> or
    ///     <c>asElementTable</c>).
    /// </returns>
    public static ViewKind? FromRenderTargetName(string? renderTargetName)
    {
        return renderTargetName switch
        {
            "asGeneralDiagram" => ViewKind.General,
            "asInterconnectionDiagram" => ViewKind.Interconnection,
            "asStateTransitionDiagram" => ViewKind.StateTransition,
            "asActionFlowDiagram" => ViewKind.ActionFlow,
            "asSequenceDiagram" => ViewKind.Sequence,
            "asGridDiagram" => ViewKind.Grid,
            _ => null,
        };
    }
}
