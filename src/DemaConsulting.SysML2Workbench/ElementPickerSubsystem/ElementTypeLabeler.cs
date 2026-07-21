using DemaConsulting.SysML2Tools.Semantic.Model;

namespace DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

/// <summary>
///     Shared, static helper that maps a <see cref="SysmlNode" /> to a short, human-readable
///     "type label" used by <see cref="ElementPickerViewModel" />'s type-filter chips (for
///     example, <c>"part def"</c>, <c>"part"</c>, <c>"package"</c>, <c>"view"</c>).
/// </summary>
/// <remarks>
///     Extracted from <c>ViewBuilderDialogViewModel</c>'s previous private
///     <c>GetExposeTypeLabel</c>/<c>GetFallbackExposeTypeLabel</c> pair and generalized so
///     any dialog exposing the element picker (Custom View Builder, Query dialog) can compute
///     the same, consistent label for the same node. The full-taxonomy switch table unions
///     what <c>ViewBuilderDialogViewModel</c> already covered with the additional node kinds
///     <c>DemaConsulting.SysML2Tools.Query.QueryEngine.DescribeKind</c> covers, so a picker
///     restricted to nothing (like the Query dialog's) still shows a meaningful label for
///     every node kind that can appear in <c>SysmlWorkspace.Declarations</c>. Kept as a plain
///     static class (rather than a virtual/injectable seam) because the mapping is a pure
///     data-lookup with no external dependencies and no reason to differ between callers.
///     Thread-safe: no shared state.
/// </remarks>
public static class ElementTypeLabeler
{
    /// <summary>
    ///     Computes the human-readable "type label" for a single candidate element's
    ///     underlying <see cref="SysmlNode" />. Known node kinds map to a keyword-style label
    ///     (<see cref="SysmlDefinitionNode.DefinitionKeyword" />,
    ///     <see cref="SysmlFeatureNode.FeatureKeyword" />,
    ///     <see cref="SysmlConnectionNode.ConnectionKeyword" />, or a fixed literal for kinds
    ///     with no keyword of their own), and any other node kind falls back to a defensively
    ///     derived label so a future/unrecognized subtype still gets a reasonable label rather
    ///     than a raw <c>Sysml...Node</c> class name.
    /// </summary>
    /// <param name="node">The candidate element's underlying node. Must not be <see langword="null" />.</param>
    /// <returns>
    ///     A short, lowercase, human-readable type label such as <c>"part def"</c>,
    ///     <c>"part"</c>, <c>"package"</c>, or <c>"view"</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node" /> is <see langword="null" />.</exception>
    public static string GetTypeLabel(SysmlNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        // Dispatch by concrete subtype in the order that matters for correctness: definition/
        // feature/connection each carry their own keyword text, so consult that first; the
        // remaining subtypes have no keyword-like member and map to a fixed literal each.
        return node switch
        {
            SysmlDefinitionNode definition => definition.DefinitionKeyword,
            SysmlFeatureNode feature => feature.FeatureKeyword,
            SysmlConnectionNode connection => connection.ConnectionKeyword,
            SysmlImportNode => "import",
            SysmlPackageNode => "package",
            SysmlViewNode => "view",
            SysmlViewpointNode => "viewpoint",
            SysmlTransitionNode => "transition",
            SysmlSatisfyNode => "satisfy",
            SysmlDependencyNode => "dependency",
            _ => GetFallbackTypeLabel(node),
        };
    }

    /// <summary>
    ///     Derives a defensive fallback type label for a node kind not otherwise recognized by
    ///     <see cref="GetTypeLabel" />, by stripping a leading <c>Sysml</c> and/or trailing
    ///     <c>Node</c> from the node's runtime type name and lowercasing the result, so an
    ///     unrecognized future node kind still gets a reasonable label instead of a raw class
    ///     name or crashing.
    /// </summary>
    /// <param name="node">The candidate element's underlying node.</param>
    /// <returns>A best-effort, lowercase fallback type label derived from the node's runtime type name.</returns>
    private static string GetFallbackTypeLabel(SysmlNode node)
    {
        var name = node.GetType().Name;

        if (name.StartsWith("Sysml", StringComparison.Ordinal))
        {
            name = name["Sysml".Length..];
        }

        if (name.EndsWith("Node", StringComparison.Ordinal))
        {
            name = name[..^"Node".Length];
        }

        return name.ToLowerInvariant();
    }
}
