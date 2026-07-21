using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DemaConsulting.SysML2Tools.Query;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     View model for the modal Query dialog: a single always-visible form over the current workspace's
///     declarations, driven by one <see cref="Picker" /> and a "Query Type" selector covering all eleven
///     user-facing options (a merged <see cref="QueryVerb.List" /> entry plus the ten element-scoped
///     <see cref="QueryVerb" /> operations dispatched through <see cref="QueryEngine.Execute" />). Every
///     relevant change - Query Type, chip/search edits, element selection, Hierarchy direction, Impact
///     walk depth, or the Include-standard-library toggle - immediately recomputes
///     <see cref="CurrentResult" />; there is no explicit "Run" gesture anywhere in this design.
/// </summary>
/// <remarks>
///     Deliberately parallels <see cref="ViewBuilderDialogViewModel" />'s "dialog owned by the shell,
///     fresh instance per open" lifetime pattern: no subscription to <see cref="MainWindowShell" />
///     workspace events (the dialog is short-lived), a single <see cref="RefreshFromWorkspace" /> call at
///     construction, and the composed <see cref="ElementPickerViewModel" /> is wholly owned by this view
///     model. Every user-visible failure surface (no workspace loaded, no element selected, engine
///     rejects the verb for the resolved node) is reported through the observable
///     <see cref="StatusMessage" /> string rather than by throwing, matching the plan's
///     "graceful no-selection/no-workspace handling" acceptance criterion.
/// </remarks>
public sealed partial class QueryDialogViewModel : ObservableObject
{
    /// <summary>
    ///     The eleven Query Type choices the single form's combo box exposes, in the order the redesign's
    ///     UI mock lists them: the merged <see cref="QueryVerb.List" /> entry first (the user never sees
    ///     <see cref="QueryVerb.Find" />; selecting "List" always recomputes the client-side filter, never
    ///     calls <see cref="QueryEngine.List" />/<see cref="QueryEngine.Find" />), followed by the ten
    ///     element-scoped verbs that require <see cref="ElementPickerViewModel.SelectedQualifiedName" /> to
    ///     be set on <see cref="Picker" /> before <see cref="QueryEngine.Execute" /> is called.
    /// </summary>
    public static readonly IReadOnlyList<QueryVerb> QueryTypes =
    [
        QueryVerb.List,
        QueryVerb.Describe,
        QueryVerb.Uses,
        QueryVerb.UsedBy,
        QueryVerb.Dependencies,
        QueryVerb.Impact,
        QueryVerb.Hierarchy,
        QueryVerb.Requirements,
        QueryVerb.Interface,
        QueryVerb.Connections,
        QueryVerb.States,
    ];

    /// <summary>
    ///     Traversal-direction choices accepted by the <see cref="QueryVerb.Hierarchy" /> verb's
    ///     <see cref="QueryOptions.Direction" /> field. Kept as raw strings (rather than an enum) because
    ///     <see cref="QueryOptions.Direction" /> is itself typed <see langword="string" />? in the
    ///     underlying package: no adapter layer needed for the two-way binding.
    /// </summary>
    public static readonly IReadOnlyList<string> HierarchyDirectionOptions = ["up", "down", "both"];

    /// <summary>
    ///     Master, unfiltered candidate map (qualified name → underlying <see cref="SysmlNode" />) built
    ///     from <see cref="MainWindowShell.CurrentWorkspace" /> by <see cref="RefreshFromWorkspace" />.
    ///     Kept alongside <see cref="Picker" /> (which exposes only qualified-name strings) so
    ///     <see cref="RecomputeResult" /> can resolve the picker's selected qualified name back to a
    ///     <see cref="SysmlNode" /> for <see cref="QueryEngine.Execute" /> without re-querying the
    ///     workspace, and so <see cref="BuildListResult" /> can attach the same
    ///     <see cref="ElementTypeLabeler" /> kind label to each List-type entry.
    /// </summary>
    private IReadOnlyDictionary<string, SysmlNode> _candidateMap =
        new Dictionary<string, SysmlNode>(StringComparer.Ordinal);

    [ObservableProperty]
    private bool _includeStdlib;

    [ObservableProperty]
    private bool _isWorkspaceEmpty;

    [ObservableProperty]
    private QueryVerb _selectedQueryType = QueryVerb.List;

    [ObservableProperty]
    private string? _hierarchyDirection = "both";

    [ObservableProperty]
    private string? _walkDepthText;

    [ObservableProperty]
    private QueryResult? _currentResult;

    [ObservableProperty]
    private IReadOnlyList<QueryResultRow> _currentResultRows = [];

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    ///     Creates the dialog view model over <paramref name="shell" /> and immediately populates the
    ///     single picker from the current workspace. The observable <see cref="IncludeStdlib" /> flag
    ///     starts <see langword="false" /> (matching the CLI's own <c>--include-stdlib</c> default) so
    ///     stdlib names are excluded until the user toggles the checkbox.
    /// </summary>
    /// <param name="shell">Fully composed application shell providing the current workspace.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shell" /> is <see langword="null" />.</exception>
    public QueryDialogViewModel(MainWindowShell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);

        Shell = shell;
        Picker = new ElementPickerViewModel();

        // Any change to the picker's displayed items or selected qualified name must immediately
        // recompute the shared results panel: List-type results regenerate off DisplayedItems, and every
        // other Query Type's result regenerates off SelectedQualifiedName. This single subscription is
        // the redesign's entire "no explicit Run gesture" mechanism.
        Picker.PropertyChanged += OnPickerPropertyChanged;

        RefreshFromWorkspace();
    }

    /// <summary>
    ///     Fully composed application shell providing the current workspace and its declarations. Held
    ///     as a public property so the code-behind (which owns the two-way search-textbox focus and the
    ///     context-menu copy handlers) and the design-time constructor factory can both reach it.
    /// </summary>
    public MainWindowShell Shell { get; }

    /// <summary>
    ///     The single picker backing the whole form: full workspace declarations, no exclusions (beyond
    ///     the stdlib exclusion controlled by <see cref="IncludeStdlib" />), no default type filter chip.
    ///     For <see cref="QueryVerb.List" /> its <see cref="ElementPickerViewModel.DisplayedItems" /> is
    ///     the result source; for every other Query Type its
    ///     <see cref="ElementPickerViewModel.SelectedQualifiedName" /> is the target element and
    ///     <see cref="ElementPickerViewModel.DisplayedItems" /> is a pure filter aid whose selection is
    ///     otherwise unused.
    /// </summary>
    public ElementPickerViewModel Picker { get; }

    /// <summary>
    ///     Clipboard write seam used by <see cref="CopyResultAsMarkdownAsync" /> and
    ///     <see cref="CopyResultAsJsonAsync" />. Left unset (<see langword="null" />) until the owning
    ///     <see cref="QueryDialogView" /> attaches to the visual tree and assigns a real
    ///     <see cref="AvaloniaClipboardService" />; unit tests instead assign a fake test double directly
    ///     so the recompute-and-copy orchestration can be verified without any live UI/OS clipboard.
    /// </summary>
    public IClipboardService? ClipboardService { get; set; }

    /// <summary>
    ///     Computed mirror of "is there a result to copy right now", exposed so the results panel's
    ///     right-click context-menu items can bind their <c>IsEnabled</c> directly to a VM property
    ///     (mirroring <see cref="DiagramDocumentViewModel.CanCopyAsSysml" />'s proven pattern) rather than
    ///     the code-behind imperatively toggling button enablement, as the old toolbar buttons did.
    /// </summary>
    public bool HasCurrentResult => CurrentResult is not null;

    /// <summary>
    ///     Refreshes the picker's candidate list from the current workspace, applying the
    ///     <see cref="IncludeStdlib" /> filter, then explicitly recomputes the results panel so toggling
    ///     the checkbox is reflected immediately even though <see cref="ElementPickerViewModel.SetCandidates" />
    ///     already fires its own <c>PropertyChanged</c> notification (this
    ///     trailing call is harmless, idempotent defense-in-depth rather than a required step). Called once
    ///     at construction and again whenever <see cref="IncludeStdlib" /> toggles; the dialog does not
    ///     observe post-open workspace changes.
    /// </summary>
    public void RefreshFromWorkspace()
    {
        _candidateMap = Shell.CurrentWorkspace.Sources.Count == 0
            ? new Dictionary<string, SysmlNode>(StringComparer.Ordinal)
            : Shell.CurrentWorkspace.Workspace.Declarations
                .Where(kvp => IncludeStdlib || !Shell.CurrentWorkspace.Workspace.StdlibNames.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

        IsWorkspaceEmpty = Shell.CurrentWorkspace.Sources.Count == 0;

        var candidates = _candidateMap
            .Select(kvp => (QualifiedName: kvp.Key, TypeLabel: ElementTypeLabeler.GetTypeLabel(kvp.Value)))
            .OrderBy(entry => entry.QualifiedName, StringComparer.Ordinal)
            .ToList();

        // No default type filter chip: the plan explicitly calls out that the single form starts with
        // "no default filter" so every candidate is shown until the user narrows with a chip or search
        // text (unlike the ViewBuilder expose-targets picker, which defaults to "part").
        Picker.SetCandidates(candidates);

        RecomputeResult();
    }

    /// <summary>
    ///     Recomputes <see cref="CurrentResult" /> for the currently selected <see cref="SelectedQueryType" />.
    ///     For <see cref="QueryVerb.List" /> this builds a purely client-side filtered list from
    ///     <see cref="Picker" />'s <see cref="ElementPickerViewModel.DisplayedItems" /> via
    ///     <see cref="BuildListResult" />. For every other Query Type this requires
    ///     <see cref="ElementPickerViewModel.SelectedQualifiedName" />: with no selection it reports a
    ///     helpful (non-error) <see cref="StatusMessage" /> and clears the results table rather than
    ///     leaving a stale prior result, and with a selection it dispatches through
    ///     <see cref="QueryEngine.Execute" /> exactly as the design's original "Run" gesture did,
    ///     gracefully catching <see cref="ArgumentException" />. Called automatically by every relevant
    ///     property change - there is no explicit "Run" method in this design.
    /// </summary>
    public void RecomputeResult()
    {
        if (SelectedQueryType == QueryVerb.List)
        {
            BuildListResult();
            return;
        }

        if (IsWorkspaceEmpty)
        {
            StatusMessage = "No workspace is open. Add a file or folder from the Workspace panel first.";
            CurrentResult = null;
            CurrentResultRows = [];
            return;
        }

        var qualifiedName = Picker.SelectedQualifiedName;
        if (string.IsNullOrEmpty(qualifiedName))
        {
            // Not an error: the user simply hasn't picked a target element yet for this element-scoped
            // Query Type. Clear any stale prior result (e.g. from a previously selected Query Type)
            // rather than leaving it on screen.
            StatusMessage = $"Select an element above to see results for '{SelectedQueryType}'.";
            CurrentResult = null;
            CurrentResultRows = [];
            return;
        }

        if (!_candidateMap.TryGetValue(qualifiedName, out var node))
        {
            // A qualified name from the picker that isn't in the candidate map means the workspace
            // changed under us between the last refresh and this recompute; treat as a user-visible
            // error rather than a crash, and stop before touching QueryEngine.
            StatusMessage = $"Element '{qualifiedName}' is no longer in the workspace. Reopen the dialog.";
            CurrentResult = null;
            CurrentResultRows = [];
            return;
        }

        var options = BuildOptions(qualifiedName);

        try
        {
            var result = QueryEngine.Execute(Shell.CurrentWorkspace.Workspace, options, node);
            CurrentResult = result;
            CurrentResultRows = result.Entries.Select(BuildRow).ToList();
            StatusMessage = null;
        }
        catch (ArgumentException ex)
        {
            // QueryEngine throws ArgumentNullException for a null element on a verb that requires one,
            // and ArgumentOutOfRangeException for an unknown verb - both derive from ArgumentException.
            // Convert to a user-visible message rather than a crash, matching the plan's "graceful
            // handling" acceptance criterion.
            StatusMessage = $"Could not run query: {ex.Message}";
            CurrentResult = null;
            CurrentResultRows = [];
        }
    }

    /// <summary>
    ///     Rebuilds the shared results panel from the picker's current displayed items. Runs
    ///     automatically whenever <see cref="SelectedQueryType" /> is <see cref="QueryVerb.List" /> and
    ///     <see cref="Picker" /> emits a <see cref="ElementPickerViewModel.DisplayedItems" /> change (a
    ///     chip toggle or search-text edit); may also be invoked directly by tests to assert the "List"
    ///     Query Type's client-side "list" semantics without spinning up an Avalonia view.
    /// </summary>
    public void BuildListResult()
    {
        var displayed = Picker.DisplayedItems;

        var entries = displayed
            .Select(qualifiedName => new QueryResultEntry
            {
                QualifiedName = qualifiedName,
                Kind = _candidateMap.TryGetValue(qualifiedName, out var node)
                    ? ElementTypeLabeler.GetTypeLabel(node)
                    : null,
            })
            .ToList();

        // "List" is deliberately a purely client-side filter: it does NOT call QueryEngine.List/Find
        // (the plan's explicit deviation). Rendered with Verb="list" so the shared markdown/json
        // renderer produces a consistent "query list" heading, but Element is null (there is no target
        // element, and the workspace-wide list/find verbs already allow null).
        CurrentResult = new QueryResult
        {
            Verb = "list",
            Element = null,
            Summary = [$"{displayed.Count} element(s) match the filter."],
            Entries = entries,
        };

        CurrentResultRows = entries.Select(BuildRow).ToList();
        StatusMessage = null;
    }

    /// <summary>
    ///     Copies <see cref="CurrentResult" /> to the clipboard as the Markdown rendering produced by
    ///     <see cref="QueryResultRenderer.RenderMarkdown" />, joined with newline separators. A no-op
    ///     (rather than an exception) when either <see cref="CurrentResult" /> or
    ///     <see cref="ClipboardService" /> is <see langword="null" />, so the "Copy as Markdown" context
    ///     menu item can be safely wired unconditionally in the view.
    /// </summary>
    public async Task CopyResultAsMarkdownAsync()
    {
        if (CurrentResult is null || ClipboardService is null)
        {
            return;
        }

        var lines = QueryResultRenderer.RenderMarkdown(CurrentResult);
        await ClipboardService.SetTextAsync(string.Join("\n", lines));
    }

    /// <summary>
    ///     Copies <see cref="CurrentResult" /> to the clipboard as the JSON rendering produced by
    ///     <see cref="QueryResultRenderer.RenderJson" />. A no-op (rather than an exception) when either
    ///     <see cref="CurrentResult" /> or <see cref="ClipboardService" /> is <see langword="null" />.
    /// </summary>
    public async Task CopyResultAsJsonAsync()
    {
        if (CurrentResult is null || ClipboardService is null)
        {
            return;
        }

        var text = QueryResultRenderer.RenderJson(CurrentResult);
        await ClipboardService.SetTextAsync(text);
    }

    /// <summary>
    ///     Builds a <see cref="QueryOptions" /> instance for <paramref name="qualifiedName" /> that
    ///     reflects the current form's verb-specific state: <see cref="HierarchyDirection" /> is only
    ///     attached for <see cref="QueryVerb.Hierarchy" />, and <see cref="WalkDepthText" /> is only
    ///     parsed for <see cref="QueryVerb.Impact" />. Every option unconditionally carries
    ///     <see cref="IncludeStdlib" />.
    /// </summary>
    /// <param name="qualifiedName">The resolved target element's qualified name.</param>
    /// <returns>The <see cref="QueryOptions" /> to pass to <see cref="QueryEngine.Execute" />.</returns>
    public QueryOptions BuildOptions(string qualifiedName)
    {
        int? walkDepth = null;
        if (SelectedQueryType == QueryVerb.Impact
            && !string.IsNullOrWhiteSpace(WalkDepthText)
            && int.TryParse(WalkDepthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var depth)
            && depth >= 0)
        {
            walkDepth = depth;
        }

        return new QueryOptions
        {
            Verb = SelectedQueryType,
            Element = qualifiedName,
            IncludeStdlib = IncludeStdlib,
            Direction = SelectedQueryType == QueryVerb.Hierarchy ? HierarchyDirection : null,
            WalkDepth = walkDepth,
        };
    }

    /// <summary>
    ///     Refreshes the picker whenever <see cref="IncludeStdlib" /> toggles, matching the plan's
    ///     "toggling recomputes both the candidate set and the current result" contract.
    /// </summary>
    /// <param name="value">The new value of the <see cref="IncludeStdlib" /> property.</param>
    partial void OnIncludeStdlibChanged(bool value)
    {
        RefreshFromWorkspace();
    }

    /// <summary>
    ///     Recomputes immediately whenever the user changes the Query Type - the redesign's central
    ///     "no explicit Run gesture" contract for the verb selector itself.
    /// </summary>
    /// <param name="value">The newly selected Query Type.</param>
    partial void OnSelectedQueryTypeChanged(QueryVerb value)
    {
        RecomputeResult();
    }

    /// <summary>
    ///     Recomputes immediately whenever the Hierarchy direction changes, so a selection already made
    ///     for <see cref="QueryVerb.Hierarchy" /> stays live as the user flips between up/down/both.
    /// </summary>
    /// <param name="value">The newly selected hierarchy direction.</param>
    partial void OnHierarchyDirectionChanged(string? value)
    {
        RecomputeResult();
    }

    /// <summary>
    ///     Recomputes immediately whenever the Impact walk-depth text changes, so a selection already
    ///     made for <see cref="QueryVerb.Impact" /> stays live as the user edits the depth limit.
    /// </summary>
    /// <param name="value">The newly edited walk-depth text.</param>
    partial void OnWalkDepthTextChanged(string? value)
    {
        RecomputeResult();
    }

    /// <summary>
    ///     Raises the computed <see cref="HasCurrentResult" /> property's change notification whenever
    ///     <see cref="CurrentResult" /> changes, so the results panel's context-menu <c>IsEnabled</c>
    ///     bindings stay in sync without a backing <c>[ObservableProperty]</c> field.
    /// </summary>
    /// <param name="value">The newly computed current result.</param>
    partial void OnCurrentResultChanged(QueryResult? value)
    {
        OnPropertyChanged(nameof(HasCurrentResult));
    }

    /// <summary>
    ///     Observes <see cref="Picker" />'s <see cref="ElementPickerViewModel.DisplayedItems" /> and
    ///     <see cref="ElementPickerViewModel.SelectedQualifiedName" /> changes and regenerates the results
    ///     panel so it stays in sync as the user types in the search box, toggles a chip, or selects an
    ///     element - the single form's "live" contract, replacing the old two-tab design's single-purpose
    ///     Browse-only handler.
    /// </summary>
    private void OnPickerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ElementPickerViewModel.DisplayedItems)
            or nameof(ElementPickerViewModel.SelectedQualifiedName))
        {
            RecomputeResult();
        }
    }

    /// <summary>
    ///     Converts a <see cref="QueryResultEntry" /> into a display-friendly row for the results panel,
    ///     eagerly flattening <see cref="QueryResultEntry.Direction" /> to a human-readable string and
    ///     <see cref="QueryResultEntry.Notes" /> to a newline-joined tooltip string so the view can bind
    ///     directly to string properties without additional converters.
    /// </summary>
    /// <param name="entry">The engine-produced entry to project.</param>
    /// <returns>A <see cref="QueryResultRow" /> ready for the results-panel <c>ItemsControl</c>.</returns>
    private static QueryResultRow BuildRow(QueryResultEntry entry)
    {
        return new QueryResultRow(
            entry.QualifiedName,
            entry.Kind ?? string.Empty,
            entry.Detail ?? string.Empty,
            entry.Direction switch
            {
                QueryEntryDirection.Outgoing => "outgoing",
                QueryEntryDirection.Incoming => "incoming",
                _ => string.Empty,
            },
            entry.Notes.Count > 0 ? string.Join("\n", entry.Notes) : null);
    }
}

/// <summary>
///     Flattened, view-friendly projection of a <see cref="QueryResultEntry" /> for the Query dialog's
///     shared results-panel <c>ItemsControl</c>. All fields are non-null strings (or, for
///     <see cref="Notes" />, an explicitly <see langword="null" /> tooltip) so the AXAML can bind
///     directly without value converters and without null-visibility juggling.
/// </summary>
/// <param name="QualifiedName">The entry's fully-qualified name; the row's primary column.</param>
/// <param name="Kind">The entry's short kind/relationship label (empty when the engine returned <see langword="null" />).</param>
/// <param name="Detail">The entry's short free-form detail (empty when the engine returned <see langword="null" />).</param>
/// <param name="Direction">
///     A human-readable dependency-verb direction (<c>"outgoing"</c>/<c>"incoming"</c>), or empty for every
///     other verb. Rendered in a dedicated column shown only when the current verb is
///     <c>dependencies</c>.
/// </param>
/// <param name="Notes">
///     Newline-joined additional notes attached to the entry, or <see langword="null" /> when the entry
///     has no notes. Bound to the row's tooltip.
/// </param>
public sealed record QueryResultRow(
    string QualifiedName,
    string Kind,
    string Detail,
    string Direction,
    string? Notes);
