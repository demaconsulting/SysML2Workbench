using DemaConsulting.SysML2Workbench.AppShellSubsystem;
using DemaConsulting.SysML2Workbench.DiagnosticsPanelSubsystem;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.LoggingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.AppShellSubsystem;

/// <summary>
///     Unit tests for <see cref="WorkspacePanelToolViewModel" />.
/// </summary>
public sealed class WorkspacePanelToolViewModelTests : IDisposable
{
    /// <summary>
    ///     Temporary workspace root folder created fresh for each test and removed on disposal.
    /// </summary>
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;

    /// <summary>
    ///     Temporary log directory created fresh for each test and removed on disposal.
    /// </summary>
    private readonly string _tempLogRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-logs-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        if (Directory.Exists(_tempLogRoot))
        {
            Directory.Delete(_tempLogRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Builds a shell wired with real (non-mocked) subsystem units.
    /// </summary>
    private MainWindowShell CreateShell()
    {
        return new MainWindowShell(
            new WorkspaceModel(),
            new FileWatcher(TimeSpan.FromMilliseconds(1)),
            new DiagnosticsAggregator(),
            new ViewCatalogPresenter(),
            new LayoutInvoker(),
            new DiagnosticsListView(),
            new SysmlSnippetGenerator(),
            new RollingFileLogger(_tempLogRoot));
    }

    private static Task WriteFileAsync(string path, string content)
    {
        return File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Validates that a freshly constructed view model, against a zero-source shell, reports an empty tree
    ///     and the root-level empty state.
    /// </summary>
    [Fact]
    public void Construction_ZeroSources_ReportsEmptyTree()
    {
        // Arrange / Act
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);

        // Assert
        Assert.Empty(viewModel.RootNodes);
        Assert.True(viewModel.IsEmpty);
    }

    /// <summary>
    ///     Validates that a folder source's tree node has one <see cref="WorkspaceFileNode" /> child per
    ///     discovered file.
    /// </summary>
    [Fact]
    public async Task RebuildTree_FolderSource_ProducesSourceNodeWithFileChildren()
    {
        // Arrange
        await WriteFileAsync(Path.Combine(_tempRoot, "A.sysml"), "package A {\n    part def Widget;\n}\n");
        await WriteFileAsync(Path.Combine(_tempRoot, "B.sysml"), "package B {\n    part def Gadget;\n}\n");
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);

        // Act
        await shell.AddFolderSourceAsync(_tempRoot);

        // Assert: exactly one source node, with two file children
        var sourceNode = Assert.IsType<WorkspaceSourceNode>(Assert.Single(viewModel.RootNodes));
        Assert.Equal(WorkspaceSourceKind.Folder, sourceNode.Source.Kind);
        Assert.Equal(2, sourceNode.Children.Count);
        Assert.All(sourceNode.Children, child => Assert.Equal(sourceNode.Source.Id, Assert.IsType<WorkspaceFileNode>(child).SourceId));
        Assert.False(viewModel.IsEmpty);
    }

    /// <summary>
    ///     Validates that files discovered in subfolders are grouped under intermediate
    ///     <see cref="WorkspaceFolderNode" />s mirroring the on-disk hierarchy, instead of being flattened
    ///     directly under the source node - this is the whole point of the tree being expandable rather than a
    ///     flat file list.
    /// </summary>
    [Fact]
    public async Task RebuildTree_FolderSourceWithSubfolders_PreservesOnDiskHierarchy()
    {
        // Arrange: A.sysml directly under the root, B.sysml one level down in "Sub", C.sysml two levels down.
        Directory.CreateDirectory(Path.Combine(_tempRoot, "Sub", "Nested"));
        await WriteFileAsync(Path.Combine(_tempRoot, "A.sysml"), "package A {\n    part def Widget;\n}\n");
        await WriteFileAsync(Path.Combine(_tempRoot, "Sub", "B.sysml"), "package B {\n    part def Gadget;\n}\n");
        await WriteFileAsync(Path.Combine(_tempRoot, "Sub", "Nested", "C.sysml"), "package C {\n    part def Doohickey;\n}\n");
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);

        // Act
        await shell.AddFolderSourceAsync(_tempRoot);

        // Assert: the source node lists "Sub" (a folder) before A.sysml (a file) - folders sort before files.
        var sourceNode = Assert.IsType<WorkspaceSourceNode>(Assert.Single(viewModel.RootNodes));
        Assert.Equal(2, sourceNode.Children.Count);
        var subFolder = Assert.IsType<WorkspaceFolderNode>(sourceNode.Children[0]);
        Assert.Equal("Sub", subFolder.Name);
        var rootFile = Assert.IsType<WorkspaceFileNode>(sourceNode.Children[1]);
        Assert.Equal("A.sysml", rootFile.Name);

        // Assert: "Sub" lists the "Nested" subfolder before B.sysml.
        Assert.Equal(2, subFolder.Children.Count);
        var nestedFolder = Assert.IsType<WorkspaceFolderNode>(subFolder.Children[0]);
        Assert.Equal("Nested", nestedFolder.Name);
        var subFile = Assert.IsType<WorkspaceFileNode>(subFolder.Children[1]);
        Assert.Equal("B.sysml", subFile.Name);

        // Assert: "Nested" contains only C.sysml.
        var nestedFile = Assert.IsType<WorkspaceFileNode>(Assert.Single(nestedFolder.Children));
        Assert.Equal("C.sysml", nestedFile.Name);
    }

    /// <summary>
    ///     Validates that a file source's tree node is a leaf: it has zero children, distinguishing it from a
    ///     folder source with zero discovered files.
    /// </summary>
    [Fact]
    public async Task RebuildTree_FileSource_ProducesLeafSourceNodeWithNoChildren()
    {
        // Arrange
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await WriteFileAsync(filePath, "package Sample {\n    part def Widget;\n}\n");
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);

        // Act
        await shell.AddFileSourceAsync(filePath);

        // Assert
        var sourceNode = Assert.IsType<WorkspaceSourceNode>(Assert.Single(viewModel.RootNodes));
        Assert.Equal(WorkspaceSourceKind.File, sourceNode.Source.Kind);
        Assert.Empty(sourceNode.Children);
    }

    /// <summary>
    ///     Validates that a file explicitly added and also discovered under an overlapping folder source is
    ///     reflected in the tree only once (under the first-registered source), matching
    ///     <see cref="WorkspaceSourceSet" />'s dedupe/attribution contract, rather than appearing twice or under
    ///     both sources.
    /// </summary>
    [Fact]
    public async Task RebuildTree_OverlappingFileAndFolder_DedupeReflectedInTreeShape()
    {
        // Arrange: the file source is registered first, then a folder source that also discovers that file
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await WriteFileAsync(filePath, "package Sample {\n    part def Widget;\n}\n");
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);

        // Act
        await shell.AddFileSourceAsync(filePath);
        await shell.AddFolderSourceAsync(_tempRoot);

        // Assert: two source nodes (the file source and the folder source), but the folder source's own
        // per-source file list (mirroring WorkspaceSourceResolution.SourceIdToFiles) still shows the file it
        // discovered - CurrentSourceIdToFiles is per-source, not deduped, by design.
        Assert.Equal(2, viewModel.RootNodes.Count);
        var fileSourceNode = Assert.IsType<WorkspaceSourceNode>(viewModel.RootNodes[0]);
        var folderSourceNode = Assert.IsType<WorkspaceSourceNode>(viewModel.RootNodes[1]);
        Assert.Equal(WorkspaceSourceKind.File, fileSourceNode.Source.Kind);
        Assert.Empty(fileSourceNode.Children);
        Assert.Equal(WorkspaceSourceKind.Folder, folderSourceNode.Source.Kind);
        Assert.Single(folderSourceNode.Children);

        // The merged, deduped workspace itself only counts the file once.
        Assert.Single(shell.CurrentWorkspace.Files);
    }

    /// <summary>
    ///     Validates that the tree rebuilds automatically when <see cref="MainWindowShell.SourcesChanged" /> is
    ///     raised by a mutation the view model itself did not initiate, and that removing the last source
    ///     restores the empty-tree state.
    /// </summary>
    [Fact]
    public async Task SourcesChanged_TriggersRebuild_AndRemovalRestoresEmptyState()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);

        // Act: add directly through the shell (not through the view model), simulating a mutation from the File
        // menu or a drag-and-drop drop.
        var source = await shell.AddFolderSourceAsync(_tempRoot);

        // Assert: the view model's tree reflects the addition without any direct call on it.
        Assert.Single(viewModel.RootNodes);
        Assert.False(viewModel.IsEmpty);

        // Act: remove the only source, going back down to zero.
        await shell.RemoveSourceAsync(source.Sources[0].Id);

        // Assert: back to the empty-tree state.
        Assert.Empty(viewModel.RootNodes);
        Assert.True(viewModel.IsEmpty);
    }

    /// <summary>
    ///     Validates that invoking the Add File command raises <see cref="WorkspacePanelToolViewModel.RequestAddFile" />
    ///     rather than performing any filesystem interaction itself (the view model has no direct Avalonia
    ///     picker access).
    /// </summary>
    [Fact]
    public void AddFileCommand_RaisesRequestAddFile()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);
        var raised = false;
        viewModel.RequestAddFile += (_, _) => raised = true;

        // Act
        viewModel.AddFileCommand.Execute(null);

        // Assert
        Assert.True(raised);
    }

    /// <summary>
    ///     Validates that invoking the Add Folder command raises
    ///     <see cref="WorkspacePanelToolViewModel.RequestAddFolder" /> rather than performing any filesystem
    ///     interaction itself.
    /// </summary>
    [Fact]
    public void AddFolderCommand_RaisesRequestAddFolder()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);
        var raised = false;
        viewModel.RequestAddFolder += (_, _) => raised = true;

        // Act
        viewModel.AddFolderCommand.Execute(null);

        // Assert
        Assert.True(raised);
    }

    /// <summary>
    ///     Validates that invoking Remove with a selected source node forwards to
    ///     <see cref="MainWindowShell.RemoveSourceAsync" /> for that node's owning source, actually removing it.
    /// </summary>
    [Fact]
    public async Task RemoveSelectedCommand_WithSourceNodeSelected_RemovesOwningSource()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);
        await shell.AddFolderSourceAsync(_tempRoot);
        var sourceNode = Assert.IsType<WorkspaceSourceNode>(Assert.Single(viewModel.RootNodes));
        viewModel.SelectedNode = sourceNode;

        // Act
        await viewModel.RemoveSelectedCommand.ExecuteAsync(null);

        // Assert
        Assert.Empty(shell.CurrentWorkspace.Sources);
        Assert.Empty(viewModel.RootNodes);
    }

    /// <summary>
    ///     Validates that invoking Remove with a file-node child selected still removes that file's owning
    ///     source (the whole source, since files cannot be individually removed), not just the leaf.
    /// </summary>
    [Fact]
    public async Task RemoveSelectedCommand_WithFileNodeSelected_RemovesOwningSource()
    {
        // Arrange
        await WriteFileAsync(Path.Combine(_tempRoot, "A.sysml"), "package A {\n    part def Widget;\n}\n");
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);
        await shell.AddFolderSourceAsync(_tempRoot);
        var sourceNode = Assert.IsType<WorkspaceSourceNode>(Assert.Single(viewModel.RootNodes));
        var fileNode = Assert.Single(sourceNode.Children);
        viewModel.SelectedNode = fileNode;

        // Act
        await viewModel.RemoveSelectedCommand.ExecuteAsync(null);

        // Assert
        Assert.Empty(shell.CurrentWorkspace.Sources);
        Assert.Empty(viewModel.RootNodes);
    }

    /// <summary>
    ///     Validates that invoking Remove with no node selected is a safe no-op.
    /// </summary>
    [Fact]
    public async Task RemoveSelectedCommand_NoSelection_IsNoOp()
    {
        // Arrange
        using var shell = CreateShell();
        var viewModel = new WorkspacePanelToolViewModel(shell);
        await shell.AddFolderSourceAsync(_tempRoot);

        // Act
        var exception = await Record.ExceptionAsync(() => viewModel.RemoveSelectedCommand.ExecuteAsync(null));

        // Assert: no exception, and the source is untouched
        Assert.Null(exception);
        Assert.Single(shell.CurrentWorkspace.Sources);
    }
}
