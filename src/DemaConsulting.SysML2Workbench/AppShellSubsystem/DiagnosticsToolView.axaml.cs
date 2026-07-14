using Avalonia.Controls;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia view for the "Diagnostics" Dock tool panel. All state lives in
///     <see cref="DiagnosticsToolViewModel" />, bound as this control's data context by Dock when the panel is
///     realized.
/// </summary>
public partial class DiagnosticsToolView : UserControl
{
    /// <summary>
    ///     Constructor used both at runtime (by Dock's view locator) and by the Avalonia XAML previewer/designer.
    /// </summary>
    public DiagnosticsToolView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = new DiagnosticsToolViewModel(DesignTimeShellFactory.Create());
        }
    }
}
