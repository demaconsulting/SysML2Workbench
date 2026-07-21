using System.Collections.ObjectModel;
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
///     picker without duplicating the OR-then-AND filtering logic and chip management. This
///     view model composes an <see cref="ElementFilterViewModel" /> instance (<see cref="Filter" />)
///     to own the filtering/chip-management logic itself, and adds only its own
///     <see cref="SelectedQualifiedName" /> selection concept on top; every filtering-related
///     public member (<see cref="AvailableTypeLabels" />, <see cref="SearchText" />,
///     <see cref="DisplayedItems" />, <see cref="ActiveTypeFilters" />,
///     <see cref="GetAddableTypeLabels" />, <see cref="AddTypeFilter" />,
///     <see cref="RemoveTypeFilter" />) is a thin pass-through to <see cref="Filter" />, so
///     existing callers and bindings see no change to this class's public API. The view
///     model is deliberately independent of <c>ViewDefinitionModel</c>, <c>MainWindowShell</c>,
///     and any workspace type: the caller is responsible for building the candidate list
///     (typically by mapping <c>SysmlWorkspace.Declarations</c> through
///     <see cref="ElementTypeLabeler.GetTypeLabel" /> and applying any caller-owned
///     exclusions such as stdlib names or unsupported node kinds) and handing it to
///     <see cref="SetCandidates" />. Not thread-safe: all state (properties and
///     <see cref="ActiveTypeFilters" />) must be mutated from a single (typically UI) thread.
/// </remarks>
public sealed partial class ElementPickerViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _selectedQualifiedName;

    /// <summary>
    ///     Creates the picker view model in its empty initial state: no candidates, no active
    ///     type filters, empty search text, an empty displayed list, and no selection.
    ///     Callers populate it later by calling <see cref="SetCandidates" />.
    /// </summary>
    public ElementPickerViewModel()
    {
        Filter = new ElementFilterViewModel();
        Filter.PropertyChanged += OnFilterPropertyChanged;
    }

    /// <summary>
    ///     The composed, selection-free filter view model that actually owns the candidate
    ///     list, chip management, and search-text filtering. Exposed so a hosting
    ///     <see cref="ElementPickerView" /> can embed an <see cref="ElementFilterView" /> bound
    ///     directly to this instance for its chip-row/search markup, and so callers that need
    ///     only the filter (no selection) - such as the Query dialog's "List" Query Type - can
    ///     use a standalone <see cref="ElementFilterViewModel" /> instead of this class.
    /// </summary>
    public ElementFilterViewModel Filter { get; }

    /// <summary>
    ///     Pass-through to <see cref="Filter" />'s <see cref="ElementFilterViewModel.AvailableTypeLabels" />.
    /// </summary>
    public IReadOnlyList<string> AvailableTypeLabels => Filter.AvailableTypeLabels;

    /// <summary>
    ///     Pass-through to <see cref="Filter" />'s <see cref="ElementFilterViewModel.SearchText" />.
    ///     A manual property (rather than <c>[ObservableProperty]</c>) because it must forward
    ///     both reads and writes to <see cref="Filter" /> rather than backing its own field.
    /// </summary>
    public string? SearchText
    {
        get => Filter.SearchText;
        set => Filter.SearchText = value;
    }

    /// <summary>
    ///     Pass-through to <see cref="Filter" />'s <see cref="ElementFilterViewModel.DisplayedItems" />.
    /// </summary>
    public IReadOnlyList<string> DisplayedItems => Filter.DisplayedItems;

    /// <summary>
    ///     Pass-through to <see cref="Filter" />'s <see cref="ElementFilterViewModel.ActiveTypeFilters" />.
    ///     Returns the same <see cref="ObservableCollection{T}" /> instance every time, so a
    ///     view bound to this property observes the same collection-changed notifications as
    ///     one bound directly to <see cref="Filter" />.
    /// </summary>
    public ObservableCollection<string> ActiveTypeFilters => Filter.ActiveTypeFilters;

    /// <summary>
    ///     Replaces the picker's master candidate list with <paramref name="candidates" /> by
    ///     forwarding to <see cref="Filter" />'s <see cref="ElementFilterViewModel.SetCandidates" />,
    ///     then clears <see cref="SelectedQualifiedName" /> so a stale prior selection cannot
    ///     linger after a workspace-derived refresh.
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
        Filter.SetCandidates(candidates, defaultTypeFilterLabel);

        // A candidate replacement invalidates any previously-highlighted qualified name; clear
        // the selection so a caller reading SelectedQualifiedName immediately after
        // SetCandidates never sees a name that is no longer in the picker.
        SelectedQualifiedName = null;
    }

    /// <summary>
    ///     Pass-through to <see cref="Filter" />'s <see cref="ElementFilterViewModel.GetAddableTypeLabels" />.
    /// </summary>
    /// <returns>
    ///     Type labels not currently applied as an active filter chip, in the same order as
    ///     <see cref="AvailableTypeLabels" />.
    /// </returns>
    public IReadOnlyList<string> GetAddableTypeLabels()
    {
        return Filter.GetAddableTypeLabels();
    }

    /// <summary>
    ///     Pass-through to <see cref="Filter" />'s <see cref="ElementFilterViewModel.AddTypeFilter" />.
    /// </summary>
    /// <param name="typeLabel">Type label chip to add. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLabel" /> is <see langword="null" />.</exception>
    public void AddTypeFilter(string typeLabel)
    {
        Filter.AddTypeFilter(typeLabel);
    }

    /// <summary>
    ///     Pass-through to <see cref="Filter" />'s <see cref="ElementFilterViewModel.RemoveTypeFilter" />.
    /// </summary>
    /// <param name="typeLabel">Type label chip to remove. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLabel" /> is <see langword="null" />.</exception>
    public void RemoveTypeFilter(string typeLabel)
    {
        Filter.RemoveTypeFilter(typeLabel);
    }

    /// <summary>
    ///     Re-raises this view model's own <c>PropertyChanged</c> notification whenever
    ///     <see cref="Filter" /> reports a change to one of the pass-through properties this
    ///     class exposes, so code/bindings observing <see cref="ElementPickerViewModel" />
    ///     directly (rather than <see cref="Filter" />) still see live updates. No re-raise is
    ///     needed for <see cref="ActiveTypeFilters" />: the same <see cref="ObservableCollection{T}" />
    ///     reference is returned by both this class and <see cref="Filter" />, so its own
    ///     <c>CollectionChanged</c> event already fires for any bound view/code.
    /// </summary>
    /// <param name="sender">The <see cref="Filter" /> instance raising the notification.</param>
    /// <param name="e">The changed property's event arguments.</param>
    private void OnFilterPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ElementFilterViewModel.DisplayedItems):
                OnPropertyChanged(nameof(DisplayedItems));
                break;

            case nameof(ElementFilterViewModel.AvailableTypeLabels):
                OnPropertyChanged(nameof(AvailableTypeLabels));
                break;

            case nameof(ElementFilterViewModel.SearchText):
                OnPropertyChanged(nameof(SearchText));
                break;
        }
    }
}
