using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.WorkspaceSubsystem;

/// <summary>
///     Unit tests for <see cref="WorkspaceModel" />.
/// </summary>
public sealed class WorkspaceModelTests : IDisposable
{
    /// <summary>
    ///     Temporary workspace root folder created fresh for each test and removed on disposal.
    /// </summary>
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-tests-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Writes a SysML source file, honoring the ambient test cancellation token.
    /// </summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="content">File content to write.</param>
    private static Task WriteFileAsync(string path, string content)
    {
        return File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Loads a single folder source into a fresh <see cref="WorkspaceSourceSet" /> and returns both the
    ///     sources and the resolution, for tests that only care about the resulting <see cref="WorkspaceModel" />
    ///     state rather than the source-set mechanics themselves.
    /// </summary>
    private static (IReadOnlyList<WorkspaceSource> Sources, WorkspaceSourceResolution Resolution) ResolveFolder(string folderPath)
    {
        var sourceSet = new WorkspaceSourceSet();
        sourceSet.AddFolder(folderPath);
        return (sourceSet.Sources, sourceSet.Resolve());
    }

    /// <summary>
    ///     Validates that loading a workspace folder builds a tracked file tree covering every discovered file.
    /// </summary>
    [Fact]
    public async Task LoadWorkspaceAsync_BuildsTrackedFileTree()
    {
        // Arrange: a workspace folder containing a single valid SysML file
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await WriteFileAsync(filePath, "package Sample {\n    part def Widget;\n}\n");
        var model = new WorkspaceModel();
        var (sources, resolution) = ResolveFolder(_tempRoot);

        // Act: load the workspace
        var snapshot = await model.LoadWorkspaceAsync(sources, resolution);

        // Assert: the tracked file tree reflects the discovered file and the resolved sources
        Assert.Single(model.Sources);
        Assert.Single(model.Files);
        Assert.True(model.Files.ContainsKey(filePath));
        Assert.Single(snapshot.Files);
        Assert.Single(snapshot.Sources);
    }

    /// <summary>
    ///     Validates that resolving workspace inputs combines glob-discovered files with SysML import
    ///     resolution across those discovered files.
    /// </summary>
    [Fact]
    public async Task LoadWorkspaceAsync_FindsDiscoveredAndImportedFiles()
    {
        // Arrange: two files where the second imports a definition declared in the first
        await WriteFileAsync(
            Path.Combine(_tempRoot, "A.sysml"),
            "package PackageA {\n    part def Widget;\n}\n");
        await WriteFileAsync(
            Path.Combine(_tempRoot, "B.sysml"),
            "package PackageB {\n    import PackageA::*;\n    part myWidget : Widget;\n}\n");
        var model = new WorkspaceModel();
        var (sources, resolution) = ResolveFolder(_tempRoot);

        // Act: load the workspace
        var snapshot = await model.LoadWorkspaceAsync(sources, resolution);

        // Assert: both files were discovered and the cross-file import resolved without errors
        Assert.Equal(2, snapshot.Files.Count);
        Assert.DoesNotContain(snapshot.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.True(snapshot.Workspace.Declarations.ContainsKey("PackageA::Widget"));
        Assert.True(snapshot.Workspace.Declarations.ContainsKey("PackageB::myWidget"));
    }

    /// <summary>
    ///     Validates that reloading the workspace only replaces the per-file state of files whose diagnostics
    ///     actually changed, leaving unaffected file state instances untouched.
    /// </summary>
    [Fact]
    public async Task ReloadFile_UpdatesOnlyAffectedFileState()
    {
        // Arrange: load a workspace with one always-valid file and one file that will be broken later
        var unaffectedPath = Path.Combine(_tempRoot, "Unaffected.sysml");
        var affectedPath = Path.Combine(_tempRoot, "Affected.sysml");
        await WriteFileAsync(unaffectedPath, "package Unaffected {\n    part def Widget;\n}\n");
        await WriteFileAsync(affectedPath, "package Affected {\n    part def Gadget;\n}\n");
        var model = new WorkspaceModel();
        var (sources, resolution) = ResolveFolder(_tempRoot);
        await model.LoadWorkspaceAsync(sources, resolution);
        var unaffectedBefore = model.Files[unaffectedPath];
        var affectedBefore = model.Files[affectedPath];

        // Act: introduce an unresolved reference into only the "affected" file, then reload
        await WriteFileAsync(affectedPath, "package Affected {\n    part def Gadget : DoesNotExist;\n}\n");
        var snapshot = await model.ReloadFilesAsync([affectedPath]);

        // Assert: only the affected file's tracked state instance was replaced
        Assert.Same(unaffectedBefore, model.Files[unaffectedPath]);
        Assert.NotSame(affectedBefore, model.Files[affectedPath]);
        Assert.NotEmpty(model.Files[affectedPath].Diagnostics);
        Assert.NotNull(snapshot);
    }

    /// <summary>
    ///     Validates that a zero-source, zero-file resolution is first-class: it produces a valid,
    ///     diagnostic-free, standard-library-only snapshot rather than throwing.
    /// </summary>
    [Fact]
    public async Task LoadWorkspaceAsync_EmptyResolution_ProducesValidStdlibOnlySnapshot()
    {
        // Arrange: an empty source set, resolved to zero sources and zero files
        var model = new WorkspaceModel();
        var sourceSet = new WorkspaceSourceSet();
        var resolution = sourceSet.Resolve();

        // Act: load the empty resolution
        var snapshot = await model.LoadWorkspaceAsync(sourceSet.Sources, resolution);

        // Assert: a valid, non-throwing, diagnostic-free snapshot with no sources and no files
        Assert.Empty(snapshot.Sources);
        Assert.Empty(snapshot.Files);
        Assert.Empty(snapshot.Diagnostics);
        Assert.NotNull(snapshot.Workspace);
    }

    /// <summary>
    ///     Validates that reloading against a current resolution that has since become empty (0 sources, 0
    ///     files) is a valid, non-throwing no-op recomputation rather than an error.
    /// </summary>
    [Fact]
    public async Task ReloadFilesAsync_AfterResolutionBecomesEmpty_ProducesValidEmptySnapshot()
    {
        // Arrange: load a non-empty workspace, then reset it to an empty resolution (mirrors what
        // MainWindowShell does after removing the last source).
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await WriteFileAsync(filePath, "package Sample {\n    part def Widget;\n}\n");
        var model = new WorkspaceModel();
        var (sources, resolution) = ResolveFolder(_tempRoot);
        await model.LoadWorkspaceAsync(sources, resolution);

        var emptySourceSet = new WorkspaceSourceSet();
        await model.LoadWorkspaceAsync(emptySourceSet.Sources, emptySourceSet.Resolve());

        // Act: reload against the now-empty resolution
        var snapshot = await model.ReloadFilesAsync([]);

        // Assert: a valid, empty snapshot, and no lingering per-file state
        Assert.Empty(snapshot.Files);
        Assert.Empty(model.Files);
    }

    /// <summary>
    ///     Validates that requesting the semantic workspace before any load throws instead of returning a
    ///     misleading default.
    /// </summary>
    [Fact]
    public void GetSemanticWorkspace_BeforeLoad_ThrowsInvalidOperationException()
    {
        // Arrange: a model that has never loaded a workspace
        var model = new WorkspaceModel();

        // Act / Assert: retrieval throws rather than returning null or a stale workspace
        Assert.Throws<InvalidOperationException>(() => model.GetSemanticWorkspace());
    }
}
