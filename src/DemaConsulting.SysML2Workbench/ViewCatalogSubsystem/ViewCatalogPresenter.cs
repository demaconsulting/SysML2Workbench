using DemaConsulting.SysML2Tools.Rendering;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;

namespace DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;

/// <summary>
///     A single predefined view discovered in the loaded workspace, ready for display in the view catalog.
/// </summary>
/// <param name="QualifiedName">
///     Fully qualified SysML name of the view, also used as its catalog identifier and as the
///     <c>viewFilter</c> passed to <c>DiagramRenderer.RenderWorkspace</c> (via its simple <see cref="Name" />).
/// </param>
/// <param name="Name">Simple (unqualified) SysML name of the view.</param>
/// <param name="DisplayName">User-facing name for the view.</param>
/// <param name="Kind">
///     Recognized Phase 0 view kind, or <see langword="null" /> when the view's <c>render</c> statement names a
///     rendering style outside Phase 0 scope (for example <c>asTreeDiagram</c>).
/// </param>
public sealed record ViewDescriptor(string QualifiedName, string Name, string DisplayName, ViewKind? Kind);

/// <summary>
///     ViewCatalogPresenter converts the workspace's predefined SysML view usages into a deterministic, UI-ready
///     catalog and tracks which predefined view is currently selected for rendering.
/// </summary>
public sealed class ViewCatalogPresenter
{
    /// <summary>
    ///     Flattened catalog of predefined views visible in the current workspace.
    /// </summary>
    private IReadOnlyList<ViewDescriptor> _availableViews = [];

    /// <summary>
    ///     Flattened catalog of predefined views visible in the current workspace.
    /// </summary>
    public IReadOnlyList<ViewDescriptor> AvailableViews => _availableViews;

    /// <summary>
    ///     Identifier of the currently selected predefined view, or <see langword="null" /> when nothing is
    ///     selected.
    /// </summary>
    public string? SelectedViewId { get; private set; }

    /// <summary>
    ///     Stable ordering for the six supported predefined view kinds.
    /// </summary>
    public IReadOnlyList<ViewKind> KindsInDisplayOrder { get; } =
    [
        ViewKind.General,
        ViewKind.Interconnection,
        ViewKind.StateTransition,
        ViewKind.ActionFlow,
        ViewKind.Sequence,
        ViewKind.Grid,
    ];

    /// <summary>
    ///     Opaque token used to detect when the underlying workspace snapshot has changed and the catalog must be
    ///     rebuilt.
    /// </summary>
    public string WorkspaceRevision { get; private set; } = string.Empty;

    /// <summary>
    ///     Rebuilds the catalog from the latest workspace snapshot.
    /// </summary>
    /// <param name="workspace">Loaded model content.</param>
    /// <param name="workspaceRevision">Opaque revision token identifying the snapshot being catalogued.</param>
    /// <returns>Updated view descriptors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workspace" /> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the workspace declares two views with the same qualified name.
    /// </exception>
    public IReadOnlyList<ViewDescriptor> RefreshCatalog(SysmlWorkspace workspace, string workspaceRevision)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var identities = DiagramRenderer.GetViewIdentities(workspace);
        var seenQualifiedNames = new HashSet<string>(StringComparer.Ordinal);
        var descriptors = new List<ViewDescriptor>();
        foreach (var identity in identities)
        {
            if (!seenQualifiedNames.Add(identity.QualifiedName))
            {
                throw new InvalidOperationException(
                    $"Duplicate predefined view identifier '{identity.QualifiedName}' was discovered in the workspace.");
            }

            // Look up the underlying view node so its render token can be mapped to a supported ViewKind;
            // DiagramRenderer.ViewIdentity itself does not carry the kind
            ViewKind? kind = null;
            if (workspace.Declarations.TryGetValue(identity.QualifiedName, out var node) && node is SysmlViewNode viewNode)
            {
                kind = ViewKindExtensions.FromRenderTargetName(viewNode.RenderTargetName);
            }

            descriptors.Add(new ViewDescriptor(identity.QualifiedName, node?.Name ?? identity.QualifiedName, identity.DisplayName, kind));
        }

        _availableViews = descriptors;
        WorkspaceRevision = workspaceRevision;

        // A selection that no longer resolves in the rebuilt catalog is cleared so the UI prompts for a new choice
        if (SelectedViewId is not null && descriptors.All(d => d.QualifiedName != SelectedViewId))
        {
            SelectedViewId = null;
        }

        return _availableViews;
    }

    /// <summary>
    ///     Marks one predefined view as active.
    /// </summary>
    /// <param name="viewId">Identifier of the chosen descriptor.</param>
    /// <returns>Descriptor to forward to rendering.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="viewId" /> is null, whitespace, or does not exist in
    ///     <see cref="AvailableViews" />.
    /// </exception>
    public ViewDescriptor SelectView(string viewId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewId);

        var descriptor = _availableViews.FirstOrDefault(d => d.QualifiedName == viewId);
        if (descriptor is null)
        {
            throw new ArgumentException($"View '{viewId}' was not found in the current catalog.", nameof(viewId));
        }

        SelectedViewId = descriptor.QualifiedName;
        return descriptor;
    }

    /// <summary>
    ///     Returns the active predefined view descriptor.
    /// </summary>
    /// <returns>Active descriptor, or <see langword="null" /> when nothing is selected.</returns>
    public ViewDescriptor? GetSelectedView()
    {
        return SelectedViewId is null ? null : _availableViews.FirstOrDefault(d => d.QualifiedName == SelectedViewId);
    }

    /// <summary>
    ///     Derives a <see cref="ViewDefinitionModel" /> that faithfully reconstructs a predefined view's real
    ///     <c>view</c> declaration - view kind, every <c>expose</c> member (with its own recursion kind and
    ///     optional bracket-filter expression), filter expression, and display name - from the loaded workspace.
    /// </summary>
    /// <remarks>
    ///     This is the shared derivation path used both by the view catalog (should it ever need a definition
    ///     rather than just a <see cref="ViewDescriptor" />) and by "Copy as SysML" for a predefined-view diagram
    ///     tab, so a single unit owns "convert a workspace's predefined view usage into UI-ready data" rather than
    ///     splitting that responsibility across the catalog and the shell.
    /// </remarks>
    /// <param name="workspace">Loaded model content.</param>
    /// <param name="viewId">Qualified name of the predefined view, as published in <see cref="AvailableViews" />.</param>
    /// <returns>
    ///     A populated <see cref="ViewDefinitionModel" />, or <see langword="null" /> when <paramref name="viewId" />
    ///     does not resolve to a <see cref="SysmlViewNode" /> in <paramref name="workspace" />, its render target
    ///     does not map to a supported <see cref="ViewKind" />, or it has zero <c>ExposeMembers</c> (the "expose
    ///     everything, no scoping" case - valid SysML v2, but with no finite expose list for
    ///     <c>SysmlSnippetGenerator</c> to serialize).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workspace" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="viewId" /> is null or whitespace.</exception>
    public ViewDefinitionModel? BuildViewDefinition(SysmlWorkspace workspace, string viewId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewId);

        var descriptor = _availableViews.FirstOrDefault(d => d.QualifiedName == viewId);
        if (descriptor is null)
        {
            return null;
        }

        if (!workspace.Declarations.TryGetValue(viewId, out var node) || node is not SysmlViewNode viewNode)
        {
            return null;
        }

        var kind = ViewKindExtensions.FromRenderTargetName(viewNode.RenderTargetName);
        if (kind is null)
        {
            return null;
        }

        if (viewNode.ExposeMembers.Count == 0)
        {
            return null;
        }

        var definition = new ViewDefinitionModel();
        definition.SetViewKind(kind.Value);

        foreach (var (member, resolvedQualifiedName) in viewNode.ResolvedExposeMembers)
        {
            definition.AddExposeTarget(new ExposeTargetSelection(resolvedQualifiedName, member.RecursionKind, member.BracketFilterExpressionText));
        }

        definition.SetFilterExpression(viewNode.FilterExpressionText);
        definition.SetDisplayName(descriptor.DisplayName);

        return definition;
    }
}
