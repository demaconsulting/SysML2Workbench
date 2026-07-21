using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the shared <see cref="ElementFilterViewModel" />-backed
///     filter <see cref="UserControl" />. Wires the "+" add-filter flyout, the addable-labels
///     <c>ListBox</c>'s selection, and each chip's remove button to the view model methods
///     that already own the OR/AND filtering, dedupe, and no-op-on-absent semantics.
/// </summary>
/// <remarks>
///     This control has no selection concept: it renders only the chip row and search box.
///     Callers that need a selectable candidate list on top of the same filtering behavior
///     should use <see cref="ElementPickerViewModel" />/<see cref="ElementPickerView" />
///     instead, which composes an <see cref="ElementFilterViewModel" /> internally and embeds
///     this control for its chip-row/search markup.
/// </remarks>
public partial class ElementFilterView : UserControl
{
    /// <summary>
    ///     Creates the filter view. The <see cref="ElementFilterViewModel" /> is supplied by
    ///     the parent through the standard <c>DataContext</c> binding, not by this constructor.
    /// </summary>
    public ElementFilterView()
    {
        InitializeComponent();

        AddTypeFilterButton.Flyout!.Opened += OnAddTypeFilterFlyoutOpening;
        AddableTypeFilterListBox.SelectionChanged += OnAddableTypeFilterSelectionChanged;
    }

    /// <summary>
    ///     Returns the filter view model this control is currently bound to, or
    ///     <see langword="null" /> when no compatible <c>DataContext</c> is set (for example
    ///     during design-time construction). Provided so parent dialogs can invoke view-model
    ///     methods (<see cref="ElementFilterViewModel.GetAddableTypeLabels" />,
    ///     <see cref="ElementFilterViewModel.AddTypeFilter" />, and
    ///     <see cref="ElementFilterViewModel.RemoveTypeFilter" />) from their own code-behind
    ///     without needing to know the control's internal children.
    /// </summary>
    public ElementFilterViewModel? ViewModel => DataContext as ElementFilterViewModel;

    /// <summary>
    ///     Populates <see cref="AddableTypeFilterListBox" /> from the view model's currently
    ///     addable type labels each time the "+" button's flyout is about to open, so the
    ///     list always reflects the latest workspace/active-filter state rather than a stale
    ///     snapshot from construction time.
    /// </summary>
    private void OnAddTypeFilterFlyoutOpening(object? sender, EventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        AddableTypeFilterListBox.ItemsSource = ViewModel.GetAddableTypeLabels();
    }

    /// <summary>
    ///     Adds the selected type label as a new active filter chip and closes the flyout.
    ///     Also clears the flyout <c>ListBox</c>'s own <c>SelectedItem</c> so re-opening it
    ///     starts with no highlighted row.
    /// </summary>
    private void OnAddableTypeFilterSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null || AddableTypeFilterListBox.SelectedItem is not string typeLabel)
        {
            return;
        }

        ViewModel.AddTypeFilter(typeLabel);
        AddableTypeFilterListBox.SelectedItem = null;
        AddTypeFilterButton.Flyout?.Hide();
    }

    /// <summary>
    ///     Removes the clicked chip's type label from the active filters. The chip's own
    ///     <c>Tag</c> carries the label so this handler stays generic across every chip.
    /// </summary>
    private void OnRemoveTypeFilterClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null && sender is Button { Tag: string typeLabel })
        {
            ViewModel.RemoveTypeFilter(typeLabel);
        }
    }
}
