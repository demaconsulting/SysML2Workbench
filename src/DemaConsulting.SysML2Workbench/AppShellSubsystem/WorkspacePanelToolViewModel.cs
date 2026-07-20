using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;
using Material.Icons;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     One node in the workspace tree shown by <see cref="WorkspacePanelToolView" />.
/// </summary>
public abstract class WorkspaceTreeNode
{
    /// <summary>
    ///     Stable identifier used as the tree control's item key.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     The icon shown alongside this node in the tree.
    /// </summary>
    public abstract MaterialIconKind IconKind { get; }
}

/// <summary>
///     A tree node representing one <see cref="WorkspaceSource" />. Has no children for a
///     <see cref="WorkspaceSourceKind.File" /> source (a leaf, with no expand arrow); has one child per
///     top-level entry (subfolder or file) discovered under a <see cref="WorkspaceSourceKind.Folder" /> source,
///     preserving that folder's on-disk hierarchy rather than flattening every discovered file into a single
///     list (see <see cref="WorkspaceFolderNode" />).
/// </summary>
public sealed class WorkspaceSourceNode : WorkspaceTreeNode
{
    /// <summary>
    ///     The source this node represents.
    /// </summary>
    public required WorkspaceSource Source { get; init; }

    /// <summary>
    ///     Top-level children (subfolders and/or files) contributed by this source. Empty for a
    ///     <see cref="WorkspaceSourceKind.File" /> source.
    /// </summary>
    public required IReadOnlyList<WorkspaceTreeNode> Children { get; init; }

    /// <inheritdoc />
    public override MaterialIconKind IconKind =>
        Source.Kind == WorkspaceSourceKind.Folder ? MaterialIconKind.Folder : MaterialIconKind.FileOutline;
}

/// <summary>
///     An intermediate tree node representing one subfolder discovered under a <see cref="WorkspaceSourceKind.Folder" />
///     source, preserving that subfolder's on-disk position instead of flattening its files up to the source
///     node. Purely a display grouping - it carries no identity used for removal (only whole sources are
///     removable) or file-watching (the source's watch scope already covers everything beneath it).
/// </summary>
public sealed class WorkspaceFolderNode : WorkspaceTreeNode
{
    /// <summary>
    ///     The folder's own name (the last path segment), not its full path.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     This folder's children: nested <see cref="WorkspaceFolderNode" />s for its subfolders, and
    ///     <see cref="WorkspaceFileNode" />s for files directly within it.
    /// </summary>
    public required IReadOnlyList<WorkspaceTreeNode> Children { get; init; }

    /// <inheritdoc />
    public override MaterialIconKind IconKind => MaterialIconKind.Folder;
}

/// <summary>
///     A leaf tree node representing one file contributed by a source. Its <see cref="FilePath" /> is a stable
///     identity intended for a future (out of scope for this feature) double-click read-only file viewer; it must
///     not be erased or aggregated away even though nothing currently reads it beyond display.
/// </summary>
public sealed class WorkspaceFileNode : WorkspaceTreeNode
{
    /// <summary>
    ///     Absolute path of the file this node represents.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    ///     Identifier of the <see cref="WorkspaceSource" /> that owns this file.
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    ///     The file's own name (the last path segment), shown in the tree instead of its full path since
    ///     ancestor <see cref="WorkspaceFolderNode" />/<see cref="WorkspaceSourceNode" /> nodes already convey
    ///     its directory.
    /// </summary>
    public string Name => Path.GetFileName(FilePath);

    /// <inheritdoc />
    public override MaterialIconKind IconKind => MaterialIconKind.FileOutline;
}

/// <summary>
///     Dock tool view model backing the "Workspace" panel. Presents the current set of workspace sources (and,
///     for folder sources, the files discovered under them) as a tree, and forwards Add File/Add Folder/Remove
///     commands to <see cref="MainWindowShell" />. Holds no source-set state itself: the shell is the single
///     owner of workspace sources, and this view model only reads <see cref="MainWindowShell.CurrentWorkspace" />
///     and <see cref="MainWindowShell.CurrentSourceIdToFiles" /> to rebuild its tree.
/// </summary>
public partial class WorkspacePanelToolViewModel : Dock.Model.Mvvm.Controls.Tool
{
    private readonly MainWindowShell _shell;

    [ObservableProperty]
    private IReadOnlyList<WorkspaceTreeNode> _rootNodes = [];

    [ObservableProperty]
    private WorkspaceTreeNode? _selectedNode;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isEmpty = true;

    /// <summary>
    ///     Raised when the user invokes the "Add File..." command, so the Avalonia-aware view can fulfill it with
    ///     a real file picker (this view model has no direct access to Avalonia's <c>StorageProvider</c>) and call
    ///     back into <see cref="MainWindowShell.AddFileSourceAsync" />.
    /// </summary>
    public event EventHandler? RequestAddFile;

    /// <summary>
    ///     Raised when the user invokes the "Add Folder..." command, so the Avalonia-aware view can fulfill it
    ///     with a real folder picker and call back into <see cref="MainWindowShell.AddFolderSourceAsync" />.
    /// </summary>
    public event EventHandler? RequestAddFolder;

    /// <summary>
    ///     Creates the workspace panel tool view model.
    /// </summary>
    /// <param name="shell">Fully composed application shell.</param>
    public WorkspacePanelToolViewModel(MainWindowShell shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _shell.SourcesChanged += (_, _) => RebuildTree();
        RebuildTree();
    }

    /// <summary>
    ///     Shared application shell, exposed so the Avalonia-aware view can call the Add/Remove APIs directly
    ///     once it has resolved a real path from a picker or drag-and-drop drop, without needing its own
    ///     forwarding methods on this view model.
    /// </summary>
    public MainWindowShell Shell => _shell;

    /// <summary>
    ///     Rebuilds <see cref="RootNodes" /> from the shell's current workspace sources and per-source file
    ///     attribution.
    /// </summary>
    public void RebuildTree()
    {
        var sourceIdToFiles = _shell.CurrentSourceIdToFiles;

        RootNodes = _shell.CurrentWorkspace.Sources
            .Select(source =>
            {
                var files = source.Kind == WorkspaceSourceKind.Folder && sourceIdToFiles.TryGetValue(source.Id, out var sourceFiles)
                    ? sourceFiles
                    : [];

                var children = BuildFolderChildren(source, files);

                return (WorkspaceTreeNode)new WorkspaceSourceNode { Id = source.Id, Source = source, Children = children };
            })
            .ToList();

        IsEmpty = RootNodes.Count == 0;
    }

    /// <summary>
    ///     Builds the nested subfolder/file children of a <see cref="WorkspaceSourceKind.Folder" /> source,
    ///     grouping the flat <paramref name="files" /> list (absolute paths) by their position relative to
    ///     <paramref name="source" />'s own path so the tree preserves that folder's on-disk hierarchy instead
    ///     of flattening every discovered file directly under the source node.
    /// </summary>
    /// <param name="source">The source the files were discovered under.</param>
    /// <param name="files">Absolute paths of every file discovered under <paramref name="source" />.</param>
    private static IReadOnlyList<WorkspaceTreeNode> BuildFolderChildren(WorkspaceSource source, IReadOnlyList<string> files)
    {
        if (files.Count == 0)
        {
            return [];
        }

        var root = new FolderGroup(RelativePath: string.Empty);

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(source.Path, file);
            var segments = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

            var current = root;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (!current.Subfolders.TryGetValue(segments[i], out var next))
                {
                    next = new FolderGroup(RelativePath: current.RelativePath.Length == 0 ? segments[i] : $"{current.RelativePath}/{segments[i]}");
                    current.Subfolders[segments[i]] = next;
                }

                current = next;
            }

            current.Files.Add(file);
        }

        return ToTreeNodes(root, source.Id);
    }

    /// <summary>
    ///     Converts a <see cref="FolderGroup" /> built by <see cref="BuildFolderChildren" /> into the immutable
    ///     <see cref="WorkspaceTreeNode" /> shape the tree binds to, listing subfolders before files and sorting
    ///     each alphabetically by name.
    /// </summary>
    private static IReadOnlyList<WorkspaceTreeNode> ToTreeNodes(FolderGroup group, string sourceId)
    {
        var nodes = new List<WorkspaceTreeNode>();

        foreach (var (name, subfolder) in group.Subfolders.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            nodes.Add(new WorkspaceFolderNode
            {
                Id = $"{sourceId}::{subfolder.RelativePath}",
                Name = name,
                Children = ToTreeNodes(subfolder, sourceId),
            });
        }

        foreach (var file in group.Files.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            nodes.Add(new WorkspaceFileNode { Id = $"{sourceId}::{file}", FilePath = file, SourceId = sourceId });
        }

        return nodes;
    }

    /// <summary>
    ///     A mutable, in-progress grouping of one folder level while <see cref="BuildFolderChildren" /> walks a
    ///     source's flat discovered-file list; converted to immutable <see cref="WorkspaceTreeNode" />s by
    ///     <see cref="ToTreeNodes" /> once fully populated. <see cref="RelativePath" /> is this folder's own
    ///     path relative to the source root (empty for the root group itself), kept so descendant node ids stay
    ///     unique even when two different subfolders share the same leaf name (for example <c>a/sub</c> and
    ///     <c>b/sub</c>).
    /// </summary>
    private sealed class FolderGroup(string RelativePath)
    {
        public string RelativePath { get; } = RelativePath;

        public Dictionary<string, FolderGroup> Subfolders { get; } = new(StringComparer.Ordinal);

        public List<string> Files { get; } = [];
    }

    /// <summary>
    ///     Raises <see cref="RequestAddFile" /> so the view can present a file picker.
    /// </summary>
    [RelayCommand]
    private void AddFile()
    {
        RequestAddFile?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Raises <see cref="RequestAddFolder" /> so the view can present a folder picker.
    /// </summary>
    [RelayCommand]
    private void AddFolder()
    {
        RequestAddFolder?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Removes the source owning <see cref="SelectedNode" />, if one is selected.
    /// </summary>
    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        var sourceId = SelectedNode switch
        {
            WorkspaceSourceNode sourceNode => sourceNode.Source.Id,
            WorkspaceFileNode fileNode => fileNode.SourceId,
            _ => null,
        };

        if (sourceId is null)
        {
            return;
        }

        try
        {
            await _shell.RemoveSourceAsync(sourceId).ConfigureAwait(false);
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to remove workspace source: {ex.Message}";
        }
    }
}
