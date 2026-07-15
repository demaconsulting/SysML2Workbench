using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     Subsystem-level tests exercising ViewCatalogSubsystem's unit (<see cref="ViewCatalogPresenter" />) against
///     a real loaded workspace, per docs/reqstream/sysml2-workbench/view-catalog-subsystem.yaml.
/// </summary>
public sealed class ViewCatalogSubsystemTests : IDisposable
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

    private async Task<SysML2Tools.Semantic.SysmlWorkspace> LoadWorkspaceWithTwoViewsAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    part def Wheel;\n"
            + "    view GeneralView {\n"
            + "        expose Engine;\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "    view SequenceView {\n"
            + "        expose Wheel;\n"
            + "        render asSequenceDiagram;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);

        var model = new WorkspaceModel();
        var snapshot = await model.LoadWorkspaceAsync(_tempRoot);
        return snapshot.Workspace;
    }

    /// <summary>
    ///     Validates that the loaded model's declared views are all listed as supported predefined views.
    /// </summary>
    [Fact]
    public async Task LoadedModel_ListsSupportedViews()
    {
        // Arrange
        var workspace = await LoadWorkspaceWithTwoViewsAsync();
        var presenter = new ViewCatalogPresenter();

        // Act
        var views = presenter.RefreshCatalog(workspace, "rev-1");

        // Assert: both declared views are present with their recognized kinds
        Assert.Equal(2, views.Count);
        Assert.Contains(views, v => v.Name == "GeneralView" && v.Kind == ViewKind.General);
        Assert.Contains(views, v => v.Name == "SequenceView" && v.Kind == ViewKind.Sequence);
    }

    /// <summary>
    ///     Validates that selecting a view publishes it as the active selection.
    /// </summary>
    [Fact]
    public async Task SelectView_PublishesActiveSelection()
    {
        // Arrange
        var workspace = await LoadWorkspaceWithTwoViewsAsync();
        var presenter = new ViewCatalogPresenter();
        var views = presenter.RefreshCatalog(workspace, "rev-1");
        var target = views.First(v => v.Name == "SequenceView");

        // Act
        presenter.SelectView(target.QualifiedName);

        // Assert
        Assert.Equal(target, presenter.GetSelectedView());
        Assert.Equal(target.QualifiedName, presenter.SelectedViewId);
    }
}
