using DemaConsulting.SysML2Tools.Io;
using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Stdlib;

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
            Path.Combine(_tempRoot, "Parts.sysml"),
            "package Parts {\n    part def Engine;\n}\n",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Vehicle.sysml"),
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
            Path.Combine(_tempRoot, "Sample.sysml"),
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
}
