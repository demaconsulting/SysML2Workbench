using DemaConsulting.SysML2Tools.Semantic.Model;
using DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests;

/// <summary>
///     Subsystem-level tests exercising ElementPickerSubsystem's unit
///     (<see cref="ElementPickerViewModel" /> plus <see cref="ElementTypeLabeler" />) composed together
///     against a caller-built candidate list, per
///     docs/reqstream/sysml2-workbench/element-picker-subsystem.yaml.
/// </summary>
public sealed class ElementPickerSubsystemTests
{
    /// <summary>
    ///     Validates that the labeler and the view model compose end-to-end: labels computed by
    ///     <see cref="ElementTypeLabeler.GetTypeLabel" /> flow through <see cref="ElementPickerViewModel.SetCandidates" />
    ///     into <see cref="ElementPickerViewModel.AvailableTypeLabels" /> and gate
    ///     <see cref="ElementPickerViewModel.DisplayedItems" /> via the default chip.
    /// </summary>
    [Fact]
    public void SubsystemComposition_LabelerFeedsViewModel_DefaultChipFiltersDisplayedItems()
    {
        // Arrange: three candidates spanning distinct node kinds, mapped through the labeler.
        var partDef = new SysmlDefinitionNode { DefinitionKeyword = "part def" };
        var partUsage = new SysmlFeatureNode { FeatureKeyword = "part" };
        var package = new SysmlPackageNode();

        var candidates = new (string QualifiedName, string TypeLabel)[]
        {
            ("Sample::Engine", ElementTypeLabeler.GetTypeLabel(partDef)),
            ("Sample::engine", ElementTypeLabeler.GetTypeLabel(partUsage)),
            ("Sample", ElementTypeLabeler.GetTypeLabel(package))
        };

        var vm = new ElementPickerViewModel();

        // Act: hand the candidates to the picker with "part" as the default chip.
        vm.SetCandidates(candidates, defaultTypeFilterLabel: "part");

        // Assert: all three labels surface in AvailableTypeLabels (distinct, sorted), the
        // default chip is active, and only the "part" candidate is displayed.
        Assert.Equal(new[] { "package", "part", "part def" }, vm.AvailableTypeLabels);
        Assert.Equal(new[] { "part" }, vm.ActiveTypeFilters);
        Assert.Equal(new[] { "Sample::engine" }, vm.DisplayedItems);
    }
}
