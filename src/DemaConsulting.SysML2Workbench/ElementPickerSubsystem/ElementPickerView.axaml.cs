using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the shared <see cref="ElementPickerViewModel" />-backed
///     picker <see cref="UserControl" />. Wires the "+" add-filter flyout, the addable-labels
///     <c>ListBox</c>'s selection, and each chip's remove button to the view model methods
///     that already own the OR/AND filtering, dedupe, and no-op-on-absent semantics.
/// </summary>
/// <remarks>
///     The candidate <c>ListBox</c>'s <c>SelectedItem</c> is bound directly to
///     <see cref="ElementPickerViewModel.SelectedQualifiedName" /> via XAML two-way binding,
///     so parent dialogs read the current selection via that property rather than reaching
///     into this view's named children. Hosting parents that need to react to the flyout or
///     chip lifecycle themselves should observe <see cref="ElementPickerViewModel" />'s
///     <c>ActiveTypeFilters</c> collection instead of hooking events on this control.
/// </remarks>
public partial class ElementPickerView : UserControl
{
    /// <summary>
    ///     Creates the picker view. The <see cref="ElementPickerViewModel" /> is supplied by
    ///     the parent through the standard <c>DataContext</c> binding, not by this constructor.
    /// </summary>
    public ElementPickerView()
    {
        InitializeComponent();

        AddTypeFilterButton.Flyout!.Opened += OnAddTypeFilterFlyoutOpening;
        AddableTypeFilterListBox.SelectionChanged += OnAddableTypeFilterSelectionChanged;
    }

    /// <summary>
    ///     Returns the picker view model this control is currently bound to, or
    ///     <see langword="null" /> when no compatible <c>DataContext</c> is set (for example
    ///     during design-time construction). Provided so parent dialogs can invoke view-model
    ///     methods (<see cref="ElementPickerViewModel.GetAddableTypeLabels" />,
    ///     <see cref="ElementPickerViewModel.AddTypeFilter" />, and
    ///     <see cref="ElementPickerViewModel.RemoveTypeFilter" />) from their own code-behind
    ///     without needing to know the control's internal children.
    /// </summary>
    public ElementPickerViewModel? ViewModel => DataContext as ElementPickerViewModel;

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
