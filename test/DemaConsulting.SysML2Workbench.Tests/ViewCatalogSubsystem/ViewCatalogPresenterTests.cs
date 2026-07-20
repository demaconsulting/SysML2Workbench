using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewCatalogSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.ViewCatalogSubsystem;

/// <summary>
///     Unit tests for <see cref="ViewCatalogPresenter" />.
/// </summary>
public sealed class ViewCatalogPresenterTests : IDisposable
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
    ///     Loads a small workspace containing exactly one declared predefined view.
    /// </summary>
    private async Task<WorkspaceSnapshot> LoadSingleViewWorkspaceAsync()
    {
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await File.WriteAllTextAsync(
            filePath,
            "package Sample {\n"
            + "    part def Widget;\n"
            + "    part myWidget : Widget;\n"
            + "    view MyView {\n"
            + "        expose myWidget;\n"
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
    ///     Validates that the catalog lists every predefined view declared in the loaded model.
    /// </summary>
    [Fact]
    public async Task LoadedModel_ListsSupportedViewDefinitions()
    {
        // Arrange: a workspace with one declared view
        var snapshot = await LoadSingleViewWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();

        // Act: refresh the catalog from the loaded workspace
        var views = presenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);

        // Assert: the declared view is present in the catalog
        Assert.Single(views);
        Assert.Equal("Sample::MyView", views[0].QualifiedName);
    }

    /// <summary>
    ///     Validates that each catalog entry carries the view's kind and display name.
    /// </summary>
    [Fact]
    public async Task LoadedModel_ShowsViewKindAndDisplayName()
    {
        // Arrange: a workspace with one declared general-diagram view
        var snapshot = await LoadSingleViewWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();

        // Act: refresh the catalog
        var views = presenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);

        // Assert: the descriptor exposes both the recognized kind and a display name
        Assert.Equal(ViewKind.General, views[0].Kind);
        Assert.Equal("MyView", views[0].DisplayName);
    }

    /// <summary>
    ///     Validates that selecting a predefined view publishes it as the current selection.
    /// </summary>
    [Fact]
    public async Task SelectView_PublishesCurrentSelection()
    {
        // Arrange: a catalog refreshed from a loaded workspace
        var snapshot = await LoadSingleViewWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();
        presenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);

        // Act: select the only available view
        var selected = presenter.SelectView("Sample::MyView");

        // Assert: the selection is published both as the return value and as presenter state
        Assert.Equal("Sample::MyView", selected.QualifiedName);
        Assert.Equal("Sample::MyView", presenter.SelectedViewId);
        Assert.Equal("Sample::MyView", presenter.GetSelectedView()?.QualifiedName);
    }

    /// <summary>
    ///     Validates that selecting an identifier absent from the catalog is rejected instead of silently
    ///     accepted.
    /// </summary>
    [Fact]
    public async Task SelectView_UnknownIdentifier_ThrowsArgumentException()
    {
        // Arrange: a catalog refreshed from a loaded workspace
        var snapshot = await LoadSingleViewWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();
        presenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);

        // Act / Assert: selecting an unknown identifier throws
        Assert.Throws<ArgumentException>(() => presenter.SelectView("Sample::DoesNotExist"));
    }

    /// <summary>
    ///     Validates that a stale selection is cleared when a catalog refresh no longer contains it.
    /// </summary>
    [Fact]
    public async Task RefreshCatalog_SelectionNoLongerPresent_ClearsSelection()
    {
        // Arrange: select a view, then refresh from an empty workspace
        var snapshot = await LoadSingleViewWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();
        presenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);
        presenter.SelectView("Sample::MyView");

        File.Delete(Path.Combine(_tempRoot, "Sample.sysml"));
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Empty.sysml"),
            "package Empty {\n}\n",
            TestContext.Current.CancellationToken);
        var model = new WorkspaceModel();
        var sourceSet = new WorkspaceSourceSet();
        sourceSet.AddFolder(_tempRoot);
        var emptySnapshot = await model.LoadWorkspaceAsync(sourceSet.Sources, sourceSet.Resolve());

        // Act: refresh the catalog from the now view-less workspace
        presenter.RefreshCatalog(emptySnapshot.Workspace, emptySnapshot.RevisionId);

        // Assert: the stale selection was cleared
        Assert.Null(presenter.SelectedViewId);
        Assert.Null(presenter.GetSelectedView());
    }

    /// <summary>
    ///     Loads a small workspace containing a view with multiple expose members (differing recursion kinds and
    ///     a bracket filter on the recursive one) and a filter expression, so <see cref="ViewCatalogPresenter.BuildViewDefinition" />
    ///     has real fidelity to check.
    /// </summary>
    private async Task<WorkspaceSnapshot> LoadRichViewWorkspaceAsync()
    {
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await File.WriteAllTextAsync(
            filePath,
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    part def Wheel;\n"
            + "    view MyView {\n"
            + "        expose Engine;\n"
            + "        expose Wheel::**[@Safety];\n"
            + "        render asGeneralDiagram;\n"
            + "        filter @Critical;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);

        var model = new WorkspaceModel();
        var sourceSet = new WorkspaceSourceSet();
        sourceSet.AddFolder(_tempRoot);
        return await model.LoadWorkspaceAsync(sourceSet.Sources, sourceSet.Resolve());
    }

    /// <summary>
    ///     Validates that <see cref="ViewCatalogPresenter.BuildViewDefinition" /> faithfully reconstructs a
    ///     predefined view's kind, every expose member (recursion kind and bracket filter included), filter
    ///     expression, and display name.
    /// </summary>
    [Fact]
    public async Task BuildViewDefinition_ResolvesKindExposeAndFilter()
    {
        // Arrange
        var snapshot = await LoadRichViewWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();
        presenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);

        // Act
        var definition = presenter.BuildViewDefinition(snapshot.Workspace, "Sample::MyView");

        // Assert
        Assert.NotNull(definition);
        Assert.Equal(ViewKind.General, definition!.ViewKind);
        Assert.Equal("MyView", definition.DisplayName);
        Assert.Equal("@Critical", definition.FilterExpression);
        Assert.Equal(2, definition.ExposeTargets.Count);
        Assert.Contains(definition.ExposeTargets, t => t.QualifiedName == "Sample::Engine" && t.RecursionKind == ExposeRecursionKind.MembershipExact);
        Assert.Contains(definition.ExposeTargets, t =>
            t.QualifiedName == "Sample::Wheel"
            && t.RecursionKind == ExposeRecursionKind.NamespaceRecursive
            && t.BracketFilterExpression == "@Safety");
    }

    /// <summary>
    ///     Validates that an unknown view identifier produces <see langword="null" /> rather than throwing.
    /// </summary>
    [Fact]
    public async Task BuildViewDefinition_UnknownViewId_ReturnsNull()
    {
        var snapshot = await LoadSingleViewWorkspaceAsync();
        var presenter = new ViewCatalogPresenter();
        presenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);

        var definition = presenter.BuildViewDefinition(snapshot.Workspace, "Sample::DoesNotExist");

        Assert.Null(definition);
    }

    /// <summary>
    ///     Validates that a predefined view with zero expose members - a valid, unscoped "expose everything" view
    ///     - produces <see langword="null" /> since there is no finite expose list to reconstruct.
    /// </summary>
    [Fact]
    public async Task BuildViewDefinition_NoExposeMembers_ReturnsNull()
    {
        var filePath = Path.Combine(_tempRoot, "Sample.sysml");
        await File.WriteAllTextAsync(
            filePath,
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    view MyView {\n"
            + "        render asGeneralDiagram;\n"
            + "    }\n"
            + "}\n",
            TestContext.Current.CancellationToken);

        var model = new WorkspaceModel();
        var sourceSet = new WorkspaceSourceSet();
        sourceSet.AddFolder(_tempRoot);
        var snapshot = await model.LoadWorkspaceAsync(sourceSet.Sources, sourceSet.Resolve());

        var presenter = new ViewCatalogPresenter();
        presenter.RefreshCatalog(snapshot.Workspace, snapshot.RevisionId);

        var definition = presenter.BuildViewDefinition(snapshot.Workspace, "Sample::MyView");

        Assert.Null(definition);
    }
}
