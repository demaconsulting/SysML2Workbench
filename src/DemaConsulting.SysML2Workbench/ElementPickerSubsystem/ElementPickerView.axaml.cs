using Avalonia.Controls;

namespace DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the shared <see cref="ElementPickerViewModel" />-backed
///     picker <see cref="UserControl" />. The chip-row/search flyout wiring now lives entirely
///     in <see cref="ElementFilterView" />, which this control embeds (bound to
///     <see cref="ElementPickerViewModel.Filter" />) for that markup; this class only needs to
///     surface a convenience accessor to its bound view model for any hosting parent that
///     wants to reach view-model methods without walking this control's named children.
/// </summary>
/// <remarks>
///     The candidate <c>ListBox</c>'s <c>SelectedItem</c> is bound directly to
///     <see cref="ElementPickerViewModel.SelectedQualifiedName" /> via XAML two-way binding,
///     so parent dialogs read the current selection via that property rather than reaching
///     into this view's named children.
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
    }

    /// <summary>
    ///     Returns the picker view model this control is currently bound to, or
    ///     <see langword="null" /> when no compatible <c>DataContext</c> is set (for example
    ///     during design-time construction). Provided so parent dialogs can invoke view-model
    ///     methods from their own code-behind without needing to know the control's internal
    ///     children.
    /// </summary>
    public ElementPickerViewModel? ViewModel => DataContext as ElementPickerViewModel;
}
