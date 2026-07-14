using Avalonia.Controls;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia view for the "Predefined Views" Dock tool panel. All state and orchestration live in
///     <see cref="PredefinedViewsToolViewModel" />, bound as this control's data context by Dock when the panel
///     is realized.
/// </summary>
public partial class PredefinedViewsToolView : UserControl
{
    /// <summary>
    ///     Constructor used both at runtime (Dock's view locator creates this control with no arguments and then
    ///     assigns the corresponding <see cref="PredefinedViewsToolViewModel" /> as its data context) and by the
    ///     Avalonia XAML previewer/designer, which is given a throwaway design-time view model.
    /// </summary>
    public PredefinedViewsToolView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            var shell = DesignTimeShellFactory.Create();
            DataContext = new PredefinedViewsToolViewModel(shell, new DiagramDocumentViewModel(shell));
        }
    }
}
