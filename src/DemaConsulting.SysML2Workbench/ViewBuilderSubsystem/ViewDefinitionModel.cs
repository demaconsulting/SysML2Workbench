using DemaConsulting.SysML2Tools.Filtering;
using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;

namespace DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;

/// <summary>
///     ViewDefinitionModel captures the complete session-only definition of a custom view so the same normalized
///     state can be used both for live preview rendering and for SysML snippet generation.
/// </summary>
/// <remarks>
///     Deviation from the original design sketch: expose targets are plain qualified-name strings rather than a
///     dedicated <c>QualifiedName</c> value type. The real SysML2Tools semantic model
///     (<see cref="DemaConsulting.SysML2Tools.Semantic.Model.ExposeMember" />,
///     <see cref="DemaConsulting.SysML2Tools.Semantic.Model.SysmlNode.QualifiedName" />) represents qualified
///     names as plain <see cref="string" /> throughout, so introducing a wrapper type here would add ceremony
///     without a corresponding real-API contract to justify it.
/// </remarks>
public sealed class ViewDefinitionModel
{
    /// <summary>
    ///     Sentinel file path used for diagnostics that originate from the custom-view definition itself rather
    ///     than from a workspace file.
    /// </summary>
    private const string DiagnosticSourceName = "<custom-view>";

    /// <summary>
    ///     Node kinds that the real SysML2Tools dynamic-view synthesizer disallows as expose/render targets.
    /// </summary>
    private static readonly Type[] DisallowedTargetNodeTypes =
    [
        typeof(SysmlViewNode),
        typeof(SysmlViewpointNode),
        typeof(SysmlImportNode),
        typeof(SysmlMetadataNode),
        typeof(SysmlTransitionNode),
        typeof(SysmlConnectionNode),
    ];

    /// <summary>
    ///     Ordered, duplicate-free expose targets.
    /// </summary>
    private List<string> _exposeTargets = [];

    /// <summary>
    ///     Selected custom-view rendering style, constrained to the supported SysML view kinds exposed by the UI.
    /// </summary>
    public ViewKind? ViewKind { get; private set; }

    /// <summary>
    ///     Ordered set of packages or elements selected for <c>expose</c> clauses.
    /// </summary>
    public IReadOnlyList<string> ExposeTargets => _exposeTargets;

    /// <summary>
    ///     Optional filter text applied to narrow the rendered content within the selected targets.
    /// </summary>
    public string? FilterExpression { get; private set; }

    /// <summary>
    ///     Optional user-facing view name used when exporting a named snippet.
    /// </summary>
    public string? DisplayName { get; private set; }

    /// <summary>
    ///     Reports whether the current selections contain enough information to render a live preview.
    /// </summary>
    public bool IsReadyToRender => ViewKind is not null && _exposeTargets.Count > 0;

    /// <summary>
    ///     Reports whether the current selections contain enough information to export a SysML snippet.
    /// </summary>
    /// <remarks>
    ///     Phase 0 uses the same readiness conditions for preview and export; the property is kept distinct from
    ///     <see cref="IsReadyToRender" /> because the two concerns are independently documented in requirements
    ///     and could diverge in a future phase (for example, if export later requires a non-empty
    ///     <see cref="DisplayName" />).
    /// </remarks>
    public bool IsReadyToExport => IsReadyToRender;

    /// <summary>
    ///     Changes the target rendering style for the custom view.
    /// </summary>
    /// <param name="viewKind">Selected view kind.</param>
    public void SetViewKind(ViewKind viewKind)
    {
        ViewKind = viewKind;
    }

    /// <summary>
    ///     Replaces the current <c>expose</c> target set.
    /// </summary>
    /// <param name="targets">Selected packages or elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="targets" /> is null.</exception>
    public void SetExposeTargets(IReadOnlyList<string> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        // Preserve requested order while removing exact duplicates, per the unit's documented contract
        var seen = new HashSet<string>(StringComparer.Ordinal);
        _exposeTargets = targets.Where(t => !string.IsNullOrWhiteSpace(t) && seen.Add(t)).ToList();
    }

    /// <summary>
    ///     Sets the optional filter expression applied to narrow the rendered content.
    /// </summary>
    /// <param name="filterExpression">Filter text, or <see langword="null" />/whitespace to clear it.</param>
    public void SetFilterExpression(string? filterExpression)
    {
        FilterExpression = string.IsNullOrWhiteSpace(filterExpression) ? null : filterExpression;
    }

    /// <summary>
    ///     Sets the optional user-facing view name used when exporting a named snippet.
    /// </summary>
    /// <param name="displayName">Display name, or <see langword="null" />/whitespace to clear it.</param>
    public void SetDisplayName(string? displayName)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
    }

    /// <summary>
    ///     Confirms the definition is renderable against a workspace.
    /// </summary>
    /// <remarks>
    ///     This is a best-effort subset of the real SysML2Tools dynamic-view synthesizer's internal per-kind
    ///     structural pre-checks (which are not part of its public API): it confirms every target resolves,
    ///     is not a standard-library element, and is not one of the node kinds the real library disallows as a
    ///     dynamic-view target. It does not attempt the deeper per-kind structural checks (for example,
    ///     "an interconnection target must have at least one nested part") documented for
    ///     <c>SynthesizeDynamicView</c>, since those are internal to SysML2Tools. This is an intentional,
    ///     disclosed limitation.
    /// </remarks>
    /// <param name="workspace">Current loaded workspace.</param>
    /// <returns>Validation findings for the builder UI.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workspace" /> is null.</exception>
    public IReadOnlyList<SysmlDiagnostic> ValidateAgainstWorkspace(SysmlWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var diagnostics = new List<SysmlDiagnostic>();

        if (ViewKind is null)
        {
            diagnostics.Add(Diagnostic("No view kind has been selected."));
        }

        if (_exposeTargets.Count == 0)
        {
            diagnostics.Add(Diagnostic("At least one expose target is required."));
        }

        foreach (var target in _exposeTargets)
        {
            if (!workspace.Declarations.TryGetValue(target, out var node))
            {
                diagnostics.Add(Diagnostic($"Target '{target}' does not resolve in the current workspace."));
                continue;
            }

            if (workspace.StdlibNames.Contains(target))
            {
                diagnostics.Add(Diagnostic($"Target '{target}' is a standard library element and cannot be exposed."));
                continue;
            }

            if (DisallowedTargetNodeTypes.Contains(node.GetType()))
            {
                diagnostics.Add(Diagnostic($"Target '{target}' is a {node.GetType().Name} and cannot be exposed."));
            }
        }

        if (!string.IsNullOrWhiteSpace(FilterExpression))
        {
            var parseResult = FilterExpressionParser.Parse(FilterExpression);
            if (parseResult.Expression is null)
            {
                diagnostics.AddRange(parseResult.Diagnostics);
            }
        }

        return diagnostics;
    }

    /// <summary>
    ///     Builds a locally-originated validation diagnostic.
    /// </summary>
    /// <param name="message">Diagnostic message text.</param>
    /// <returns>A diagnostic attributed to the custom-view definition rather than a workspace file.</returns>
    private static SysmlDiagnostic Diagnostic(string message)
    {
        return new SysmlDiagnostic(DiagnosticSourceName, 0, 0, DiagnosticSeverity.Error, message);
    }
}
