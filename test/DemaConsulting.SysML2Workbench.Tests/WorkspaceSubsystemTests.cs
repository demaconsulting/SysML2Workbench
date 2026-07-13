using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     Subsystem-level tests exercising WorkspaceSubsystem's units (<see cref="WorkspaceModel" />,
///     <see cref="FileWatcher" />, <see cref="DiagnosticsAggregator" />) together, per
///     docs/reqstream/sysml2-workbench/workspace-subsystem.yaml.
/// </summary>
public sealed class WorkspaceSubsystemTests : IDisposable
{
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
    ///     Validates that opening a workspace builds a complete, discoverable workspace state: the file tree,
    ///     semantic workspace, and diagnostics are all populated together.
    /// </summary>
    [Fact]
    public async Task OpenWorkspace_BuildsWorkspaceState()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n    part def Engine;\n}\n",
            TestContext.Current.CancellationToken);
        var model = new WorkspaceModel();

        // Act
        var snapshot = await model.LoadWorkspaceAsync(_tempRoot);

        // Assert: the file tree, semantic workspace, and diagnostics are all populated from the same load pass
        Assert.Single(snapshot.Files);
        Assert.True(snapshot.Workspace.Declarations.ContainsKey("Sample::Engine"));
        Assert.Empty(snapshot.Diagnostics);
    }

    /// <summary>
    ///     Validates that an external file change detected by <see cref="FileWatcher" /> results in
    ///     <see cref="WorkspaceModel" /> refreshing its state when the reload pipeline is driven from the
    ///     watcher's flushed change set.
    /// </summary>
    [Fact]
    public async Task ExternalChange_RefreshesWorkspaceState()
    {
        // Arrange: an initially loaded, watched workspace
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n    part def Engine;\n}\n",
            TestContext.Current.CancellationToken);
        var model = new WorkspaceModel();
        await model.LoadWorkspaceAsync(_tempRoot);

        var now = DateTimeOffset.UtcNow;
        var watcher = new FileWatcher(TimeSpan.FromMilliseconds(1), () => now);
        watcher.StartWatching(_tempRoot);

        // Act: an external process adds a new file, the watcher observes it, and the debounce window elapses
        var newFilePath = Path.Combine(_tempRoot, "Extra.sysml");
        await File.WriteAllTextAsync(newFilePath, "package Extra {\n    part def Bracket;\n}\n", TestContext.Current.CancellationToken);
        watcher.QueueChange(newFilePath);
        now = now.AddSeconds(1);
        var changed = watcher.FlushPendingChanges();
        var refreshed = await model.ReloadFilesAsync(changed);

        // Assert: the refreshed workspace state now includes the new file
        Assert.Equal(2, refreshed.Files.Count);
        Assert.True(refreshed.Workspace.Declarations.ContainsKey("Extra::Bracket"));

        watcher.Dispose();
    }

    /// <summary>
    ///     Validates that opening a workspace produces one unified, deterministically ordered diagnostic view
    ///     spanning every file, via <see cref="DiagnosticsAggregator" />.
    /// </summary>
    [Fact]
    public async Task OpenWorkspace_ProducesUnifiedDiagnostics()
    {
        // Arrange: two files, one containing a deliberate syntax error (unmatched brace) guaranteed to produce
        // a parser diagnostic regardless of semantic resolution specifics
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "A.sysml"),
            "package A {\n    part def Widget;\n}\n",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "B.sysml"),
            "package B {\n    part def Gadget\n",
            TestContext.Current.CancellationToken);
        var model = new WorkspaceModel();

        // Act
        var snapshot = await model.LoadWorkspaceAsync(_tempRoot);
        var aggregator = new DiagnosticsAggregator();
        aggregator.ReplaceWorkspaceDiagnostics(snapshot.Diagnostics);
        var ordered = aggregator.RebuildAggregate();

        // Assert: diagnostics from across the whole workspace are present in one deterministic, ordered view
        Assert.NotEmpty(ordered);
        Assert.Equal(ordered, ordered.OrderBy(d => d.FilePath, StringComparer.OrdinalIgnoreCase).ThenBy(d => d.Line).ThenBy(d => d.Column));
    }
}
