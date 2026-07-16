using DemaConsulting.SysML2Tools.Filtering;
using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;

namespace DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;

/// <summary>
///     One selected <c>expose</c> target together with its recursion kind and optional bracket-filter expression.
/// </summary>
/// <param name="QualifiedName">Qualified name of the selected package or element.</param>
/// <param name="RecursionKind">
///     Which of the four SysML v2 <c>expose</c> textual forms this target should be emitted/rendered as. Defaults
///     to <see cref="ExposeRecursionKind.MembershipRecursive" />, matching this unit's prior de-facto behavior
///     before per-target recursion kinds existed.
/// </param>
/// <param name="BracketFilterExpression">
///     Optional bracket-filter expression (the <c>[...]</c> suffix on a recursive <c>expose</c> clause). Only
///     meaningful when <paramref name="RecursionKind" /> is <see cref="ExposeRecursionKind.MembershipRecursive" />
///     or <see cref="ExposeRecursionKind.NamespaceRecursive" />; <see cref="ViewDefinitionModel.ValidateAgainstWorkspace" />
///     reports an error diagnostic if set on either non-recursive kind.
/// </param>
public sealed record ExposeTargetSelection(
    string QualifiedName,
    ExposeRecursionKind RecursionKind = ExposeRecursionKind.MembershipRecursive,
    string? BracketFilterExpression = null);

/// <summary>
///     ViewDefinitionModel captures the complete session-only definition of a custom view so the same normalized
///     state can be used both for live preview rendering and for SysML snippet generation.
/// </summary>
/// <remarks>
///     Deviation from the original design sketch: expose targets were originally modeled as plain qualified-name
///     strings. That rationale is now partly superseded - each expose target is represented by
///     <see cref="ExposeTargetSelection" />, a 3-field record mirroring the real SysML2Tools semantic model's own
///     <see cref="DemaConsulting.SysML2Tools.Semantic.Model.ExposeMember" /> shape (qualified name, recursion
///     kind, optional bracket-filter expression) rather than a bare string. The qualified name itself is still a
///     plain <see cref="string" />, matching
///     <see cref="DemaConsulting.SysML2Tools.Semantic.Model.SysmlNode.QualifiedName" />'s representation, so no
///     dedicated <c>QualifiedName</c> value type was introduced for that inner field.
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
    ///     Ordered, duplicate-free expose targets, keyed by the pair of
    ///     (<see cref="ExposeTargetSelection.QualifiedName" />, <see cref="ExposeTargetSelection.RecursionKind" />).
    ///     This allows the same qualified name to appear more than once as long as each occurrence uses a
    ///     distinct recursion kind (for example both <c>expose PublishingSubsystem;</c> and
    ///     <c>expose PublishingSubsystem::*;</c> for the same package), matching valid SysML v2 semantics.
    /// </summary>
    private List<ExposeTargetSelection> _exposeTargets = [];

    /// <summary>
    ///     Selected custom-view rendering style, constrained to the supported SysML view kinds exposed by the UI.
    /// </summary>
    public ViewKind? ViewKind { get; private set; }

    /// <summary>
    ///     Ordered set of packages or elements selected for <c>expose</c> clauses, each with its own recursion
    ///     kind and optional bracket-filter expression.
    /// </summary>
    public IReadOnlyList<ExposeTargetSelection> ExposeTargets => _exposeTargets;

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
    ///     Adds a qualified name to the <c>expose</c> target set, defaulting to
    ///     <see cref="ExposeRecursionKind.MembershipRecursive" /> (matching this unit's prior de-facto behavior).
    /// </summary>
    /// <remarks>
    ///     If a selection with the same <paramref name="qualifiedName" /> AND
    ///     <see cref="ExposeRecursionKind.MembershipRecursive" /> already exists, this is a no-op: the existing
    ///     selection (including any previously-set bracket filter) is preserved rather than reset, so re-adding
    ///     an already-present target from the UI never silently discards prior configuration. If
    ///     <paramref name="qualifiedName" /> is already selected under a <em>different</em> recursion kind (for
    ///     example <see cref="ExposeRecursionKind.NamespaceDirectChildren" />), a second, independent selection is
    ///     added for the same qualified name — this is valid SysML v2 (for example both
    ///     <c>expose PublishingSubsystem;</c> and <c>expose PublishingSubsystem::*;</c> for the same package).
    /// </remarks>
    /// <param name="qualifiedName">Qualified name of the package or element to expose.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="qualifiedName" /> is null or whitespace.</exception>
    public void AddExposeTarget(string qualifiedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedName);

        if (_exposeTargets.Any(t => t.QualifiedName == qualifiedName && t.RecursionKind == ExposeRecursionKind.MembershipRecursive))
        {
            return;
        }

        _exposeTargets.Add(new ExposeTargetSelection(qualifiedName));
    }

    /// <summary>
    ///     Removes a qualified name/recursion kind pair from the <c>expose</c> target set.
    /// </summary>
    /// <remarks>
    ///     A no-op when no selection matches the given (<paramref name="qualifiedName" />,
    ///     <paramref name="recursionKind" />) pair, so callers do not need to guard the call with a prior
    ///     existence check. Only the matching selection is removed; any other selection sharing the same
    ///     <paramref name="qualifiedName" /> but a different recursion kind is left untouched.
    /// </remarks>
    /// <param name="qualifiedName">Qualified name of the previously-added target to remove.</param>
    /// <param name="recursionKind">Recursion kind of the previously-added target to remove.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="qualifiedName" /> is null or whitespace.</exception>
    public void RemoveExposeTarget(string qualifiedName, ExposeRecursionKind recursionKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedName);

        _exposeTargets.RemoveAll(t => t.QualifiedName == qualifiedName && t.RecursionKind == recursionKind);
    }

    /// <summary>
    ///     Changes the recursion kind of a previously-added expose target.
    /// </summary>
    /// <remarks>
    ///     A no-op when no selection matches the given (<paramref name="qualifiedName" />,
    ///     <paramref name="currentRecursionKind" />) pair. Also a no-op when a <em>different</em> selection
    ///     already exists for (<paramref name="qualifiedName" />, <paramref name="newRecursionKind" />): changing
    ///     the matched selection's kind would otherwise collide with that sibling selection, so the change is
    ///     rejected rather than creating a duplicate pair or silently clobbering the sibling.
    /// </remarks>
    /// <param name="qualifiedName">Qualified name of the previously-added target.</param>
    /// <param name="currentRecursionKind">Current recursion kind of the target to change.</param>
    /// <param name="newRecursionKind">New recursion kind for the target.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="qualifiedName" /> is null or whitespace.</exception>
    public void SetExposeRecursionKind(string qualifiedName, ExposeRecursionKind currentRecursionKind, ExposeRecursionKind newRecursionKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedName);

        var index = _exposeTargets.FindIndex(t => t.QualifiedName == qualifiedName && t.RecursionKind == currentRecursionKind);
        if (index < 0)
        {
            return;
        }

        if (currentRecursionKind != newRecursionKind &&
            _exposeTargets.Any(t => t.QualifiedName == qualifiedName && t.RecursionKind == newRecursionKind))
        {
            return;
        }

        _exposeTargets[index] = _exposeTargets[index] with { RecursionKind = newRecursionKind };
    }

    /// <summary>
    ///     Sets or clears the optional bracket-filter expression of a previously-added expose target.
    /// </summary>
    /// <remarks>
    ///     A no-op when no selection matches the given (<paramref name="qualifiedName" />,
    ///     <paramref name="recursionKind" />) pair. Setting a bracket-filter expression on a target whose
    ///     recursion kind is <see cref="ExposeRecursionKind.MembershipExact" /> or
    ///     <see cref="ExposeRecursionKind.NamespaceDirectChildren" /> is permitted by this mutator (it does not
    ///     validate the combination) but is reported as an error diagnostic by
    ///     <see cref="ValidateAgainstWorkspace" />, since bracket filters are only valid SysML v2 syntax on the
    ///     two recursive expose forms.
    /// </remarks>
    /// <param name="qualifiedName">Qualified name of the previously-added target.</param>
    /// <param name="recursionKind">Recursion kind of the previously-added target.</param>
    /// <param name="expression">Bracket-filter expression text, or <see langword="null" />/whitespace to clear it.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="qualifiedName" /> is null or whitespace.</exception>
    public void SetExposeBracketFilter(string qualifiedName, ExposeRecursionKind recursionKind, string? expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedName);

        var index = _exposeTargets.FindIndex(t => t.QualifiedName == qualifiedName && t.RecursionKind == recursionKind);
        if (index < 0)
        {
            return;
        }

        var normalized = string.IsNullOrWhiteSpace(expression) ? null : expression;
        _exposeTargets[index] = _exposeTargets[index] with { BracketFilterExpression = normalized };
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

        foreach (var selection in _exposeTargets)
        {
            var target = selection.QualifiedName;

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

            if (selection.BracketFilterExpression is not null)
            {
                if (selection.RecursionKind is ExposeRecursionKind.MembershipExact or ExposeRecursionKind.NamespaceDirectChildren)
                {
                    diagnostics.Add(Diagnostic(
                        $"Target '{target}' has a bracket-filter expression but its recursion kind '{selection.RecursionKind}' does not support one; only the two recursive kinds do."));
                }

                var targetParseResult = FilterExpressionParser.Parse(selection.BracketFilterExpression);
                if (targetParseResult.Expression is null)
                {
                    diagnostics.AddRange(targetParseResult.Diagnostics);
                }
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
