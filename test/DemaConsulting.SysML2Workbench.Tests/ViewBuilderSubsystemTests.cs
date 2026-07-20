using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     Subsystem-level tests exercising ViewBuilderSubsystem's units (<see cref="ViewDefinitionModel" />,
///     <see cref="SysmlSnippetGenerator" />) together, per
///     docs/reqstream/sysml2-workbench/view-builder-subsystem.yaml.
/// </summary>
public sealed class ViewBuilderSubsystemTests : IDisposable
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
    ///     Validates that editing a custom view definition tracks the user's kind, target, and filter inputs,
    ///     and that the definition validates cleanly against a real loaded workspace.
    /// </summary>
    [Fact]
    public async Task EditDefinition_TracksCustomViewInputs()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n    part def Engine;\n    part def Wheel;\n}\n",
            TestContext.Current.CancellationToken);
        var model = new WorkspaceModel();
        var sourceSet = new WorkspaceSourceSet();
        sourceSet.AddFolder(_tempRoot);
        var snapshot = await model.LoadWorkspaceAsync(sourceSet.Sources, sourceSet.Resolve());
        var definition = new ViewDefinitionModel();

        // Act: the user makes a sequence of builder selections
        definition.SetViewKind(ViewKind.Grid);
        definition.AddExposeTarget("Sample::Engine");
        definition.AddExposeTarget("Sample::Wheel");
        definition.SetFilterExpression("@Safety");
        definition.SetDisplayName("EngineGrid");

        // Assert: every input is tracked and the definition validates against the real workspace
        Assert.Equal(ViewKind.Grid, definition.ViewKind);
        Assert.Equal(["Sample::Engine", "Sample::Wheel"], definition.ExposeTargets.Select(t => t.QualifiedName));
        Assert.Equal("@Safety", definition.FilterExpression);
        Assert.Equal("EngineGrid", definition.DisplayName);
        Assert.Empty(definition.ValidateAgainstWorkspace(snapshot.Workspace));
    }

    /// <summary>
    ///     Validates that a fully edited custom-view definition can be exported as a valid SysML snippet.
    /// </summary>
    [Fact]
    public void ExportDefinition_GeneratesSysmlSnippet()
    {
        // Arrange
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.ActionFlow);
        definition.AddExposeTarget("Sample::Engine");
        definition.SetDisplayName("EngineFlow");
        var generator = new SysmlSnippetGenerator();

        // Act
        var snippet = generator.GenerateSnippet(definition);

        // Assert
        Assert.Contains("view EngineFlow {", snippet);
        Assert.Contains("expose Sample::Engine::**;", snippet);
        Assert.Contains("render asActionFlowDiagram;", snippet);
    }
}
