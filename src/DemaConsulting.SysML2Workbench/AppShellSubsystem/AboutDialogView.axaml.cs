using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia code-behind for the modal About dialog. All displayed content is bound from
///     <see cref="AboutDialogViewModel" />; this class only constructs that view model as the dialog's data
///     context and closes the window when the user dismisses it.
/// </summary>
public partial class AboutDialogView : Window
{
    /// <summary>
    ///     Creates the About dialog, constructing a fresh <see cref="AboutDialogViewModel" /> as its data context.
    ///     Used both at runtime (by <see cref="MainWindowView" />'s Help menu handler) and by the Avalonia XAML
    ///     previewer/designer.
    /// </summary>
    public AboutDialogView()
    {
        InitializeComponent();

        DataContext = new AboutDialogViewModel();
    }

    /// <summary>
    ///     Handles the OK button click by closing the dialog.
    /// </summary>
    private void OnOkButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
