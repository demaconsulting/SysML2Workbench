using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.LayoutRenderingSubsystem;

/// <summary>
///     Unit tests for <see cref="LayoutInvoker" />.
/// </summary>
public sealed class LayoutInvokerTests : IDisposable
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
    ///     Loads a small workspace containing one predefined view and two elements usable as expose targets.
    /// </summary>
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

        var sourceSet = new WorkspaceSourceSet();

        sourceSet.AddFolder(_tempRoot);
        return await model.LoadWorkspaceAsync(sourceSet.Sources, sourceSet.Resolve());
    }

    /// <summary>
    ///     Validates that selecting a predefined view produces SVG diagram markup.
    /// </summary>
    [Fact]
    public async Task RenderPredefinedView_DisplaysSvgDiagram()
    {
        // Arrange: a workspace with one declared view, discovered through the catalog presenter
        var snapshot = await LoadSampleWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();
        var views = presenter.RefreshCatalog(snapshot.Workspace, "rev-1");
        var view = Assert.Single(views);
        var invoker = new LayoutInvoker();

        // Act: render the predefined view
        var svg = invoker.RenderPredefinedView(snapshot.Workspace, view);

        // Assert: SVG markup is returned
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Validates that a GUI-built custom view with multiple expose targets renders to SVG suitable for
    ///     display with pan and zoom (i.e. loadable into <see cref="SvgCanvasHost" />).
    /// </summary>
    [Fact]
    public async Task RenderCustomView_SupportsPanAndZoom()
    {
        // Arrange: a custom view definition exposing both elements
        var snapshot = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        definition.AddExposeTarget("Sample::Wheel");
        var invoker = new LayoutInvoker();

        // Act: render the custom view and load the result into a canvas host
        var svg = invoker.RenderCustomView(snapshot.Workspace, definition);
        var host = new SvgCanvasHost();
        host.LoadSvg(svg);
        host.SetZoom(2.0);
        host.PanViewport(new Avalonia.Point(10, 5));

        // Assert: the canvas host accepted the diagram and the pan/zoom state was applied
        Assert.True(host.IsContentLoaded);
        Assert.Equal(2.0, host.ZoomLevel);
        Assert.Equal(new Avalonia.Point(10, 5), host.ViewportOffset);
    }

    /// <summary>
    ///     Validates that rendering a custom view never mutates the shared, live workspace's declarations.
    /// </summary>
    [Fact]
    public async Task RenderCustomView_DoesNotMutateLiveWorkspace()
    {
        // Arrange
        var snapshot = await LoadSampleWorkspaceAsync();
        var originalCount = snapshot.Workspace.Declarations.Count;
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        var invoker = new LayoutInvoker();

        // Act
        invoker.RenderCustomView(snapshot.Workspace, definition);

        // Assert: the live workspace's declaration count is unchanged
        Assert.Equal(originalCount, snapshot.Workspace.Declarations.Count);
    }

    /// <summary>
    ///     Regression test for the SysML2Tools 0.1.0-beta.8 <c>ResolvedExposeMembers</c> requirement: without it,
    ///     <c>ExposeScopeResolver</c> treats the ephemeral preview node as unscoped and renders the entire
    ///     workspace instead of just the selected targets. Selecting only one of two workspace elements must
    ///     therefore produce SVG that does not contain the unselected element.
    /// </summary>
    [Fact]
    public async Task RenderCustomView_ScopesOutputToSelectedTargetsOnly()
    {
        // Arrange: a custom view definition exposing only one of the workspace's two elements
        var snapshot = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        var invoker = new LayoutInvoker();

        // Act: render the custom view
        var svg = invoker.RenderCustomView(snapshot.Workspace, definition);

        // Assert: the selected target is present, but the unselected target is not - proving the preview node
        // is scoped, not rendering the whole workspace
        Assert.Contains("Engine", svg);
        Assert.DoesNotContain("Wheel", svg);
    }

    /// <summary>
    ///     Validates that a custom view exposing the same qualified name twice under two different recursion
    ///     kinds renders without error, covering the valid SysML v2 pattern of exposing the same package both
    ///     exactly and via its direct children (for example <c>expose PublishingSubsystem;</c> and
    ///     <c>expose PublishingSubsystem::*;</c>).
    /// </summary>
    [Fact]
    public async Task RenderCustomView_SameQualifiedNameTwoRecursionKinds_RendersWithoutError()
    {
        // Arrange: a custom view definition exposing the same qualified name under two recursion kinds
        var snapshot = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        definition.SetExposeRecursionKind("Sample::Engine", ExposeRecursionKind.MembershipRecursive, ExposeRecursionKind.MembershipExact);
        definition.AddExposeTarget("Sample::Engine");
        definition.SetExposeRecursionKind("Sample::Engine", ExposeRecursionKind.MembershipRecursive, ExposeRecursionKind.NamespaceDirectChildren);
        Assert.Equal(2, definition.ExposeTargets.Count(t => t.QualifiedName == "Sample::Engine"));
        var invoker = new LayoutInvoker();

        // Act: render the custom view
        var svg = invoker.RenderCustomView(snapshot.Workspace, definition);

        // Assert: rendering succeeds and produces SVG containing the shared target
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Engine", svg);
    }
}
