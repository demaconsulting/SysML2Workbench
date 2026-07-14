namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Dock document view model backing the single, always-open diagram surface. Phase 0 hosts exactly one
///     diagram document (no multi-tab/document support); this view model only holds the shared
///     <see cref="MainWindowShell" /> reference and a minimal notification so <see cref="DiagramDocumentView" />
///     knows when to repaint after another panel changes the shell's active diagram.
/// </summary>
public sealed class DiagramDocumentViewModel : Dock.Model.Mvvm.Controls.Document
{
    /// <summary>
    ///     Creates the diagram document view model.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public DiagramDocumentViewModel(MainWindowShell shell)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    /// <summary>
    ///     Shared application shell whose canvas state backs the diagram surface.
    /// </summary>
    public MainWindowShell Shell { get; }

    /// <summary>
    ///     Raised when another panel has changed the shell's active diagram (a predefined view was selected, or
    ///     a custom view preview was rendered) and the diagram surface should repaint.
    /// </summary>
    public event EventHandler? DiagramChanged;

    /// <summary>
    ///     Notifies the diagram view that it should reload the shell's current diagram.
    /// </summary>
    public void RaiseDiagramChanged()
    {
        DiagramChanged?.Invoke(this, EventArgs.Empty);
    }
}
