using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;
using DemaConsulting.SysML2Workbench.WorkspaceSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.ViewBuilderSubsystem;

/// <summary>
///     Unit tests for <see cref="ViewDefinitionModel" />.
/// </summary>
public sealed class ViewDefinitionModelTests : IDisposable
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
    ///     Loads a small workspace with two elements available as expose targets.
    /// </summary>
    private async Task<SysmlWorkspace> LoadSampleWorkspaceAsync()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "Sample.sysml"),
            "package Sample {\n"
            + "    part def Engine;\n"
            + "    part def Wheel;\n"
            + "}\n",
            TestContext.Current.CancellationToken);

        var model = new WorkspaceModel();
        var snapshot = await model.LoadWorkspaceAsync(_tempRoot);
        return snapshot.Workspace;
    }

    /// <summary>
    ///     Validates that changing the view kind stores the current selection.
    /// </summary>
    [Fact]
    public void ChangeViewKind_StoresCurrentSelection()
    {
        // Arrange: a fresh definition
        var definition = new ViewDefinitionModel();

        // Act: select a view kind
        definition.SetViewKind(ViewKind.Interconnection);

        // Assert: the selection is stored
        Assert.Equal(ViewKind.Interconnection, definition.ViewKind);
    }

    /// <summary>
    ///     Validates that selecting expose targets stores multiple targets in the requested order, without
    ///     duplicates.
    /// </summary>
    [Fact]
    public void SelectTargets_StoresMultipleExposeTargets()
    {
        // Arrange: a fresh definition
        var definition = new ViewDefinitionModel();

        // Act: select three targets, including a duplicate
        definition.SetExposeTargets(["Sample::Engine", "Sample::Wheel", "Sample::Engine"]);

        // Assert: order is preserved and the exact duplicate is removed
        Assert.Equal(["Sample::Engine", "Sample::Wheel"], definition.ExposeTargets);
    }

    /// <summary>
    ///     Validates that the definition reports whether it has enough information to render a preview or export
    ///     a snippet.
    /// </summary>
    [Fact]
    public void DefinitionState_ReportsRenderAndExportReadiness()
    {
        // Arrange: a fresh, empty definition
        var definition = new ViewDefinitionModel();

        // Act / Assert: an empty definition is not ready
        Assert.False(definition.IsReadyToRender);
        Assert.False(definition.IsReadyToExport);

        // Act: supply a view kind only
        definition.SetViewKind(ViewKind.General);
        Assert.False(definition.IsReadyToRender);

        // Act: supply expose targets too
        definition.SetExposeTargets(["Sample::Engine"]);

        // Assert: the definition is now ready for both preview and export
        Assert.True(definition.IsReadyToRender);
        Assert.True(definition.IsReadyToExport);
    }

    /// <summary>
    ///     Validates that a definition targeting real workspace elements validates without diagnostics.
    /// </summary>
    [Fact]
    public async Task ValidateAgainstWorkspace_ResolvableTargets_ReturnsNoDiagnostics()
    {
        // Arrange: a definition targeting two real elements
        var workspace = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.SetExposeTargets(["Sample::Engine", "Sample::Wheel"]);

        // Act: validate against the loaded workspace
        var diagnostics = definition.ValidateAgainstWorkspace(workspace);

        // Assert: no validation findings are reported
        Assert.Empty(diagnostics);
    }

    /// <summary>
    ///     Validates that an unresolved expose target is reported as a diagnostic rather than throwing.
    /// </summary>
    [Fact]
    public async Task ValidateAgainstWorkspace_UnresolvedTarget_ReturnsDiagnostic()
    {
        // Arrange: a definition targeting an element that does not exist
        var workspace = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.SetExposeTargets(["Sample::DoesNotExist"]);

        // Act: validate against the loaded workspace
        var diagnostics = definition.ValidateAgainstWorkspace(workspace);

        // Assert: a validation diagnostic reports the unresolved target
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Message.Contains("Sample::DoesNotExist"));
    }

    /// <summary>
    ///     Validates that an empty definition reports both missing-kind and missing-target diagnostics.
    /// </summary>
    [Fact]
    public async Task ValidateAgainstWorkspace_EmptyDefinition_ReturnsMissingKindAndTargetDiagnostics()
    {
        // Arrange: a definition with nothing selected
        var workspace = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();

        // Act: validate against the loaded workspace
        var diagnostics = definition.ValidateAgainstWorkspace(workspace);

        // Assert: both missing-kind and missing-target findings are present
        Assert.Contains(diagnostics, d => d.Message.Contains("view kind"));
        Assert.Contains(diagnostics, d => d.Message.Contains("expose target"));
    }
}
