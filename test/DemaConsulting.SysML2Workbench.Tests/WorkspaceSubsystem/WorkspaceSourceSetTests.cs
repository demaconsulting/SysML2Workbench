using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.WorkspaceSubsystem;

/// <summary>
///     Unit tests for <see cref="WorkspaceSourceSet" />.
/// </summary>
public sealed class WorkspaceSourceSetTests : IDisposable
{
    /// <summary>
    ///     Temporary root folder created fresh for each test and removed on disposal.
    /// </summary>
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-source-set-tests-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static Task WriteFileAsync(string path, string content)
    {
        return File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
    }

    /// <summary>
    ///     Validates that a freshly constructed source set has no sources and resolves to an empty result with
    ///     no error.
    /// </summary>
    [Fact]
    public void Resolve_ZeroSources_ReturnsEmptyResolution()
    {
        // Arrange
        var sourceSet = new WorkspaceSourceSet();

        // Act
        var resolution = sourceSet.Resolve();

        // Assert
        Assert.Empty(sourceSet.Sources);
        Assert.Empty(resolution.MergedFiles);
        Assert.Empty(resolution.FileToSourceId);
        Assert.Empty(resolution.SourceIdToFiles);
    }

    /// <summary>
    ///     Validates that adding a file source is idempotent: adding the exact same path twice returns the same
    ///     source and does not create a duplicate entry.
    /// </summary>
    [Fact]
    public async Task AddFile_SamePathTwice_ReturnsSameSourceAndDoesNotDuplicate()
    {
        // Arrange
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await WriteFileAsync(filePath, "package Sample {\n    part def Widget;\n}\n");
        var sourceSet = new WorkspaceSourceSet();

        // Act
        var first = sourceSet.AddFile(filePath);
        var second = sourceSet.AddFile(filePath);

        // Assert
        Assert.Equal(first.Id, second.Id);
        Assert.Single(sourceSet.Sources);
    }

    /// <summary>
    ///     Validates that adding a folder source is idempotent: adding the exact same path twice returns the same
    ///     source and does not create a duplicate entry.
    /// </summary>
    [Fact]
    public void AddFolder_SamePathTwice_ReturnsSameSourceAndDoesNotDuplicate()
    {
        // Arrange
        var sourceSet = new WorkspaceSourceSet();

        // Act
        var first = sourceSet.AddFolder(_tempRoot);
        var second = sourceSet.AddFolder(_tempRoot);

        // Assert
        Assert.Equal(first.Id, second.Id);
        Assert.Single(sourceSet.Sources);
    }

    /// <summary>
    ///     Validates that <see cref="WorkspaceSourceSet.AddFolder" /> throws a clear, fail-fast error for a
    ///     folder that does not exist, rather than silently registering a source that will always resolve to
    ///     zero files.
    /// </summary>
    [Fact]
    public void AddFolder_MissingFolder_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var missingPath = Path.Combine(_tempRoot, "does-not-exist");
        var sourceSet = new WorkspaceSourceSet();

        // Act / Assert
        Assert.Throws<DirectoryNotFoundException>(() => sourceSet.AddFolder(missingPath));
    }

    /// <summary>
    ///     Validates that <see cref="WorkspaceSourceSet.RemoveSource" /> removes a registered source and returns
    ///     <see langword="true" />, and returns <see langword="false" /> for an unknown identifier.
    /// </summary>
    [Fact]
    public void RemoveSource_RegisteredThenUnknownId_ReturnsTrueThenFalse()
    {
        // Arrange
        var sourceSet = new WorkspaceSourceSet();
        var source = sourceSet.AddFolder(_tempRoot);

        // Act
        var removed = sourceSet.RemoveSource(source.Id);
        var removedAgain = sourceSet.RemoveSource(source.Id);

        // Assert
        Assert.True(removed);
        Assert.False(removedAgain);
        Assert.Empty(sourceSet.Sources);
    }

    /// <summary>
    ///     Validates that resolving a folder source discovers every <c>.sysml</c> file under it, recursively.
    /// </summary>
    [Fact]
    public async Task Resolve_FolderSource_DiscoversAllSysmlFilesRecursively()
    {
        // Arrange
        var nestedDir = Path.Combine(_tempRoot, "nested");
        Directory.CreateDirectory(nestedDir);
        await WriteFileAsync(Path.Combine(_tempRoot, "Top.sysml"), "package Top {\n    part def Widget;\n}\n");
        await WriteFileAsync(Path.Combine(nestedDir, "Nested.sysml"), "package Nested {\n    part def Gadget;\n}\n");
        var sourceSet = new WorkspaceSourceSet();
        var folderSource = sourceSet.AddFolder(_tempRoot);

        // Act
        var resolution = sourceSet.Resolve();

        // Assert
        Assert.Equal(2, resolution.MergedFiles.Count);
        Assert.Equal(2, resolution.SourceIdToFiles[folderSource.Id].Count);
    }

    /// <summary>
    ///     Validates that a file explicitly added and also discovered under an overlapping folder source is
    ///     deduplicated to a single entry in <see cref="WorkspaceSourceResolution.MergedFiles" />, and that the
    ///     first-registered source wins attribution in <see cref="WorkspaceSourceResolution.FileToSourceId" />.
    /// </summary>
    [Fact]
    public async Task Resolve_FileOverlappingFolder_DedupesAndFirstRegisteredSourceWinsAttribution()
    {
        // Arrange: the file source is registered first, then a folder source that also discovers that file
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await WriteFileAsync(filePath, "package Sample {\n    part def Widget;\n}\n");
        var sourceSet = new WorkspaceSourceSet();
        var fileSource = sourceSet.AddFile(filePath);
        var folderSource = sourceSet.AddFolder(_tempRoot);

        // Act
        var resolution = sourceSet.Resolve();

        // Assert: deduped to one entry, attributed to the first-registered (file) source
        Assert.Single(resolution.MergedFiles);
        Assert.Equal(fileSource.Id, resolution.FileToSourceId[resolution.MergedFiles[0]]);
        // The folder source's own per-source file list still independently lists the file it discovered.
        Assert.Single(resolution.SourceIdToFiles[folderSource.Id]);
    }

    /// <summary>
    ///     Validates that two overlapping folder sources (one nested inside the other) dedupe their shared files
    ///     to a single merged entry, attributed to the first-registered folder.
    /// </summary>
    [Fact]
    public async Task Resolve_NestedFolderOverlap_DedupesAndFirstRegisteredSourceWinsAttribution()
    {
        // Arrange: an outer folder registered first, and an inner (nested) folder registered second, both
        // discovering the same nested file.
        var nestedDir = Path.Combine(_tempRoot, "nested");
        Directory.CreateDirectory(nestedDir);
        var nestedFile = Path.Combine(nestedDir, "Nested.sysml");
        await WriteFileAsync(nestedFile, "package Nested {\n    part def Gadget;\n}\n");
        var sourceSet = new WorkspaceSourceSet();
        var outerSource = sourceSet.AddFolder(_tempRoot);
        sourceSet.AddFolder(nestedDir);

        // Act
        var resolution = sourceSet.Resolve();

        // Assert: the nested file appears once, attributed to the first-registered (outer) folder
        Assert.Single(resolution.MergedFiles);
        Assert.Equal(outerSource.Id, resolution.FileToSourceId[resolution.MergedFiles[0]]);
    }

    /// <summary>
    ///     Validates that <see cref="WorkspaceSourceSet.AddFolder" /> corrects a differently-cased path to its
    ///     actual on-disk casing, and that the corrected casing is what makes adding the same folder again
    ///     (via yet another differently-cased path) idempotent rather than registering a duplicate source -
    ///     the underlying scenario is two case-distinct paths to the very same on-disk folder, which routinely
    ///     happens on case-insensitive filesystems (Windows, default macOS) when a path is typed, drag-dropped,
    ///     or picked with different casing than the real directory entry.
    /// </summary>
    [Fact]
    public void AddFolder_DifferentlyCasedPath_NormalizesToOnDiskCasingAndIsIdempotent()
    {
        // Arrange
        var actualName = Path.GetFileName(_tempRoot);
        var upperCasedPath = Path.Combine(Path.GetDirectoryName(_tempRoot)!, actualName.ToUpperInvariant());
        if (!Directory.Exists(upperCasedPath))
        {
            // The host filesystem is case-sensitive (for example Linux ext4), so the differently-cased path
            // genuinely does not resolve to the same directory - the normalization behavior under test only
            // applies on case-insensitive filesystems (Windows, default macOS).
            Assert.Skip("Host filesystem is case-sensitive; differently-cased path does not alias the same folder.");
        }

        var sourceSet = new WorkspaceSourceSet();

        // Act
        var first = sourceSet.AddFolder(_tempRoot);
        var second = sourceSet.AddFolder(upperCasedPath);

        // Assert: both additions resolve to the same source, and its Path reflects the real on-disk casing
        Assert.Equal(first.Id, second.Id);
        Assert.Single(sourceSet.Sources);
        Assert.Equal(_tempRoot, first.Path, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Validates that <see cref="WorkspaceSourceSet.Sources" /> preserves registration order across mixed
    ///     file and folder additions.
    /// </summary>
    [Fact]
    public async Task Sources_PreservesRegistrationOrder()
    {
        // Arrange
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await WriteFileAsync(filePath, "package Sample {\n    part def Widget;\n}\n");
        var otherDir = Directory.CreateTempSubdirectory("sysml2workbench-source-set-tests-other-").FullName;
        try
        {
            var sourceSet = new WorkspaceSourceSet();

            // Act
            var folderSource = sourceSet.AddFolder(_tempRoot);
            var fileSource = sourceSet.AddFile(filePath);
            var secondFolderSource = sourceSet.AddFolder(otherDir);

            // Assert
            Assert.Equal([folderSource, fileSource, secondFolderSource], sourceSet.Sources);
        }
        finally
        {
            if (Directory.Exists(otherDir))
            {
                Directory.Delete(otherDir, recursive: true);
            }
        }
    }
}
