namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Dock document view model backing one open read-only source-text tab. Multiple instances can exist at
///     once - one per <see cref="WorkbenchTabKind.SourceText" /> entry in
///     <see cref="MainWindowShell.OpenTabs" /> - each presenting a different workspace file. This is a Phase 1,
///     read-only viewer: the file's contents are read once, eagerly, at construction; there is no caching layer
///     to invalidate, no file-watch/auto-refresh, and no write-back path. Unlike <see cref="DiagramDocumentViewModel" />,
///     a source-text tab has no restore-vs-close distinction to model - closing it is always a safe, ordinary
///     operation, and reopening it is one double-click away.
/// </summary>
public sealed class SourceTextDocumentViewModel : Dock.Model.Mvvm.Controls.Document
{
    /// <summary>
    ///     Creates the source-text document view model for a single open tab.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    /// <param name="tabId">Identifier of the <see cref="WorkbenchTab" /> this document presents.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shell" /> is null.</exception>
    public SourceTextDocumentViewModel(MainWindowShell shell, string tabId)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        TabId = tabId ?? throw new ArgumentNullException(nameof(tabId));

        var filePath = Shell.GetTabFilePath(TabId);
        Title = filePath is null ? "Source" : Path.GetFileName(filePath);
        Text = ReadFileTextOrFriendlyError(filePath);
    }

    /// <summary>
    ///     Shared application shell whose tab registry resolves this document's file path.
    /// </summary>
    public MainWindowShell Shell { get; }

    /// <summary>
    ///     Identifier of the <see cref="WorkbenchTab" /> this document instance presents.
    /// </summary>
    public string TabId { get; }

    /// <summary>
    ///     The raw text this tab displays: either the file's exact on-disk contents at the time this document was
    ///     constructed, or a friendly, non-throwing error message when the file could not be read.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Reads the given file's full text, or produces a friendly in-editor error message instead of throwing
    ///     when the file is missing, deleted, or inaccessible - a read-only viewer must never crash the shell
    ///     over a stale workspace-tree entry pointing at a file that has since disappeared or been locked by
    ///     another process.
    /// </summary>
    /// <param name="filePath">Absolute path of the file to read, or <see langword="null" /> if unresolved.</param>
    /// <returns>The file's contents, or a friendly error message.</returns>
    private static string ReadFileTextOrFriendlyError(string? filePath)
    {
        if (filePath is null)
        {
            return "This tab is no longer associated with an open workspace file.";
        }

        try
        {
            return File.ReadAllText(filePath);
        }
        catch (IOException ex)
        {
            return $"Unable to read '{filePath}': {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            return $"Unable to read '{filePath}': {ex.Message}";
        }
    }
}
