using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

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
}

/// <summary>
///     A tree node representing one <see cref="WorkspaceSource" />. Has no children for a
///     <see cref="WorkspaceSourceKind.File" /> source (a leaf, with no expand arrow); has one
///     <see cref="WorkspaceFileNode" /> child per discovered file for a <see cref="WorkspaceSourceKind.Folder" />
///     source.
/// </summary>
public sealed class WorkspaceSourceNode : WorkspaceTreeNode
{
    /// <summary>
    ///     The source this node represents.
    /// </summary>
    public required WorkspaceSource Source { get; init; }

    /// <summary>
    ///     Files contributed by this source. Empty for a <see cref="WorkspaceSourceKind.File" /> source.
    /// </summary>
    public required IReadOnlyList<WorkspaceFileNode> Children { get; init; }
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

                var children = files
                    .Select(file => new WorkspaceFileNode { Id = $"{source.Id}::{file}", FilePath = file, SourceId = source.Id })
                    .ToList();

                return (WorkspaceTreeNode)new WorkspaceSourceNode { Id = source.Id, Source = source, Children = children };
            })
            .ToList();

        IsEmpty = RootNodes.Count == 0;
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
