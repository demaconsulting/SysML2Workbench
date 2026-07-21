using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

/// <summary>
///     Reusable, dialog-agnostic view model backing the shared "Element Picker" control - a
///     chip-row of type-label filters (OR semantics), a case-insensitive substring search
///     text box, and a single-select list of candidate qualified names.
/// </summary>
/// <remarks>
///     Extracted from the pre-refactor <c>ViewBuilderDialogViewModel</c>'s expose-target
///     picker so more than one dialog (Custom View Builder, Query dialog) can host the same
///     picker without duplicating the OR-then-AND filtering logic and chip management. The
///     view model is deliberately independent of <c>ViewDefinitionModel</c>, <c>MainWindowShell</c>,
///     and any workspace type: the caller is responsible for building the candidate list
///     (typically by mapping <c>SysmlWorkspace.Declarations</c> through
///     <see cref="ElementTypeLabeler.GetTypeLabel" /> and applying any caller-owned
///     exclusions such as stdlib names or unsupported node kinds) and handing it to
///     <see cref="SetCandidates" />. Not thread-safe: all state (properties and
///     <see cref="ActiveTypeFilters" />) must be mutated from a single (typically UI) thread.
/// </remarks>
public sealed partial class ElementPickerViewModel : ObservableObject
{
    /// <summary>
    ///     Master, unfiltered list mapping each candidate element's qualified name to its
    ///     computed type label, in the same sorted order as <see cref="DisplayedItems" />'s
    ///     pre-filter source. Rebuilt by <see cref="SetCandidates" /> and consulted by
    ///     <see cref="RecomputeDisplayedItems" /> so the OR/AND filter pass can re-narrow
    ///     without re-querying the caller for the master set on every keystroke or chip
    ///     change.
    /// </summary>
    private IReadOnlyList<(string QualifiedName, string TypeLabel)> _candidates = [];

    [ObservableProperty]
    private IReadOnlyList<string> _availableTypeLabels = [];

    [ObservableProperty]
    private string? _searchText = "";

    [ObservableProperty]
    private IReadOnlyList<string> _displayedItems = [];

    [ObservableProperty]
    private string? _selectedQualifiedName;

    /// <summary>
    ///     Creates the picker view model in its empty initial state: no candidates, no active
    ///     type filters, empty search text, and an empty displayed list. Callers populate it
    ///     later by calling <see cref="SetCandidates" />.
    /// </summary>
    public ElementPickerViewModel()
    {
        ActiveTypeFilters.CollectionChanged += OnActiveTypeFiltersCollectionChanged;
    }

    /// <summary>
    ///     Type labels currently applied as chips over the picker, combined with OR semantics:
    ///     an item is shown when its type label is any one of these. An empty collection means
    ///     no type restriction is applied (every candidate's type is shown). Populated by
    ///     <see cref="SetCandidates" />'s <c>defaultTypeFilterLabel</c> argument, and
    ///     subsequently mutated only via <see cref="AddTypeFilter" />/<see cref="RemoveTypeFilter" />
    ///     (or, defensively, any other direct mutation, which is also observed by the internal
    ///     collection-changed handler that re-runs <see cref="RecomputeDisplayedItems" />), so
    ///     the view's chip-row <c>ItemsControl</c> can bind to this instance directly.
    /// </summary>
    public ObservableCollection<string> ActiveTypeFilters { get; } = [];

    /// <summary>
    ///     Replaces the picker's master candidate list with <paramref name="candidates" />,
    ///     recomputes <see cref="AvailableTypeLabels" /> from that list, resets
    ///     <see cref="ActiveTypeFilters" /> to a single-chip default (or empty) per
    ///     <paramref name="defaultTypeFilterLabel" />, and recomputes
    ///     <see cref="DisplayedItems" />. Also clears <see cref="SelectedQualifiedName" /> so
    ///     a stale prior selection cannot linger after a workspace-derived refresh.
    /// </summary>
    /// <param name="candidates">
    ///     The full, unfiltered set of qualified-name / type-label pairs. Assumed to be
    ///     already sorted in whatever order the caller wants displayed; the picker preserves
    ///     that order in <see cref="DisplayedItems" /> when filtering. Must not be
    ///     <see langword="null" />; may be empty.
    /// </param>
    /// <param name="defaultTypeFilterLabel">
    ///     Optional type label to pre-populate <see cref="ActiveTypeFilters" /> with when
    ///     <paramref name="candidates" /> contains at least one entry using that label; when
    ///     <see langword="null" />, or when the label is absent from
    ///     <paramref name="candidates" />, the picker starts with no active type filter
    ///     (every type shown).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="candidates" /> is <see langword="null" />.</exception>
    public void SetCandidates(
        IReadOnlyList<(string QualifiedName, string TypeLabel)> candidates,
        string? defaultTypeFilterLabel = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        _candidates = candidates;
        AvailableTypeLabels = candidates
            .Select(entry => entry.TypeLabel)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(label => label, StringComparer.Ordinal)
            .ToList();

        // Reset the chip row to its per-call default: the requested single-chip default when
        // available, otherwise no restriction. Uses Clear+Add rather than replacing the
        // collection instance so any view bound to ActiveTypeFilters keeps seeing the same
        // ObservableCollection reference.
        ActiveTypeFilters.Clear();
        if (defaultTypeFilterLabel is not null && AvailableTypeLabels.Contains(defaultTypeFilterLabel))
        {
            ActiveTypeFilters.Add(defaultTypeFilterLabel);
        }

        // A candidate replacement invalidates any previously-highlighted qualified name; clear
        // the selection so a caller reading SelectedQualifiedName immediately after
        // SetCandidates never sees a name that is no longer in the picker.
        SelectedQualifiedName = null;

        RecomputeDisplayedItems();
    }

    /// <summary>
    ///     Computes the set of type labels available to add as a new filter chip: every label
    ///     present in <see cref="AvailableTypeLabels" /> that is not already active in
    ///     <see cref="ActiveTypeFilters" />. Computed on demand rather than cached, so a view
    ///     opening its "+" add-filter flyout always sees the current addable set.
    /// </summary>
    /// <returns>
    ///     Type labels not currently applied as an active filter chip, in the same order as
    ///     <see cref="AvailableTypeLabels" />.
    /// </returns>
    public IReadOnlyList<string> GetAddableTypeLabels()
    {
        return AvailableTypeLabels
            .Where(label => !ActiveTypeFilters.Contains(label))
            .ToList();
    }

    /// <summary>
    ///     Adds <paramref name="typeLabel" /> to <see cref="ActiveTypeFilters" /> if it is not
    ///     already present (no duplicate chips), then recomputes <see cref="DisplayedItems" />.
    ///     A no-op beyond the recompute when the label is already active.
    /// </summary>
    /// <param name="typeLabel">Type label chip to add. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLabel" /> is <see langword="null" />.</exception>
    public void AddTypeFilter(string typeLabel)
    {
        ArgumentNullException.ThrowIfNull(typeLabel);

        if (!ActiveTypeFilters.Contains(typeLabel))
        {
            ActiveTypeFilters.Add(typeLabel);
        }

        RecomputeDisplayedItems();
    }

    /// <summary>
    ///     Removes <paramref name="typeLabel" /> from <see cref="ActiveTypeFilters" /> if
    ///     present, then recomputes <see cref="DisplayedItems" />. A no-op beyond the
    ///     recompute when the label is not currently active.
    /// </summary>
    /// <param name="typeLabel">Type label chip to remove. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLabel" /> is <see langword="null" />.</exception>
    public void RemoveTypeFilter(string typeLabel)
    {
        ArgumentNullException.ThrowIfNull(typeLabel);

        ActiveTypeFilters.Remove(typeLabel);

        RecomputeDisplayedItems();
    }

    /// <summary>
    ///     Recomputes <see cref="DisplayedItems" /> from the master <see cref="_candidates" />
    ///     list by applying <see cref="ActiveTypeFilters" /> (OR semantics; empty means no
    ///     type restriction) and then <see cref="SearchText" /> (case-insensitive substring
    ///     match, applied with AND semantics against whatever the type filter already narrowed
    ///     to). The master list's order is preserved.
    /// </summary>
    private void RecomputeDisplayedItems()
    {
        IEnumerable<(string QualifiedName, string TypeLabel)> query = _candidates;

        if (ActiveTypeFilters.Count > 0)
        {
            query = query.Where(entry => ActiveTypeFilters.Contains(entry.TypeLabel));
        }

        var searchText = SearchText;
        if (!string.IsNullOrEmpty(searchText))
        {
            query = query.Where(entry => entry.QualifiedName.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        DisplayedItems = query.Select(entry => entry.QualifiedName).ToList();
    }

    /// <summary>
    ///     CommunityToolkit.Mvvm-generated hook invoked whenever <see cref="SearchText" />
    ///     changes (for example via the view's two-way-bound search <c>TextBox</c>),
    ///     recomputing <see cref="DisplayedItems" /> so the picker updates live as the user
    ///     types.
    /// </summary>
    /// <param name="value">The new search text value.</param>
    partial void OnSearchTextChanged(string? value)
    {
        RecomputeDisplayedItems();
    }

    /// <summary>
    ///     Handles external mutation of <see cref="ActiveTypeFilters" /> (beyond the
    ///     <see cref="AddTypeFilter" />/<see cref="RemoveTypeFilter" /> methods, which already
    ///     recompute directly) by recomputing <see cref="DisplayedItems" />, since a plain
    ///     <see cref="ObservableCollection{T}" /> does not itself participate in
    ///     CommunityToolkit.Mvvm's change notification.
    /// </summary>
    private void OnActiveTypeFiltersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecomputeDisplayedItems();
    }
}
