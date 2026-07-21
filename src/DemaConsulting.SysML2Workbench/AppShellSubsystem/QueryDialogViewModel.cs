using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using DemaConsulting.SysML2Tools.Query;
using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     View model for the modal Query dialog: a two-tab picker (Browse / Element Query) over the
///     current workspace's declarations that lets the user either see a filtered client-side list of
///     elements or run one of the ten element-scoped <see cref="QueryVerb" /> operations through
///     <see cref="QueryEngine.Execute" /> and render the result via <see cref="QueryResultRenderer" />.
/// </summary>
/// <remarks>
///     Deliberately parallels <see cref="ViewBuilderDialogViewModel" />'s "dialog owned by the shell,
///     fresh instance per open" lifetime pattern: no subscription to <see cref="MainWindowShell" />
///     workspace events (the dialog is short-lived), a single <see cref="RefreshFromWorkspace" /> call at
///     construction, and the two composed <see cref="ElementPickerViewModel" />s are wholly owned by this
///     view model. Every user-visible failure surface (no workspace loaded, no element selected, engine
///     rejects the verb for the resolved node) is reported through the observable
///     <see cref="StatusMessage" /> string rather than by throwing, matching the plan's
///     "graceful no-selection/no-workspace handling" acceptance criterion.
/// </remarks>
public sealed partial class QueryDialogViewModel : ObservableObject
{
    /// <summary>
    ///     The ten element-scoped verbs the Element Query tab exposes, in the order the plan's UI mock
    ///     lists them. Deliberately excludes <see cref="QueryVerb.List" /> and <see cref="QueryVerb.Find" />
    ///     (whose "workspace-wide, no target element" semantics the Browse tab already covers with a
    ///     purely client-side filter), so the verb selector never presents an option that
    ///     <see cref="QueryEngine.Execute" /> would need a <see langword="null" /> element for.
    /// </summary>
    public static readonly IReadOnlyList<QueryVerb> ElementScopedVerbs =
    [
        QueryVerb.Uses,
        QueryVerb.UsedBy,
        QueryVerb.Dependencies,
        QueryVerb.Impact,
        QueryVerb.Describe,
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
    ///     Kept alongside the two <see cref="ElementPickerViewModel" />s (which expose only qualified-name
    ///     strings) so <see cref="RunElementQuery" /> can resolve the picker's selected qualified name
    ///     back to a <see cref="SysmlNode" /> for <see cref="QueryEngine.Execute" /> without re-querying
    ///     the workspace, and so <see cref="BuildBrowseResult" /> can attach the same
    ///     <see cref="ElementTypeLabeler" /> kind label to each Browse-tab entry.
    /// </summary>
    private IReadOnlyDictionary<string, SysmlNode> _candidateMap =
        new Dictionary<string, SysmlNode>(StringComparer.Ordinal);

    [ObservableProperty]
    private bool _includeStdlib;

    [ObservableProperty]
    private bool _isWorkspaceEmpty;

    [ObservableProperty]
    private QueryVerb _selectedVerb = QueryVerb.Describe;

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
    ///     Creates the dialog view model over <paramref name="shell" /> and immediately populates both
    ///     tabs' pickers from the current workspace. The observable <see cref="IncludeStdlib" /> flag
    ///     starts <see langword="false" /> (matching the CLI's own <c>--include-stdlib</c> default) so
    ///     stdlib names are excluded until the user toggles the checkbox.
    /// </summary>
    /// <param name="shell">Fully composed application shell providing the current workspace.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shell" /> is <see langword="null" />.</exception>
    public QueryDialogViewModel(MainWindowShell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);

        Shell = shell;
        BrowsePicker = new ElementPickerViewModel();
        ElementQueryPicker = new ElementPickerViewModel();

        // Any change to Browse-tab's displayed items must regenerate the shared results panel so the
        // Browse tab remains "live" (typing in its search box updates the results panel with no
        // Run-button gesture) - the plan's contract for that tab.
        BrowsePicker.PropertyChanged += OnBrowsePickerPropertyChanged;

        RefreshFromWorkspace();
    }

    /// <summary>
    ///     Fully composed application shell providing the current workspace and its declarations. Held
    ///     as a public property so the code-behind (which owns the two-way search-textbox focus and the
    ///     copy-buttons' click handlers) and the design-time constructor factory can both reach it.
    /// </summary>
    public MainWindowShell Shell { get; }

    /// <summary>
    ///     Picker backing the "Browse" tab: full workspace declarations, no exclusions (beyond the
    ///     stdlib exclusion controlled by <see cref="IncludeStdlib" />), no default type filter chip.
    /// </summary>
    public ElementPickerViewModel BrowsePicker { get; }

    /// <summary>
    ///     Picker backing the "Element Query" tab: same candidate set as <see cref="BrowsePicker" />
    ///     (they share <see cref="RefreshFromWorkspace" />), no default type filter chip. Selection
    ///     drives <see cref="RunElementQuery" />.
    /// </summary>
    public ElementPickerViewModel ElementQueryPicker { get; }

    /// <summary>
    ///     Clipboard write seam used by <see cref="CopyResultAsMarkdownAsync" /> and
    ///     <see cref="CopyResultAsJsonAsync" />. Left unset (<see langword="null" />) until the owning
    ///     <see cref="QueryDialogView" /> attaches to the visual tree and assigns a real
    ///     <see cref="AvaloniaClipboardService" />; unit tests instead assign a fake test double directly
    ///     so the run-and-copy orchestration can be verified without any live UI/OS clipboard.
    /// </summary>
    public IClipboardService? ClipboardService { get; set; }

    /// <summary>
    ///     Refreshes both pickers' candidate lists from the current workspace, applying the
    ///     <see cref="IncludeStdlib" /> filter. Called once at construction and again whenever
    ///     <see cref="IncludeStdlib" /> toggles; the dialog does not observe post-open workspace changes.
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

        // Neither tab has a default type filter chip: the plan explicitly calls out that both start
        // "no default filter" so every candidate is shown until the user narrows with a chip or search
        // text (unlike the ViewBuilder expose-targets picker, which defaults to "part").
        BrowsePicker.SetCandidates(candidates);
        ElementQueryPicker.SetCandidates(candidates);
    }

    /// <summary>
    ///     Rebuilds the shared results panel from the Browse tab's current displayed items. Runs
    ///     automatically whenever <see cref="BrowsePicker" /> emits a <see cref="ElementPickerViewModel.DisplayedItems" />
    ///     change (a chip toggle or search-text edit); may also be invoked directly by tests to assert
    ///     the tab's client-side "list" semantics without spinning up an Avalonia view.
    /// </summary>
    public void BuildBrowseResult()
    {
        var displayed = BrowsePicker.DisplayedItems;

        var entries = displayed
            .Select(qualifiedName => new QueryResultEntry
            {
                QualifiedName = qualifiedName,
                Kind = _candidateMap.TryGetValue(qualifiedName, out var node)
                    ? ElementTypeLabeler.GetTypeLabel(node)
                    : null,
            })
            .ToList();

        // The Browse tab is deliberately a purely client-side filter: it does NOT call
        // QueryEngine.List/Find (the plan's explicit deviation). Rendered with Verb="list" so the
        // shared markdown/json renderer produces a consistent "query list" heading, but Element is null
        // (there is no target element, and the workspace-wide list/find verbs already allow null).
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
    ///     Runs the Element Query tab's currently-configured verb against
    ///     <see cref="ElementQueryPicker" />'s selected qualified name. Reports every recoverable failure
    ///     (no selection, empty workspace, unknown qualified name, engine argument rejection) through
    ///     <see cref="StatusMessage" /> and leaves <see cref="CurrentResult" /> unchanged; never throws.
    /// </summary>
    public void RunElementQuery()
    {
        if (IsWorkspaceEmpty)
        {
            StatusMessage = "No workspace is open. Add a file or folder from the Workspace panel first.";
            return;
        }

        var qualifiedName = ElementQueryPicker.SelectedQualifiedName;
        if (string.IsNullOrEmpty(qualifiedName))
        {
            StatusMessage = "Select an element from the list before running a query.";
            return;
        }

        if (!_candidateMap.TryGetValue(qualifiedName, out var node))
        {
            // A qualified name from the picker that isn't in the candidate map means the workspace
            // changed under us between the last refresh and this run; treat as a user-visible error
            // rather than a crash, and stop before touching QueryEngine.
            StatusMessage = $"Element '{qualifiedName}' is no longer in the workspace. Reopen the dialog.";
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
        }
    }

    /// <summary>
    ///     Copies <see cref="CurrentResult" /> to the clipboard as the Markdown rendering produced by
    ///     <see cref="QueryResultRenderer.RenderMarkdown" />, joined with newline separators. A no-op
    ///     (rather than an exception) when either <see cref="CurrentResult" /> or
    ///     <see cref="ClipboardService" /> is <see langword="null" />, so the "Copy as Markdown" button
    ///     can be safely wired unconditionally in the view.
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
    ///     reflects the current tab's verb-specific state: <see cref="HierarchyDirection" /> is only
    ///     attached for <see cref="QueryVerb.Hierarchy" />, and <see cref="WalkDepthText" /> is only
    ///     parsed for <see cref="QueryVerb.Impact" />. Every option unconditionally carries
    ///     <see cref="IncludeStdlib" />.
    /// </summary>
    /// <param name="qualifiedName">The resolved target element's qualified name.</param>
    /// <returns>The <see cref="QueryOptions" /> to pass to <see cref="QueryEngine.Execute" />.</returns>
    public QueryOptions BuildOptions(string qualifiedName)
    {
        int? walkDepth = null;
        if (SelectedVerb == QueryVerb.Impact
            && !string.IsNullOrWhiteSpace(WalkDepthText)
            && int.TryParse(WalkDepthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var depth)
            && depth >= 0)
        {
            walkDepth = depth;
        }

        return new QueryOptions
        {
            Verb = SelectedVerb,
            Element = qualifiedName,
            IncludeStdlib = IncludeStdlib,
            Direction = SelectedVerb == QueryVerb.Hierarchy ? HierarchyDirection : null,
            WalkDepth = walkDepth,
        };
    }

    /// <summary>
    ///     Refreshes both pickers whenever <see cref="IncludeStdlib" /> toggles, matching the plan's
    ///     "toggling recomputes both tabs' candidates via <c>SetCandidates</c> again" contract.
    /// </summary>
    /// <param name="value">The new value of the <see cref="IncludeStdlib" /> property.</param>
    partial void OnIncludeStdlibChanged(bool value)
    {
        RefreshFromWorkspace();
    }

    /// <summary>
    ///     Observes <see cref="BrowsePicker" />'s <see cref="ElementPickerViewModel.DisplayedItems" />
    ///     changes and regenerates the Browse-tab result so the shared results panel stays in sync as
    ///     the user types in the search box or toggles a chip - the Browse tab's "live" contract.
    /// </summary>
    private void OnBrowsePickerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ElementPickerViewModel.DisplayedItems))
        {
            BuildBrowseResult();
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
