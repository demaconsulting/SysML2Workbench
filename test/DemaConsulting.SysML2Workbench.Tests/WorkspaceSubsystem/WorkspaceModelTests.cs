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
    ///     Validates that loading a workspace folder builds a tracked file tree covering every discovered file.
    /// </summary>
    [Fact]
    public async Task OpenWorkspace_BuildsTrackedFileTree()
    {
        // Arrange: a workspace folder containing a single valid SysML file
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await WriteFileAsync(filePath, "package Sample {\n    part def Widget;\n}\n");
        var model = new WorkspaceModel();

        // Act: load the workspace
        var snapshot = await model.LoadWorkspaceAsync(_tempRoot);

        // Assert: the tracked file tree reflects the discovered file and the workspace root
        Assert.Equal(Path.GetFullPath(_tempRoot), model.RootPath);
        Assert.Single(model.Files);
        Assert.True(model.Files.ContainsKey(filePath));
        Assert.Single(snapshot.Files);
    }

    /// <summary>
    ///     Validates that resolving workspace inputs combines glob-discovered files with SysML import
    ///     resolution across those discovered files.
    /// </summary>
    [Fact]
    public async Task ResolveInputs_FindsDiscoveredAndImportedFiles()
    {
        // Arrange: two files where the second imports a definition declared in the first
        await WriteFileAsync(
            Path.Combine(_tempRoot, "A.sysml"),
            "package PackageA {\n    part def Widget;\n}\n");
        await WriteFileAsync(
            Path.Combine(_tempRoot, "B.sysml"),
            "package PackageB {\n    import PackageA::*;\n    part myWidget : Widget;\n}\n");
        var model = new WorkspaceModel();

        // Act: load the workspace
        var snapshot = await model.LoadWorkspaceAsync(_tempRoot);

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
        await model.LoadWorkspaceAsync(_tempRoot);
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
    ///     Validates that loading from a folder that does not exist reports a clear, propagated failure rather
    ///     than an empty workspace.
    /// </summary>
    [Fact]
    public async Task LoadWorkspaceAsync_MissingRootFolder_ThrowsDirectoryNotFoundException()
    {
        // Arrange: a path that does not exist on disk
        var missingPath = Path.Combine(_tempRoot, "does-not-exist");
        var model = new WorkspaceModel();

        // Act / Assert: loading throws instead of silently producing an empty workspace
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => model.LoadWorkspaceAsync(missingPath));
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
