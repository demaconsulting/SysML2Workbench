using DemaConsulting.SysML2Tools.Io;
using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Tools.Query;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Stdlib;
using DemaConsulting.SysML2Workbench;

namespace OtsSoftwareTests;

/// <summary>
///     Verifies the OTS SysML2Tools requirements in docs/reqstream/ots/sysml2-tools.yaml: that the workbench
///     genuinely depends on the published <c>DemaConsulting.SysML2Tools</c> packages for multi-file model
///     parsing/import resolution and for view rendering, rather than a local re-implementation.
/// </summary>
public sealed class SysML2ToolsTests : IDisposable
{
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-ots-sysml2tools-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    /// <summary>
    ///     Validates that <see cref="GlobFileCollector" />, <see cref="StdlibProvider" />, and
    ///     <see cref="WorkspaceLoader" /> - used directly, exactly as the workbench's WorkspaceModel calls them -
    ///     discover a multi-file workspace and resolve a cross-file import between its files.
    /// </summary>
    [Fact]
    public async Task LoadWorkspaceModel_ParsesAndResolvesImports()
    {
        // Arrange: two files where one imports a definition declared in the other
        await File.WriteAllTextAsync(
            PathHelpers.SafePathCombine(_tempRoot, "Parts.sysml"),
            "package Parts {\n    part def Engine;\n}\n",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            PathHelpers.SafePathCombine(_tempRoot, "Vehicle.sysml"),
            "package Vehicle {\n"
            + "    private import Parts::*;\n"
            + "    part def Car {\n"
            + "        part engine : Engine;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);

        // Act: the same discovery/load pipeline the workbench uses
        var discoveredFiles = GlobFileCollector.Collect(["**/*.sysml"], [], _tempRoot);
        var (symbolTable, _) = StdlibProvider.GetSymbolTable();
        var loadResult = await WorkspaceLoader.LoadAsync(discoveredFiles, symbolTable);

        // Assert: both files were discovered, the workspace parsed without diagnostics, and the cross-file
        // import resolved so `Car::engine`'s type is the `Parts::Engine` declared in the other file
        Assert.Equal(2, discoveredFiles.Count);
        Assert.Empty(loadResult.Diagnostics);
        Assert.True(loadResult.Workspace!.Declarations.ContainsKey("Vehicle::Car"));
        Assert.True(loadResult.Workspace.Declarations.ContainsKey("Parts::Engine"));
    }

    /// <summary>
    ///     Validates that SysML2Tools generates renderable diagram output for a selected view usage.
    /// </summary>
    /// <remarks>
    ///     Deviation from the reqstream-mandated test name: this test predates the empirical discovery (recorded
    ///     in the planning report's Assumption #1) that SysML2Tools 0.1.0-beta.7 has no public
    ///     <c>LayoutGraph</c> type or layout-strategy registry -
    ///     <see cref="DemaConsulting.SysML2Tools.Rendering.DiagramRenderer.RenderWorkspace" /> fuses layout and
    ///     SVG rendering into one call. The test name is kept unchanged to preserve ReqStream traceability; the
    ///     assertions instead verify the real, single-call contract: that the OTS package can turn a named view
    ///     usage into concrete rendered diagram output.
    /// </remarks>
    [Fact]
    public async Task RenderView_GeneratesLayoutGraph()
    {
        // Arrange
        await File.WriteAllTextAsync(
            PathHelpers.SafePathCombine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    view PredefinedView {\n"
            + "        expose Engine;\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);
        var discoveredFiles = GlobFileCollector.Collect(["**/*.sysml"], [], _tempRoot);
        var (symbolTable, _) = StdlibProvider.GetSymbolTable();
        var loadResult = await WorkspaceLoader.LoadAsync(discoveredFiles, symbolTable);

        // Act
        var renderer = new DemaConsulting.SysML2Tools.Rendering.DiagramRenderer();
        var svgRenderer = new DemaConsulting.Rendering.Svg.SvgRenderer();
        var options = new DemaConsulting.Rendering.Abstractions.RenderOptions(DemaConsulting.Rendering.Abstractions.Themes.Light);
        var outputs = renderer.RenderWorkspace(loadResult.Workspace!, svgRenderer, options, "PredefinedView");

        // Assert: SysML2Tools produced concrete, renderable diagram output for the requested view
        Assert.NotEmpty(outputs);
    }

    /// <summary>
    ///     Validates that the impact analysis in <see cref="QueryEngine" /> traverses connector
    ///     (<c>connect</c>) relationships only when <see cref="QueryOptions.IncludeConnections" /> is set,
    ///     and that entries reached that way carry the machine-readable traversal metadata
    ///     (<see cref="QueryResultEntry.Depth" /> and <see cref="QueryResultEntry.Relation" />) the
    ///     workbench's results panel displays.
    /// </summary>
    /// <remarks>
    ///     This pins the dependency's narrowed default: the impact walk now follows resolved reference
    ///     edges only, so connector reachability the workbench previously got implicitly must be
    ///     requested explicitly.
    /// </remarks>
    [Fact]
    public async Task ImpactQuery_IncludeConnections_TraversesConnectorEdges()
    {
        // Arrange: two sibling parts joined by nothing but a connector
        await File.WriteAllTextAsync(
            PathHelpers.SafePathCombine(_tempRoot, "Connected.sysml"),
            "package Connected {\n"
            + "    part def Engine;\n"
            + "    part def Gearbox;\n"
            + "    part def Car {\n"
            + "        part engine : Engine;\n"
            + "        part gearbox : Gearbox;\n"
            + "        connect engine to gearbox;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);
        var discoveredFiles = GlobFileCollector.Collect(["**/*.sysml"], [], _tempRoot);
        var (symbolTable, _) = StdlibProvider.GetSymbolTable();
        var loadResult = await WorkspaceLoader.LoadAsync(discoveredFiles, symbolTable);
        var workspace = loadResult.Workspace!;
        var target = workspace.Declarations["Connected::Car::engine"];

        // Act: the same impact query with connector traversal off, then on
        var withoutConnections = QueryEngine.Impact(
            workspace,
            target,
            new QueryOptions { Verb = QueryVerb.Impact, Element = "Connected::Car::engine" });
        var withConnections = QueryEngine.Impact(
            workspace,
            target,
            new QueryOptions
            {
                Verb = QueryVerb.Impact,
                Element = "Connected::Car::engine",
                IncludeConnections = true,
            });

        // Assert: the connector-reached sibling appears only when connections are included, and its
        // entry carries the traversal metadata the workbench surfaces
        Assert.DoesNotContain(withoutConnections.Entries, entry => entry.QualifiedName == "Connected::Car::gearbox");
        var reached = Assert.Single(
            withConnections.Entries,
            entry => entry.QualifiedName == "Connected::Car::gearbox");
        Assert.NotNull(reached.Depth);
        Assert.NotNull(reached.Relation);
    }

    /// <summary>
    ///     Validates that <see cref="QueryOptions.WalkDepth" /> bounds connector traversal on exactly the
    ///     same terms as any other relationship: one connector is one unit of depth, and a
    ///     <see langword="null" /> walk depth is unlimited for connector edges too.
    /// </summary>
    /// <remarks>
    ///     The workbench's Impact walk-depth control tells the user that a blank value means unlimited,
    ///     with no connector-specific exception. That wording is only truthful while the dependency
    ///     treats depth uniformly - SysML2Tools 0.2.0-beta.1 instead capped an unbounded connector walk
    ///     at a single hop, which made the full connector closure unreachable from the dialog at any
    ///     setting. This pins the 0.2.0-beta.2 behavior so a regression is caught here rather than
    ///     silently turning the dialog's label into a lie.
    /// </remarks>
    [Fact]
    public async Task ImpactQuery_WalkDepth_BoundsConnectorTraversalUniformly()
    {
        // Arrange: a connector chain three hops long, joined by nothing but connectors
        await File.WriteAllTextAsync(
            PathHelpers.SafePathCombine(_tempRoot, "Chain.sysml"),
            "package Chain {\n"
            + "    part def Node;\n"
            + "    part def Assembly {\n"
            + "        part a : Node;\n"
            + "        part b : Node;\n"
            + "        part c : Node;\n"
            + "        part d : Node;\n"
            + "        connect a to b;\n"
            + "        connect b to c;\n"
            + "        connect c to d;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);
        var discoveredFiles = GlobFileCollector.Collect(["**/*.sysml"], [], _tempRoot);
        var (symbolTable, _) = StdlibProvider.GetSymbolTable();
        var loadResult = await WorkspaceLoader.LoadAsync(discoveredFiles, symbolTable);
        var workspace = loadResult.Workspace!;
        var target = workspace.Declarations["Chain::Assembly::a"];

        QueryResult Impact(int? walkDepth) => QueryEngine.Impact(
            workspace,
            target,
            new QueryOptions
            {
                Verb = QueryVerb.Impact,
                Element = "Chain::Assembly::a",
                WalkDepth = walkDepth,
                IncludeConnections = true,
            });

        // Act: the chain walked unbounded, then bounded to one hop and to two
        var unbounded = Impact(null);
        var depthOne = Impact(1);
        var depthTwo = Impact(2);

        // Assert: an unbounded walk reaches the whole chain, reporting each element's distance so the
        // results panel can present impact as an increasing blast radius
        Assert.Equal(
            [("Chain::Assembly::b", 1), ("Chain::Assembly::c", 2), ("Chain::Assembly::d", 3)],
            unbounded.Entries.Select(entry => (entry.QualifiedName, entry.Depth)).OrderBy(row => row.Depth));

        // Assert: an explicit depth truncates that same chain at exactly that many connector hops
        Assert.Equal(["Chain::Assembly::b"], depthOne.Entries.Select(entry => entry.QualifiedName));
        Assert.Equal(
            ["Chain::Assembly::b", "Chain::Assembly::c"],
            depthTwo.Entries.Select(entry => entry.QualifiedName).Order());
    }
}
