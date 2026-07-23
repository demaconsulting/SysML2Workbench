using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

/// <summary>
///     Reusable, dialog-agnostic view model backing a filter-only "Element Filter" control - a
///     chip-row of type-label filters (OR semantics) and a case-insensitive substring search text
///     box, with NO selection concept whatsoever. This is the pure filtering half of the
///     picker/filter split: <see cref="ElementPickerViewModel" /> composes an instance of this
///     class to add its own selection concept on top, and callers with no target-element concept
///     at all (for example the Query dialog's "List" Query Type) use an instance of this class
///     directly.
/// </summary>
/// <remarks>
///     Extracted from <see cref="ElementPickerViewModel" /> so the filtering/chip-management logic
///     can be reused without dragging along a selection concept that some callers (the Query
///     dialog's "List" mode) have no use for and would otherwise silently ignore. The view model
///     is deliberately independent of <c>ViewDefinitionModel</c>, <c>MainWindowShell</c>, and any
///     workspace type: the caller is responsible for building the candidate list (typically by
///     mapping <c>SysmlWorkspace.Declarations</c> through <see cref="ElementTypeLabeler.GetTypeLabel" />
///     and applying any caller-owned exclusions such as stdlib names or unsupported node kinds) and
///     handing it to <see cref="SetCandidates" />. Not thread-safe: all state (properties and
///     <see cref="ActiveTypeFilters" />) must be mutated from a single (typically UI) thread.
/// </remarks>
public sealed partial class ElementFilterViewModel : ObservableObject
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
    private string? _addableTypeFilterSearchText = "";

    [ObservableProperty]
    private IReadOnlyList<string> _addableTypeFilterCandidates = [];

    /// <summary>
    ///     Creates the filter view model in its empty initial state: no candidates, no active
    ///     type filters, empty search text, and an empty displayed list. Callers populate it
    ///     later by calling <see cref="SetCandidates" />.
    /// </summary>
    public ElementFilterViewModel()
    {
        ActiveTypeFilters.CollectionChanged += OnActiveTypeFiltersCollectionChanged;
    }

    /// <summary>
    ///     Type labels currently applied as chips over the filter, combined with OR semantics:
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
    ///     Replaces the filter's master candidate list with <paramref name="candidates" />,
    ///     recomputes <see cref="AvailableTypeLabels" /> from that list, resets
    ///     <see cref="ActiveTypeFilters" /> to a single-chip default (or empty) per
    ///     <paramref name="defaultTypeFilterLabel" />, and recomputes
    ///     <see cref="DisplayedItems" />. There is no selection concept to clear here (unlike
    ///     <see cref="ElementPickerViewModel.SetCandidates" />).
    /// </summary>
    /// <param name="candidates">
    ///     The full, unfiltered set of qualified-name / type-label pairs. Assumed to be
    ///     already sorted in whatever order the caller wants displayed; the filter preserves
    ///     that order in <see cref="DisplayedItems" /> when filtering. Must not be
    ///     <see langword="null" />; may be empty.
    /// </param>
    /// <param name="defaultTypeFilterLabel">
    ///     Optional type label to pre-populate <see cref="ActiveTypeFilters" /> with when
    ///     <paramref name="candidates" /> contains at least one entry using that label; when
    ///     <see langword="null" />, or when the label is absent from
    ///     <paramref name="candidates" />, the filter starts with no active type filter
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
    ///     Resets <see cref="AddableTypeFilterSearchText" /> to empty and recomputes
    ///     <see cref="AddableTypeFilterCandidates" /> from the current addable set. Called each
    ///     time the "+" add-filter flyout is about to open, so it always starts showing the full
    ///     addable set rather than stale search text/results left over from a previous opening.
    /// </summary>
    public void BeginAddableTypeFilterSearch()
    {
        AddableTypeFilterSearchText = "";
        RecomputeAddableTypeFilterCandidates();
    }

    /// <summary>
    ///     Adds the first entry in <see cref="AddableTypeFilterCandidates" /> (the current
    ///     search's top/only match) as a new active filter chip, the same "type ahead then
    ///     commit" semantics as a combo box's highlighted match. A no-op that returns
    ///     <see langword="false" /> when the current search text matches no addable label.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> when a chip was added; <see langword="false" /> when
    ///     <see cref="AddableTypeFilterCandidates" /> was empty.
    /// </returns>
    public bool TryCommitAddableTypeFilterSearch()
    {
        var topMatch = AddableTypeFilterCandidates.FirstOrDefault();
        if (topMatch is null)
        {
            return false;
        }

        AddTypeFilter(topMatch);
        return true;
    }

    /// <summary>
    ///     Recomputes <see cref="AddableTypeFilterCandidates" /> from <see cref="GetAddableTypeLabels" />,
    ///     narrowed by a case-insensitive substring match against <see cref="AddableTypeFilterSearchText" />
    ///     (an empty/null search shows every addable label).
    /// </summary>
    private void RecomputeAddableTypeFilterCandidates()
    {
        IEnumerable<string> query = GetAddableTypeLabels();

        var searchText = AddableTypeFilterSearchText;
        if (!string.IsNullOrEmpty(searchText))
        {
            query = query.Where(label => label.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        AddableTypeFilterCandidates = query.ToList();
    }

    /// <summary>
    ///     CommunityToolkit.Mvvm-generated hook invoked whenever
    ///     <see cref="AddableTypeFilterSearchText" /> changes (via the add-filter flyout's
    ///     two-way-bound search <c>TextBox</c>), recomputing <see cref="AddableTypeFilterCandidates" />
    ///     so the flyout's list narrows live as the user types.
    /// </summary>
    /// <param name="value">The new search text value.</param>
    partial void OnAddableTypeFilterSearchTextChanged(string? value)
    {
        RecomputeAddableTypeFilterCandidates();
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
    ///     recomputing <see cref="DisplayedItems" /> so the filter updates live as the user
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
