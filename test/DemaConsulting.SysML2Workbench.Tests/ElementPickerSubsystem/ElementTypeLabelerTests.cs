using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.ElementPickerSubsystem;

/// <summary>
///     Unit-tests <see cref="ElementTypeLabeler.GetTypeLabel" /> against each concrete
///     <c>SysmlNode</c> subtype the picker can encounter, plus the fallback path.
/// </summary>
public sealed class ElementTypeLabelerTests
{
    /// <summary>
    ///     Validates the null-argument guard: passing a <see langword="null" /> node throws
    ///     <see cref="ArgumentNullException" />.
    /// </summary>
    [Fact]
    public void GetTypeLabel_NullNode_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ElementTypeLabeler.GetTypeLabel(null!));
    }

    /// <summary>
    ///     Validates that a <see cref="SysmlDefinitionNode" /> returns its
    ///     <see cref="SysmlDefinitionNode.DefinitionKeyword" /> verbatim.
    /// </summary>
    [Fact]
    public void GetTypeLabel_DefinitionNode_ReturnsDefinitionKeyword()
    {
        // Arrange
        var node = new SysmlDefinitionNode { DefinitionKeyword = "part def" };

        // Act
        var label = ElementTypeLabeler.GetTypeLabel(node);

        // Assert
        Assert.Equal("part def", label);
    }

    /// <summary>
    ///     Validates that a <see cref="SysmlFeatureNode" /> returns its
    ///     <see cref="SysmlFeatureNode.FeatureKeyword" /> verbatim.
    /// </summary>
    [Fact]
    public void GetTypeLabel_FeatureNode_ReturnsFeatureKeyword()
    {
        // Arrange
        var node = new SysmlFeatureNode { FeatureKeyword = "port" };

        // Act
        var label = ElementTypeLabeler.GetTypeLabel(node);

        // Assert
        Assert.Equal("port", label);
    }

    /// <summary>
    ///     Validates that a <see cref="SysmlConnectionNode" /> returns its
    ///     <c>ConnectionKeyword</c> verbatim.
    /// </summary>
    [Fact]
    public void GetTypeLabel_ConnectionNode_ReturnsConnectionKeyword()
    {
        // Arrange
        var node = new SysmlConnectionNode { ConnectionKeyword = "connection" };

        // Act
        var label = ElementTypeLabeler.GetTypeLabel(node);

        // Assert
        Assert.Equal("connection", label);
    }

    /// <summary>
    ///     Validates the fixed-literal mappings for node kinds without a keyword-style member.
    /// </summary>
    [Theory]
    [InlineData(typeof(SysmlImportNode), "import")]
    [InlineData(typeof(SysmlPackageNode), "package")]
    [InlineData(typeof(SysmlViewNode), "view")]
    [InlineData(typeof(SysmlViewpointNode), "viewpoint")]
    [InlineData(typeof(SysmlTransitionNode), "transition")]
    [InlineData(typeof(SysmlSatisfyNode), "satisfy")]
    [InlineData(typeof(SysmlDependencyNode), "dependency")]
    public void GetTypeLabel_FixedLiteralNode_ReturnsExpectedLiteral(Type nodeType, string expected)
    {
        // Arrange
        var node = (SysmlNode)Activator.CreateInstance(nodeType)!;

        // Act
        var label = ElementTypeLabeler.GetTypeLabel(node);

        // Assert
        Assert.Equal(expected, label);
    }

    /// <summary>
    ///     Validates that an otherwise-unrecognized node subtype falls back to a defensive
    ///     label derived from its runtime type name (leading <c>Sysml</c> and trailing
    ///     <c>Node</c> stripped, then lowercased).
    /// </summary>
    [Fact]
    public void GetTypeLabel_UnknownSubtype_UsesFallbackFromTypeName()
    {
        // Arrange
        var node = new SysmlMetadataNode();

        // Act
        var label = ElementTypeLabeler.GetTypeLabel(node);

        // Assert
        Assert.Equal("metadata", label);
    }
}
