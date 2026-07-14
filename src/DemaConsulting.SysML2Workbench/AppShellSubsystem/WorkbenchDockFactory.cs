using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Composes the four Phase-0 panels into a resizable/floatable/closable Dock layout, approximating the
///     legacy fixed-<c>DockPanel</c> arrangement's default proportions (left ~260px, right ~320px, bottom ~180px
///     against the window's default 1280x800 size) as initial <see cref="Dock.Model.Core.IDockable" />
///     proportions, while leaving every panel user-resizable, floatable, and closable through Dock's own chrome.
///     <c>HideToolsOnClose</c> is set so that closing a <see cref="Dock.Model.Mvvm.Controls.Tool" /> hides it
///     (tracked in <see cref="Dock.Model.Controls.IRootDock.HiddenDockables" /> and restorable via
///     <c>RestoreDockable</c>) rather than permanently removing it, so the "View" menu can bring a closed panel
///     back without losing its in-progress state. This setting applies factory-wide, so any future
///     <see cref="Dock.Model.Mvvm.Controls.Tool" /> panel added to this factory will also hide-not-destroy on
///     close.
/// </summary>
public sealed class WorkbenchDockFactory : Factory
{
    private readonly PredefinedViewsToolViewModel _predefinedViewsViewModel;
    private readonly CustomViewBuilderToolViewModel _customViewBuilderViewModel;
    private readonly DiagnosticsToolViewModel _diagnosticsViewModel;
    private readonly DiagramDocumentViewModel _diagramViewModel;

    /// <summary>
    ///     Creates the dock layout factory over the four already-constructed panel view models.
    /// </summary>
    /// <param name="predefinedViewsViewModel">Predefined-views tool panel.</param>
    /// <param name="customViewBuilderViewModel">Custom-view builder tool panel.</param>
    /// <param name="diagnosticsViewModel">Diagnostics tool panel.</param>
    /// <param name="diagramViewModel">Single always-open diagram document.</param>
    public WorkbenchDockFactory(
        PredefinedViewsToolViewModel predefinedViewsViewModel,
        CustomViewBuilderToolViewModel customViewBuilderViewModel,
        DiagnosticsToolViewModel diagnosticsViewModel,
        DiagramDocumentViewModel diagramViewModel)
    {
        _predefinedViewsViewModel = predefinedViewsViewModel ?? throw new ArgumentNullException(nameof(predefinedViewsViewModel));
        _customViewBuilderViewModel = customViewBuilderViewModel ?? throw new ArgumentNullException(nameof(customViewBuilderViewModel));
        _diagnosticsViewModel = diagnosticsViewModel ?? throw new ArgumentNullException(nameof(diagnosticsViewModel));
        _diagramViewModel = diagramViewModel ?? throw new ArgumentNullException(nameof(diagramViewModel));

        HideToolsOnClose = true;
    }

    /// <inheritdoc />
    public override IRootDock CreateLayout()
    {
        var predefinedViewsDock = new ToolDock
        {
            Id = "PredefinedViewsPane",
            Alignment = Alignment.Left,
            Proportion = 0.20,
            VisibleDockables = CreateList<IDockable>(_predefinedViewsViewModel),
            ActiveDockable = _predefinedViewsViewModel,
        };

        var customViewBuilderDock = new ToolDock
        {
            Id = "CustomViewBuilderPane",
            Alignment = Alignment.Right,
            Proportion = 0.25,
            VisibleDockables = CreateList<IDockable>(_customViewBuilderViewModel),
            ActiveDockable = _customViewBuilderViewModel,
        };

        var diagnosticsDock = new ToolDock
        {
            Id = "DiagnosticsPane",
            Alignment = Alignment.Bottom,
            Proportion = 0.22,
            VisibleDockables = CreateList<IDockable>(_diagnosticsViewModel),
            ActiveDockable = _diagnosticsViewModel,
        };

        var documentDock = new DocumentDock
        {
            Id = "DiagramDock",
            Proportion = 0.78,
            CanCreateDocument = false,
            VisibleDockables = CreateList<IDockable>(_diagramViewModel),
            ActiveDockable = _diagramViewModel,
        };

        var centerVerticalDock = new ProportionalDock
        {
            Id = "CenterVertical",
            Orientation = Orientation.Vertical,
            Proportion = 0.55,
            VisibleDockables = CreateList<IDockable>(documentDock, diagnosticsDock),
        };

        var mainHorizontalDock = new ProportionalDock
        {
            Id = "MainHorizontal",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(predefinedViewsDock, centerVerticalDock, customViewBuilderDock),
        };

        var root = CreateRootDock();
        root.Id = "Root";
        root.VisibleDockables = CreateList<IDockable>(mainHorizontalDock);
        root.DefaultDockable = mainHorizontalDock;
        root.ActiveDockable = mainHorizontalDock;

        return root;
    }
}
