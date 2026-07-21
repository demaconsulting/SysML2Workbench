using DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.ElementPickerSubsystem;

/// <summary>
///     Unit-tests the reusable <see cref="ElementPickerViewModel" /> in isolation, without any
///     <c>MainWindowShell</c>, workspace, or Avalonia dependency. Every scenario constructs a
///     small hand-built candidate list (qualified name + type label) so the expected filtering
///     semantics can be verified precisely.
/// </summary>
public sealed class ElementPickerViewModelTests
{
    /// <summary>
    ///     A representative candidate list covering three distinct type labels ("part",
    ///     "part def", "package") so the OR-then-AND filter behavior can be exercised without
    ///     having to spin up a real workspace.
    /// </summary>
    private static readonly (string QualifiedName, string TypeLabel)[] MixedCandidates =
    [
        ("Model::Engine", "part def"),
        ("Model::Wheel", "part def"),
        ("Model::engineInstance", "part"),
        ("Model::wheelInstance", "part"),
        ("Model::SubPackage", "package"),
    ];

    /// <summary>
    ///     Validates that a freshly-constructed picker has no candidates, no active filters,
    ///     an empty (non-null) search text, and an empty displayed list.
    /// </summary>
    [Fact]
    public void Construction_HasEmptyInitialState()
    {
        // Act
        var picker = new ElementPickerViewModel();

        // Assert
        Assert.Empty(picker.AvailableTypeLabels);
        Assert.Empty(picker.ActiveTypeFilters);
        Assert.Empty(picker.DisplayedItems);
        Assert.Equal(string.Empty, picker.SearchText);
        Assert.Null(picker.SelectedQualifiedName);
    }

    /// <summary>
    ///     Validates that <see cref="ElementPickerViewModel.SetCandidates" /> throws
    ///     <see cref="ArgumentNullException" /> when handed a <see langword="null" />
    ///     candidate list, matching the runtime null-guard behavior.
    /// </summary>
    [Fact]
    public void SetCandidates_NullCandidates_Throws()
    {
        // Arrange
        var picker = new ElementPickerViewModel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => picker.SetCandidates(null!));
    }

    /// <summary>
    ///     Validates that <see cref="ElementPickerViewModel.AvailableTypeLabels" /> is
    ///     deduplicated and sorted ordinally after <see cref="ElementPickerViewModel.SetCandidates" />.
    /// </summary>
    [Fact]
    public void SetCandidates_AvailableTypeLabels_IsDistinctAndSorted()
    {
        // Arrange
        var picker = new ElementPickerViewModel();

        // Act
        picker.SetCandidates(MixedCandidates);

        // Assert
        Assert.Equal(new[] { "package", "part", "part def" }, picker.AvailableTypeLabels);
    }

    /// <summary>
    ///     Validates that <see cref="ElementPickerViewModel.SetCandidates" /> pre-populates
    ///     <see cref="ElementPickerViewModel.ActiveTypeFilters" /> with the requested
    ///     <c>defaultTypeFilterLabel</c> when it exists in the candidates.
    /// </summary>
    [Fact]
    public void SetCandidates_DefaultLabelPresent_PrepopulatesChip()
    {
        // Arrange
        var picker = new ElementPickerViewModel();

        // Act
        picker.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");

        // Assert
        Assert.Equal(new[] { "part" }, picker.ActiveTypeFilters);
    }

    /// <summary>
    ///     Validates that when the requested <c>defaultTypeFilterLabel</c> is absent from the
    ///     candidates, <see cref="ElementPickerViewModel.ActiveTypeFilters" /> starts empty
    ///     (rather than adding a chip for a label that filters out every candidate).
    /// </summary>
    [Fact]
    public void SetCandidates_DefaultLabelAbsent_LeavesChipsEmpty()
    {
        // Arrange
        var picker = new ElementPickerViewModel();

        // Act
        picker.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "not-a-real-label");

        // Assert
        Assert.Empty(picker.ActiveTypeFilters);
    }

    /// <summary>
    ///     Validates that with the default "part" chip active, only the "part" candidates are
    ///     surfaced by <see cref="ElementPickerViewModel.DisplayedItems" />.
    /// </summary>
    [Fact]
    public void DisplayedItems_DefaultPartChip_ShowsOnlyPartUsages()
    {
        // Arrange
        var picker = new ElementPickerViewModel();

        // Act
        picker.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");

        // Assert
        Assert.Equal(
            new[] { "Model::engineInstance", "Model::wheelInstance" },
            picker.DisplayedItems);
    }

    /// <summary>
    ///     Validates that with no active chips, every candidate is displayed regardless of
    ///     type label.
    /// </summary>
    [Fact]
    public void DisplayedItems_NoChips_ShowsAllCandidates()
    {
        // Arrange
        var picker = new ElementPickerViewModel();

        // Act
        picker.SetCandidates(MixedCandidates);

        // Assert
        Assert.Equal(MixedCandidates.Length, picker.DisplayedItems.Count);
    }

    /// <summary>
    ///     Validates the OR semantics of multiple chips: an item is shown when its type label
    ///     matches any of the active chips.
    /// </summary>
    [Fact]
    public void DisplayedItems_MultipleChips_AppliesOrSemantics()
    {
        // Arrange
        var picker = new ElementPickerViewModel();
        picker.SetCandidates(MixedCandidates);
        picker.AddTypeFilter("part def");
        picker.AddTypeFilter("package");

        // Act
        var displayed = picker.DisplayedItems;

        // Assert
        Assert.Contains("Model::Engine", displayed);
        Assert.Contains("Model::Wheel", displayed);
        Assert.Contains("Model::SubPackage", displayed);
        Assert.DoesNotContain("Model::engineInstance", displayed);
    }

    /// <summary>
    ///     Validates that <see cref="ElementPickerViewModel.SearchText" /> AND-combines with
    ///     any active chip filter: only items matching both the substring and the chip set are
    ///     displayed.
    /// </summary>
    [Fact]
    public void DisplayedItems_SearchText_AppliesAndSemanticsWithChips()
    {
        // Arrange
        var picker = new ElementPickerViewModel();
        picker.SetCandidates(MixedCandidates);
        picker.AddTypeFilter("part def");
        picker.AddTypeFilter("package");

        // Act
        picker.SearchText = "engine";

        // Assert - "part def" chip matches Engine + Wheel; "package" chip matches SubPackage.
        // After substring "engine", only Engine remains.
        Assert.Equal(new[] { "Model::Engine" }, picker.DisplayedItems);
    }

    /// <summary>
    ///     Validates that the search text is matched case-insensitively.
    /// </summary>
    [Fact]
    public void DisplayedItems_SearchText_IsCaseInsensitive()
    {
        // Arrange
        var picker = new ElementPickerViewModel();
        picker.SetCandidates(MixedCandidates);

        // Act
        picker.SearchText = "SUBPACKAGE";

        // Assert
        Assert.Equal(new[] { "Model::SubPackage" }, picker.DisplayedItems);
    }

    /// <summary>
    ///     Validates that <see cref="ElementPickerViewModel.AddTypeFilter" /> is dedupe-safe:
    ///     adding a label twice leaves only one chip.
    /// </summary>
    [Fact]
    public void AddTypeFilter_DuplicateLabel_KeepsSingleChip()
    {
        // Arrange
        var picker = new ElementPickerViewModel();
        picker.SetCandidates(MixedCandidates);

        // Act
        picker.AddTypeFilter("package");
        picker.AddTypeFilter("package");

        // Assert
        Assert.Single(picker.ActiveTypeFilters, "package");
    }

    /// <summary>
    ///     Validates that <see cref="ElementPickerViewModel.RemoveTypeFilter" /> removes a
    ///     present chip and is a no-op (aside from the recompute) for absent labels.
    /// </summary>
    [Fact]
    public void RemoveTypeFilter_PresentAndAbsentLabels_BehavesGracefully()
    {
        // Arrange
        var picker = new ElementPickerViewModel();
        picker.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");

        // Act
        picker.RemoveTypeFilter("part");

        // Assert
        Assert.Empty(picker.ActiveTypeFilters);

        // Act - removing something that never existed is a no-op
        picker.RemoveTypeFilter("not-a-real-label");

        // Assert
        Assert.Empty(picker.ActiveTypeFilters);
    }

    /// <summary>
    ///     Validates <see cref="ElementPickerViewModel.GetAddableTypeLabels" /> returns the
    ///     labels not currently active, preserving the master ordinal ordering.
    /// </summary>
    [Fact]
    public void GetAddableTypeLabels_ExcludesActiveChips()
    {
        // Arrange
        var picker = new ElementPickerViewModel();
        picker.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");

        // Act
        var addable = picker.GetAddableTypeLabels();

        // Assert
        Assert.Equal(new[] { "package", "part def" }, addable);
    }

    /// <summary>
    ///     Validates that <see cref="ElementPickerViewModel.SetCandidates" /> can be called
    ///     multiple times, and that the second call fully replaces the picker's state (chips
    ///     reset, prior selection cleared, displayed list recomputed).
    /// </summary>
    [Fact]
    public void SetCandidates_SecondCall_ReplacesState()
    {
        // Arrange
        var picker = new ElementPickerViewModel();
        picker.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");
        picker.SelectedQualifiedName = "Model::engineInstance";
        picker.SearchText = "engine";

        // Act
        picker.SearchText = null;
        picker.SetCandidates([("Other::Foo", "other")], defaultTypeFilterLabel: null);

        // Assert
        Assert.Equal(new[] { "other" }, picker.AvailableTypeLabels);
        Assert.Empty(picker.ActiveTypeFilters);
        Assert.Null(picker.SelectedQualifiedName);
        Assert.Equal(new[] { "Other::Foo" }, picker.DisplayedItems);
    }

    /// <summary>
    ///     Validates that <see cref="ElementPickerViewModel.SelectedQualifiedName" /> can be
    ///     freely assigned and read back, tracking the caller's selection.
    /// </summary>
    [Fact]
    public void SelectedQualifiedName_RoundTrips()
    {
        // Arrange
        var picker = new ElementPickerViewModel();
        picker.SetCandidates(MixedCandidates);

        // Act
        picker.SelectedQualifiedName = "Model::Wheel";

        // Assert
        Assert.Equal("Model::Wheel", picker.SelectedQualifiedName);
    }
}
