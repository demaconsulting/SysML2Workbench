using DemaConsulting.SysML2Tools.Parser;
using DemaConsulting.SysML2Tools.Semantic;
using DemaConsulting.SysML2Tools.Semantic.Model;
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
    ///     duplicates, each defaulting to <see cref="ExposeRecursionKind.MembershipRecursive" />.
    /// </summary>
    [Fact]
    public void AddExposeTarget_StoresMultipleExposeTargetsWithoutDuplicates()
    {
        // Arrange: a fresh definition
        var definition = new ViewDefinitionModel();

        // Act: add three targets, including a duplicate
        definition.AddExposeTarget("Sample::Engine");
        definition.AddExposeTarget("Sample::Wheel");
        definition.AddExposeTarget("Sample::Engine");

        // Assert: order is preserved and the exact duplicate is removed
        Assert.Equal(["Sample::Engine", "Sample::Wheel"], definition.ExposeTargets.Select(t => t.QualifiedName));
        Assert.All(definition.ExposeTargets, t => Assert.Equal(ExposeRecursionKind.MembershipRecursive, t.RecursionKind));
    }

    /// <summary>
    ///     Validates that removing an expose target drops only that selection.
    /// </summary>
    [Fact]
    public void RemoveExposeTarget_RemovesOnlyMatchingSelection()
    {
        // Arrange: a definition with two targets
        var definition = new ViewDefinitionModel();
        definition.AddExposeTarget("Sample::Engine");
        definition.AddExposeTarget("Sample::Wheel");

        // Act: remove one target
        definition.RemoveExposeTarget("Sample::Engine");

        // Assert: only the other target remains
        Assert.Equal(["Sample::Wheel"], definition.ExposeTargets.Select(t => t.QualifiedName));
    }

    /// <summary>
    ///     Validates that removing a qualified name that was never added is a no-op.
    /// </summary>
    [Fact]
    public void RemoveExposeTarget_UnknownQualifiedName_IsNoOp()
    {
        // Arrange: a definition with one target
        var definition = new ViewDefinitionModel();
        definition.AddExposeTarget("Sample::Engine");

        // Act: attempt to remove an unrelated qualified name
        definition.RemoveExposeTarget("Sample::DoesNotExist");

        // Assert: the existing target is untouched
        Assert.Equal(["Sample::Engine"], definition.ExposeTargets.Select(t => t.QualifiedName));
    }

    /// <summary>
    ///     Validates that changing a selected target's recursion kind updates only that target.
    /// </summary>
    [Fact]
    public void SetExposeRecursionKind_ChangesSelectedTargetKind()
    {
        // Arrange: a definition with two targets
        var definition = new ViewDefinitionModel();
        definition.AddExposeTarget("Sample::Engine");
        definition.AddExposeTarget("Sample::Wheel");

        // Act: change one target's recursion kind
        definition.SetExposeRecursionKind("Sample::Engine", ExposeRecursionKind.NamespaceDirectChildren);

        // Assert: only the targeted selection changed
        Assert.Equal(ExposeRecursionKind.NamespaceDirectChildren, definition.ExposeTargets.Single(t => t.QualifiedName == "Sample::Engine").RecursionKind);
        Assert.Equal(ExposeRecursionKind.MembershipRecursive, definition.ExposeTargets.Single(t => t.QualifiedName == "Sample::Wheel").RecursionKind);
    }

    /// <summary>
    ///     Validates that setting/clearing a target's bracket-filter expression works and is a no-op for an
    ///     unknown qualified name.
    /// </summary>
    [Fact]
    public void SetExposeBracketFilter_SetsAndClearsExpression()
    {
        // Arrange: a definition with one recursive target
        var definition = new ViewDefinitionModel();
        definition.AddExposeTarget("Sample::Engine");

        // Act: set a bracket-filter expression
        definition.SetExposeBracketFilter("Sample::Engine", "@Safety");

        // Assert: the expression is stored
        Assert.Equal("@Safety", definition.ExposeTargets.Single().BracketFilterExpression);

        // Act: clear it again
        definition.SetExposeBracketFilter("Sample::Engine", "  ");

        // Assert: the expression is cleared
        Assert.Null(definition.ExposeTargets.Single().BracketFilterExpression);

        // Act: attempt to set a filter on an unknown qualified name
        definition.SetExposeBracketFilter("Sample::DoesNotExist", "@Safety");

        // Assert: no selection was added and the known one is unaffected
        Assert.Single(definition.ExposeTargets);
        Assert.Null(definition.ExposeTargets.Single().BracketFilterExpression);
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
        definition.AddExposeTarget("Sample::Engine");

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
        definition.AddExposeTarget("Sample::Engine");
        definition.AddExposeTarget("Sample::Wheel");

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
        definition.AddExposeTarget("Sample::DoesNotExist");

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

    /// <summary>
    ///     Validates that a valid bracket-filter expression on a recursive target produces no diagnostics.
    /// </summary>
    [Fact]
    public async Task ValidateAgainstWorkspace_ValidBracketFilterOnRecursiveTarget_ReturnsNoDiagnostics()
    {
        // Arrange: a definition with a recursive target carrying a valid bracket-filter expression
        var workspace = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        definition.SetExposeRecursionKind("Sample::Engine", ExposeRecursionKind.MembershipRecursive);
        definition.SetExposeBracketFilter("Sample::Engine", "@Safety");

        // Act: validate against the loaded workspace
        var diagnostics = definition.ValidateAgainstWorkspace(workspace);

        // Assert: no validation findings are reported
        Assert.Empty(diagnostics);
    }

    /// <summary>
    ///     Validates that an unparsable bracket-filter expression is reported as a diagnostic.
    /// </summary>
    [Fact]
    public async Task ValidateAgainstWorkspace_InvalidBracketFilterExpression_ReturnsDiagnostic()
    {
        // Arrange: a definition with a recursive target carrying a malformed bracket-filter expression
        var workspace = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        definition.SetExposeBracketFilter("Sample::Engine", "@@@not valid@@@");

        // Act: validate against the loaded workspace
        var diagnostics = definition.ValidateAgainstWorkspace(workspace);

        // Assert: a parse-failure diagnostic is reported
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    ///     Validates that a bracket-filter expression on a <see cref="ExposeRecursionKind.MembershipExact" /> or
    ///     <see cref="ExposeRecursionKind.NamespaceDirectChildren" /> target is reported as a diagnostic, since
    ///     bracket filters are only valid SysML v2 syntax on the two recursive expose forms.
    /// </summary>
    [Theory]
    [InlineData(ExposeRecursionKind.MembershipExact)]
    [InlineData(ExposeRecursionKind.NamespaceDirectChildren)]
    public async Task ValidateAgainstWorkspace_BracketFilterOnNonRecursiveKind_ReturnsDiagnostic(ExposeRecursionKind kind)
    {
        // Arrange: a definition with a non-recursive target carrying a bracket-filter expression
        var workspace = await LoadSampleWorkspaceAsync();
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.AddExposeTarget("Sample::Engine");
        definition.SetExposeRecursionKind("Sample::Engine", kind);
        definition.SetExposeBracketFilter("Sample::Engine", "@Safety");

        // Act: validate against the loaded workspace
        var diagnostics = definition.ValidateAgainstWorkspace(workspace);

        // Assert: a wrong-kind diagnostic is reported
        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Message.Contains("does not support"));
    }
}
