using DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem;
using DemaConsulting.SysML2Workbench.ViewBuilderSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.ViewBuilderSubsystem;

/// <summary>
///     Unit tests for <see cref="SysmlSnippetGenerator" />.
/// </summary>
public sealed class SysmlSnippetGeneratorTests
{
    /// <summary>
    ///     Validates that a normalized custom-view definition produces a copy-pasteable SysML view snippet.
    /// </summary>
    [Fact]
    public void ExportDefinition_EmitsSysmlViewSnippet()
    {
        // Arrange: a definition with a kind and one expose target
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        definition.SetExposeTargets(["Sample::Engine"]);
        var generator = new SysmlSnippetGenerator();

        // Act: generate the snippet
        var snippet = generator.GenerateSnippet(definition);

        // Assert: the snippet declares a named view usage with an expose and render statement
        Assert.Contains("view CustomView {", snippet);
        Assert.Contains("expose Sample::Engine;", snippet);
        Assert.Contains("render asGeneralDiagram;", snippet);
        Assert.EndsWith("}\n", snippet);
    }

    /// <summary>
    ///     Validates that the generated snippet preserves the selected view kind, every expose target, and the
    ///     optional filter expression.
    /// </summary>
    [Fact]
    public void ExportDefinition_PreservesKindTargetsAndFilter()
    {
        // Arrange: a definition with multiple expose targets, a filter, and a display name
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.Interconnection);
        definition.SetExposeTargets(["Sample::Engine", "Sample::Wheel"]);
        definition.SetFilterExpression("@Safety");
        definition.SetDisplayName("EngineOverview");
        var generator = new SysmlSnippetGenerator();

        // Act: generate the snippet
        var snippet = generator.GenerateSnippet(definition);

        // Assert: every selected element is present in the emitted text
        Assert.Contains("view EngineOverview {", snippet);
        Assert.Contains("expose Sample::Engine;", snippet);
        Assert.Contains("expose Sample::Wheel;", snippet);
        Assert.Contains("render asInterconnectionDiagram;", snippet);
        Assert.Contains("filter @Safety;", snippet);
    }

    /// <summary>
    ///     Validates that generating a snippet without a selected view kind is rejected instead of emitting
    ///     malformed SysML.
    /// </summary>
    [Fact]
    public void GenerateSnippet_NoViewKind_ThrowsInvalidOperationException()
    {
        // Arrange: a definition with expose targets but no view kind
        var definition = new ViewDefinitionModel();
        definition.SetExposeTargets(["Sample::Engine"]);
        var generator = new SysmlSnippetGenerator();

        // Act / Assert: generation throws
        Assert.Throws<InvalidOperationException>(() => generator.GenerateSnippet(definition));
    }

    /// <summary>
    ///     Validates that generating a snippet without any expose targets is rejected.
    /// </summary>
    [Fact]
    public void GenerateSnippet_NoExposeTargets_ThrowsInvalidOperationException()
    {
        // Arrange: a definition with a view kind but no expose targets
        var definition = new ViewDefinitionModel();
        definition.SetViewKind(ViewKind.General);
        var generator = new SysmlSnippetGenerator();

        // Act / Assert: generation throws
        Assert.Throws<InvalidOperationException>(() => generator.GenerateSnippet(definition));
    }

    /// <summary>
    ///     Validates that a plain, already-safe identifier is returned unchanged.
    /// </summary>
    [Fact]
    public void SanitizeIdentifier_PlainIdentifier_ReturnsUnchanged()
    {
        // Arrange
        var generator = new SysmlSnippetGenerator();

        // Act
        var result = generator.SanitizeIdentifier("EngineOverview");

        // Assert
        Assert.Equal("EngineOverview", result);
    }

    /// <summary>
    ///     Validates that a name containing spaces or a reserved keyword is quoted to remain valid SysML text.
    /// </summary>
    [Fact]
    public void SanitizeIdentifier_ReservedWordOrInvalidCharacters_QuotesTheName()
    {
        // Arrange
        var generator = new SysmlSnippetGenerator();

        // Act
        var quotedKeyword = generator.SanitizeIdentifier("view");
        var quotedSpaced = generator.SanitizeIdentifier("Engine Overview");

        // Assert
        Assert.Equal("'view'", quotedKeyword);
        Assert.Equal("'Engine Overview'", quotedSpaced);
    }

    /// <summary>
    ///     Validates that formatting a single expose clause emits the target's qualified name verbatim.
    /// </summary>
    [Fact]
    public void FormatExposeClause_QualifiedTarget_EmitsExposeStatement()
    {
        // Arrange
        var generator = new SysmlSnippetGenerator();

        // Act
        var clause = generator.FormatExposeClause("Sample::Engine");

        // Assert
        Assert.Equal("    expose Sample::Engine;", clause);
    }
}
