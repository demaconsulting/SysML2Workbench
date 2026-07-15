using DemaConsulting.Rendering.Abstractions;
using DemaConsulting.Rendering.Svg;
using DemaConsulting.SysML2Tools.Io;
using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Tools.Rendering;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Stdlib;

namespace OtsSoftwareTests;

/// <summary>
///     Verifies the OTS DemaConsulting.Rendering requirements in docs/reqstream/ots/rendering.yaml: that the
///     package genuinely produces SVG diagram output from a SysML2Tools render request, and that the SVG
///     preserves the diagram primitives (visible element labels) the workbench's canvas needs to display.
/// </summary>
public sealed class RenderingTests : IDisposable
{
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("sysml2workbench-ots-rendering-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private async Task<string> RenderSampleViewAsync()
    {
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

        var renderer = new DiagramRenderer();
        var svgRenderer = new SvgRenderer();
        var options = new RenderOptions(Themes.Light);
        var outputs = renderer.RenderWorkspace(loadResult.Workspace!, svgRenderer, options, "PredefinedView");

        using var reader = new StreamReader(outputs[0].Data);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    ///     Validates that <see cref="DemaConsulting.Rendering.Svg.SvgRenderer" />, invoked through
    ///     <see cref="DiagramRenderer.RenderWorkspace" />, produces a well-formed, self-contained SVG document.
    /// </summary>
    [Fact]
    public async Task RenderLayout_ProducesSvgDocument()
    {
        // Act
        var svg = await RenderSampleViewAsync();

        // Assert: a well-formed, self-contained SVG document was produced
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("</svg>", svg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Validates that the rendered SVG preserves the diagram primitives (element labels) needed for
    ///     interactive viewing in the desktop shell, rather than losing model content during rendering.
    /// </summary>
    [Fact]
    public async Task RenderLayout_PreservesDiagramPrimitives()
    {
        // Act
        var svg = await RenderSampleViewAsync();

        // Assert: the exposed part definition is still visible as diagram content in the rendered SVG
        Assert.Contains("Engine", svg, StringComparison.Ordinal);
    }
}
