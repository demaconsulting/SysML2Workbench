using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     Thin Avalonia view for the "Workspace" Dock tool panel. All state and orchestration live in
///     <see cref="WorkspacePanelToolViewModel" />, bound as this control's data context by Dock when the panel is
///     realized. This view's only real logic is fulfilling the view model's picker-request events with real
///     Avalonia <c>StorageProvider</c> pickers (the view model itself has no Avalonia dependency) and forwarding
///     drag-and-drop file/folder drops to the view model's shell-backed add commands.
/// </summary>
public partial class WorkspacePanelToolView : UserControl
{
    /// <summary>
    ///     Constructor used both at runtime (Dock's view locator creates this control with no arguments and then
    ///     assigns the corresponding <see cref="WorkspacePanelToolViewModel" /> as its data context) and by the
    ///     Avalonia XAML previewer/designer, which is given a throwaway design-time view model.
    /// </summary>
    public WorkspacePanelToolView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            var shell = DesignTimeShellFactory.Create();
            DataContext = new WorkspacePanelToolViewModel(shell);
        }

        DataContextChanged += OnDataContextChanged;

        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);

        WorkspaceTreeView.DoubleTapped += OnWorkspaceTreeViewDoubleTapped;
    }

    private static readonly FilePickerFileType SysmlFileType = new("SysML v2 files")
    {
        Patterns = ["*.sysml"],
    };

    private WorkspacePanelToolViewModel? ViewModel => DataContext as WorkspacePanelToolViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.RequestAddFile += OnRequestAddFile;
            viewModel.RequestAddFolder += OnRequestAddFolder;
        }
    }

    private async void OnRequestAddFile(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null || ViewModel is not { } viewModel)
        {
            return;
        }

        try
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Add Workspace File",
                AllowMultiple = false,
                FileTypeFilter = [SysmlFileType],
            });

            var filePath = files.Count > 0 ? files[0].TryGetLocalPath() : null;
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            await viewModel.Shell.AddFileSourceAsync(filePath);
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Failed to add workspace file: {ex.Message}";
        }
    }

    private async void OnRequestAddFolder(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null || ViewModel is not { } viewModel)
        {
            return;
        }

        try
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Add Workspace Folder",
                AllowMultiple = false,
            });

            var folderPath = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            await viewModel.Shell.AddFolderSourceAsync(folderPath);
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Failed to add workspace folder: {ex.Message}";
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (ViewModel is not { } viewModel || !e.DataTransfer.Formats.Contains(DataFormat.File))
        {
            return;
        }

        foreach (var item in e.DataTransfer.TryGetFiles() ?? [])
        {
            var path = item.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            try
            {
                if (Directory.Exists(path))
                {
                    await viewModel.Shell.AddFolderSourceAsync(path);
                }
                else if (File.Exists(path))
                {
                    await viewModel.Shell.AddFileSourceAsync(path);
                }
            }
            catch (Exception ex)
            {
                viewModel.StatusMessage = $"Failed to add dropped workspace source '{path}': {ex.Message}";
            }
        }
    }

    /// <summary>
    ///     Handles a double-tap anywhere on <c>WorkspaceTreeView</c> by opening a read-only source-text tab for
    ///     the currently selected node, if it is a <see cref="WorkspaceFileNode" /> whose <see cref="WorkspaceFileNode.FilePath" />
    ///     ends in <c>.sysml</c>. Avalonia's <see cref="TreeView" /> updates <see cref="TreeView.SelectedItem" />
    ///     on the same click that raises <c>DoubleTapped</c>, so no custom hit-testing of the
    ///     event's source is needed. A double-tap on any other node kind (a source or folder node), or a
    ///     non-<c>.sysml</c> file, is a safe no-op.
    /// </summary>
    private void OnWorkspaceTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        if (viewModel.SelectedNode is WorkspaceFileNode { FilePath: { } filePath } && filePath.EndsWith(".sysml", StringComparison.OrdinalIgnoreCase))
        {
            viewModel.Shell.OpenSourceTextTab(filePath);
        }
    }
}

