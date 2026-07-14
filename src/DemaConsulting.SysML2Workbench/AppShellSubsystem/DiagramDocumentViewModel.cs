using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Dock document view model backing one open diagram tab. Multiple instances can exist at once - one per
///     entry in <see cref="MainWindowShell.OpenTabs" /> - each bound to that tab's own <see cref="Canvas" />
///     rather than a single shared canvas, so independently opened diagram tabs have fully independent
///     zoom/pan/content state. This view model holds no rendering logic itself; it only holds the shared
///     <see cref="MainWindowShell" /> reference, this tab's identity, and a minimal notification so
///     <see cref="DiagramDocumentView" /> knows when to repaint after another panel changes this tab's diagram.
///     Unlike the closable-with-restore Tool panels, a diagram document has no restore path once closed - closing
///     it is a normal, always-safe operation (zero diagram tabs open is a supported, first-class shell state, and
///     reopening one is one click away via the catalog or the "+ New Diagram Tab" button), so
///     <see cref="Dock.Model.Core.IDockable.CanClose" /> is left at its Dock-default <see langword="true" />.
/// </summary>
public sealed class DiagramDocumentViewModel : Dock.Model.Mvvm.Controls.Document
{
    /// <summary>
    ///     Fallback canvas used only for the design-time/XAML-previewer construction path, or any transient state
    ///     right after construction but before <see cref="TabId" /> is registered as an open tab.
    /// </summary>
    private readonly SvgCanvasHost _designTimeFallbackCanvas = new();

    /// <summary>
    ///     Creates the diagram document view model for a single open tab.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    /// <param name="tabId">Identifier of the <see cref="WorkbenchTab" /> this document presents.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shell" /> is null.</exception>
    public DiagramDocumentViewModel(MainWindowShell shell, string tabId)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        TabId = tabId ?? throw new ArgumentNullException(nameof(tabId));
    }

    /// <summary>
    ///     Shared application shell whose canvas state backs the diagram surface.
    /// </summary>
    public MainWindowShell Shell { get; }

    /// <summary>
    ///     Identifier of the <see cref="WorkbenchTab" /> this document instance presents.
    /// </summary>
    public string TabId { get; }

    /// <summary>
    ///     This tab's own diagram surface state, looked up from the shell by <see cref="TabId" />, or a private
    ///     fallback canvas if the tab is not (or is no longer) registered with the shell.
    /// </summary>
    public SvgCanvasHost Canvas => Shell.GetTabCanvas(TabId) ?? _designTimeFallbackCanvas;

    /// <summary>
    ///     Raised when another panel has changed this tab's active diagram (a predefined view was selected, or a
    ///     custom view preview was rendered) and the diagram surface should repaint.
    /// </summary>
    public event EventHandler? DiagramChanged;

    /// <summary>
    ///     Notifies the diagram view that it should reload this tab's current diagram.
    /// </summary>
    public void RaiseDiagramChanged()
    {
        DiagramChanged?.Invoke(this, EventArgs.Empty);
    }
}
