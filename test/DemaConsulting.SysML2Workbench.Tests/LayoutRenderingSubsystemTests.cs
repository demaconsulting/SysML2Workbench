using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     Subsystem-level tests exercising LayoutRenderingSubsystem's units (<see cref="LayoutInvoker" />,
///     <see cref="SvgCanvasHost" />) together, per docs/reqstream/sysml2-workbench/layout-rendering-subsystem.yaml.
/// </summary>
public sealed class LayoutRenderingSubsystemTests : IDisposable
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

    private async Task<WorkspaceSnapshot> LoadSampleWorkspaceAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    part def Wheel;\n"
            + "    view PredefinedView {\n"
            + "        expose Engine;\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);

        var model = new WorkspaceModel();
        return await model.LoadWorkspaceAsync(_tempRoot);
    }

    /// <summary>
    ///     Validates that selecting a predefined view renders it and displays the resulting SVG diagram in the
    ///     canvas host.
    /// </summary>
    [Fact]
    public async Task RenderPredefinedView_DisplaysSvgDiagram()
    {
        // Arrange
        var snapshot = await LoadSampleWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();
        var views = presenter.RefreshCatalog(snapshot.Workspace, "rev-1");
        var invoker = new LayoutInvoker();
        var host = new SvgCanvasHost();

        // Act
        var svg = invoker.RenderPredefinedView(snapshot.Workspace, views[0]);
        host.LoadSvg(svg);

        // Assert
        Assert.True(host.IsContentLoaded);
        Assert.Contains("<svg", host.CurrentSvg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Validates that a rendered custom view is displayed in a canvas host that supports pan and zoom
    ///     interaction over it.
    /// </summary>
    [Fact]
    public async Task RenderCustomView_SupportsPanAndZoom()
    {
        // Arrange
        var snapshot = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        definition.AddExposeTarget("Sample::Wheel");
        var invoker = new LayoutInvoker();
        var host = new SvgCanvasHost();

        // Act
        var svg = invoker.RenderCustomView(snapshot.Workspace, definition);
        host.LoadSvg(svg);
        host.SetZoom(1.5);
        host.PanViewport(new Avalonia.Point(4, 4));

        // Assert
        Assert.True(host.IsContentLoaded);
        Assert.Equal(1.5, host.ZoomLevel);
        Assert.Equal(new Avalonia.Point(4, 4), host.ViewportOffset);
    }
}
