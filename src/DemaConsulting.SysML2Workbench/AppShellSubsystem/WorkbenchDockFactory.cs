using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Composes the four Phase-0 panels plus the Workspace sources panel into a resizable/floatable/closable
///     Dock layout, approximating the legacy fixed-<c>DockPanel</c> arrangement's default proportions (left
///     ~260px, right ~320px, bottom ~180px against the window's default 1280x800 size) as initial
///     <see cref="Dock.Model.Core.IDockable" /> proportions, while leaving every panel user-resizable, floatable,
///     and closable through Dock's own chrome. The Workspace panel shares the Left column with the Predefined
///     Views panel as a second tab (rather than its own column), keeping the other three panes' proportions
///     unchanged.
///     <c>HideToolsOnClose</c> is set so that closing a <see cref="Dock.Model.Mvvm.Controls.Tool" /> hides it
///     (tracked in <see cref="Dock.Model.Controls.IRootDock.HiddenDockables" /> and restorable via
///     <c>RestoreDockable</c>) rather than permanently removing it, so the "View" menu can bring a closed panel
///     back without losing its in-progress state. This setting applies factory-wide, so any future
///     <see cref="Dock.Model.Mvvm.Controls.Tool" /> panel added to this factory will also hide-not-destroy on
///     close.
///     The diagram <see cref="DocumentDock" /> (exposed as <see cref="DiagramDock" />) is built with an initially
///     empty document list and dynamically managed at runtime by <see cref="MainWindowView" /> (via the inherited
///     <c>AddDockable</c>/<c>RemoveDockable</c>) as diagram tabs open and close - <c>Document</c>s, unlike
///     <c>Tool</c>s, are removed outright on close with no restore path, which is an intentional, documented
///     departure from the Tool panels' hide/restore behavior (matching how closing a diagram tab is always safe:
///     zero tabs is a supported, first-class state, and reopening one is one click away). The
///     <see cref="DiagramDock" /> itself sets <c>IsCollapsable = false</c> so it (and its parent
///     <c>ProportionalDock</c> branch) remains visibly present in the layout even with zero documents open, rather
///     than collapsing/disappearing once its last document closes.
/// </summary>
public sealed class WorkbenchDockFactory : Factory
{
    private readonly PredefinedViewsToolViewModel _predefinedViewsViewModel;
    private readonly CustomViewBuilderToolViewModel _customViewBuilderViewModel;
    private readonly DiagnosticsToolViewModel _diagnosticsViewModel;
    private readonly WorkspacePanelToolViewModel _workspacePanelViewModel;

    /// <summary>
    ///     Raised after a <see cref="DiagramDocumentViewModel" /> is closed through Dock's own chrome (or any
    ///     other path that ultimately calls <c>CloseDockable</c>), so <see cref="MainWindowView" /> can retire the
    ///     corresponding <see cref="WorkbenchTab" /> from <see cref="MainWindowShell" />.
    /// </summary>
    public event EventHandler<DiagramDocumentViewModel>? DiagramTabClosed;

    /// <summary>
    ///     Creates the dock layout factory over the four already-constructed Tool panel view models. The diagram
    ///     <see cref="DocumentDock" /> is populated dynamically at runtime rather than at construction time - see
    ///     <see cref="DiagramDock" />.
    /// </summary>
    /// <param name="predefinedViewsViewModel">Predefined-views tool panel.</param>
    /// <param name="customViewBuilderViewModel">Custom-view builder tool panel.</param>
    /// <param name="diagnosticsViewModel">Diagnostics tool panel.</param>
    /// <param name="workspacePanelViewModel">Workspace sources tool panel.</param>
    public WorkbenchDockFactory(
        PredefinedViewsToolViewModel predefinedViewsViewModel,
        CustomViewBuilderToolViewModel customViewBuilderViewModel,
        DiagnosticsToolViewModel diagnosticsViewModel,
        WorkspacePanelToolViewModel workspacePanelViewModel)
    {
        _predefinedViewsViewModel = predefinedViewsViewModel ?? throw new ArgumentNullException(nameof(predefinedViewsViewModel));
        _customViewBuilderViewModel = customViewBuilderViewModel ?? throw new ArgumentNullException(nameof(customViewBuilderViewModel));
        _diagnosticsViewModel = diagnosticsViewModel ?? throw new ArgumentNullException(nameof(diagnosticsViewModel));
        _workspacePanelViewModel = workspacePanelViewModel ?? throw new ArgumentNullException(nameof(workspacePanelViewModel));

        HideToolsOnClose = true;
    }

    /// <summary>
    ///     The dock hosting all open diagram documents, exposed so <see cref="MainWindowView" /> can dynamically
    ///     add/remove <see cref="DiagramDocumentViewModel" /> instances and set the active/focused document as
    ///     tabs open, close, or become active.
    /// </summary>
    public IDocumentDock DiagramDock { get; private set; } = null!;

    /// <inheritdoc />
    public override IRootDock CreateLayout()
    {
        var predefinedViewsDock = new ToolDock
        {
            Id = "PredefinedViewsPane",
            Alignment = Alignment.Left,
            Proportion = 0.20,
            VisibleDockables = CreateList<IDockable>(_predefinedViewsViewModel, _workspacePanelViewModel),
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
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(),

            // Dock.Model.Mvvm.Controls.DocumentDock defaults EmptyContent to the literal string
            // "No documents open", rendered centered in the document area whenever it has zero open
            // documents. Cleared so zero open diagram tabs is a plain blank area (matching Visual Studio's
            // editor region with no files open), not a placeholder message.
            EmptyContent = null,
        };
        DiagramDock = documentDock;

        var centerVerticalDock = new ProportionalDock
        {
            Id = "CenterVertical",
            Orientation = Orientation.Vertical,
            Proportion = 0.55,
            VisibleDockables = CreateList<IDockable>(documentDock, CreateProportionalDockSplitter(), diagnosticsDock),
        };

        var mainHorizontalDock = new ProportionalDock
        {
            Id = "MainHorizontal",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                predefinedViewsDock,
                CreateProportionalDockSplitter(),
                centerVerticalDock,
                CreateProportionalDockSplitter(),
                customViewBuilderDock),
        };

        var root = CreateRootDock();
        root.Id = "Root";
        root.VisibleDockables = CreateList<IDockable>(mainHorizontalDock);
        root.DefaultDockable = mainHorizontalDock;
        root.ActiveDockable = mainHorizontalDock;

        return root;
    }

    /// <inheritdoc />
    public override void OnDockableClosed(IDockable? dockable)
    {
        base.OnDockableClosed(dockable);

        if (dockable is DiagramDocumentViewModel diagramDocument)
        {
            DiagramTabClosed?.Invoke(this, diagramDocument);
        }
    }
}
