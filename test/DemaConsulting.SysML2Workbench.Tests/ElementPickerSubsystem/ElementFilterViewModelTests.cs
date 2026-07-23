using DemaConsulting.SysML2Workbench.ElementPickerSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.ElementPickerSubsystem;

/// <summary>
///     Unit-tests the reusable, selection-free <see cref="ElementFilterViewModel" /> in isolation,
///     without any <c>MainWindowShell</c>, workspace, or Avalonia dependency. Every scenario
///     constructs a small hand-built candidate list (qualified name + type label) so the expected
///     filtering semantics can be verified precisely. Mirrors
///     <c>ElementPickerViewModelTests</c>'s style, minus any selection-related scenario since this
///     view model has no selection concept.
/// </summary>
public sealed class ElementFilterViewModelTests
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
    ///     Validates that a freshly-constructed filter has no candidates, no active filters,
    ///     an empty (non-null) search text, and an empty displayed list.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_Construction_HasEmptyInitialState()
    {
        // Act
        var filter = new ElementFilterViewModel();

        // Assert
        Assert.Empty(filter.AvailableTypeLabels);
        Assert.Empty(filter.ActiveTypeFilters);
        Assert.Empty(filter.DisplayedItems);
        Assert.Equal(string.Empty, filter.SearchText);
    }

    /// <summary>
    ///     Validates that <see cref="ElementFilterViewModel.SetCandidates" /> throws
    ///     <see cref="ArgumentNullException" /> when handed a <see langword="null" />
    ///     candidate list, matching the runtime null-guard behavior.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_SetCandidates_NullCandidates_Throws()
    {
        // Arrange
        var filter = new ElementFilterViewModel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => filter.SetCandidates(null!));
    }

    /// <summary>
    ///     Validates that <see cref="ElementFilterViewModel.AvailableTypeLabels" /> is
    ///     deduplicated and sorted ordinally after <see cref="ElementFilterViewModel.SetCandidates" />.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_SetCandidates_AvailableTypeLabels_IsDistinctAndSorted()
    {
        // Arrange
        var filter = new ElementFilterViewModel();

        // Act
        filter.SetCandidates(MixedCandidates);

        // Assert
        Assert.Equal(new[] { "package", "part", "part def" }, filter.AvailableTypeLabels);
    }

    /// <summary>
    ///     Validates that <see cref="ElementFilterViewModel.SetCandidates" /> pre-populates
    ///     <see cref="ElementFilterViewModel.ActiveTypeFilters" /> with the requested
    ///     <c>defaultTypeFilterLabel</c> when it exists in the candidates.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_SetCandidates_DefaultLabelPresent_PrepopulatesChip()
    {
        // Arrange
        var filter = new ElementFilterViewModel();

        // Act
        filter.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");

        // Assert
        Assert.Equal(new[] { "part" }, filter.ActiveTypeFilters);
    }

    /// <summary>
    ///     Validates that when the requested <c>defaultTypeFilterLabel</c> is absent from the
    ///     candidates, <see cref="ElementFilterViewModel.ActiveTypeFilters" /> starts empty
    ///     (rather than adding a chip for a label that filters out every candidate).
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_SetCandidates_DefaultLabelAbsent_LeavesChipsEmpty()
    {
        // Arrange
        var filter = new ElementFilterViewModel();

        // Act
        filter.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "not-a-real-label");

        // Assert
        Assert.Empty(filter.ActiveTypeFilters);
    }

    /// <summary>
    ///     Validates that with the default "part" chip active, only the "part" candidates are
    ///     surfaced by <see cref="ElementFilterViewModel.DisplayedItems" />.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_DisplayedItems_DefaultPartChip_ShowsOnlyPartUsages()
    {
        // Arrange
        var filter = new ElementFilterViewModel();

        // Act
        filter.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");

        // Assert
        Assert.Equal(
            new[] { "Model::engineInstance", "Model::wheelInstance" },
            filter.DisplayedItems);
    }

    /// <summary>
    ///     Validates that with no active chips, every candidate is displayed regardless of
    ///     type label.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_DisplayedItems_NoChips_ShowsAllCandidates()
    {
        // Arrange
        var filter = new ElementFilterViewModel();

        // Act
        filter.SetCandidates(MixedCandidates);

        // Assert
        Assert.Equal(MixedCandidates.Length, filter.DisplayedItems.Count);
    }

    /// <summary>
    ///     Validates the OR semantics of multiple chips: an item is shown when its type label
    ///     matches any of the active chips.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_DisplayedItems_MultipleChips_AppliesOrSemantics()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates);
        filter.AddTypeFilter("part def");
        filter.AddTypeFilter("package");

        // Act
        var displayed = filter.DisplayedItems;

        // Assert
        Assert.Contains("Model::Engine", displayed);
        Assert.Contains("Model::Wheel", displayed);
        Assert.Contains("Model::SubPackage", displayed);
        Assert.DoesNotContain("Model::engineInstance", displayed);
    }

    /// <summary>
    ///     Validates that <see cref="ElementFilterViewModel.SearchText" /> AND-combines with
    ///     any active chip filter: only items matching both the substring and the chip set are
    ///     displayed.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_DisplayedItems_SearchText_AppliesAndSemanticsWithChips()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates);
        filter.AddTypeFilter("part def");
        filter.AddTypeFilter("package");

        // Act
        filter.SearchText = "engine";

        // Assert - "part def" chip matches Engine + Wheel; "package" chip matches SubPackage.
        // After substring "engine", only Engine remains.
        Assert.Equal(new[] { "Model::Engine" }, filter.DisplayedItems);
    }

    /// <summary>
    ///     Validates that the search text is matched case-insensitively.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_DisplayedItems_SearchText_IsCaseInsensitive()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates);

        // Act
        filter.SearchText = "SUBPACKAGE";

        // Assert
        Assert.Equal(new[] { "Model::SubPackage" }, filter.DisplayedItems);
    }

    /// <summary>
    ///     Validates that <see cref="ElementFilterViewModel.AddTypeFilter" /> is dedupe-safe:
    ///     adding a label twice leaves only one chip.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_AddTypeFilter_DuplicateLabel_KeepsSingleChip()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates);

        // Act
        filter.AddTypeFilter("package");
        filter.AddTypeFilter("package");

        // Assert
        Assert.Single(filter.ActiveTypeFilters, "package");
    }

    /// <summary>
    ///     Validates that <see cref="ElementFilterViewModel.RemoveTypeFilter" /> removes a
    ///     present chip and is a no-op (aside from the recompute) for absent labels.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_RemoveTypeFilter_PresentAndAbsentLabels_BehavesGracefully()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");

        // Act
        filter.RemoveTypeFilter("part");

        // Assert
        Assert.Empty(filter.ActiveTypeFilters);

        // Act - removing something that never existed is a no-op
        filter.RemoveTypeFilter("not-a-real-label");

        // Assert
        Assert.Empty(filter.ActiveTypeFilters);
    }

    /// <summary>
    ///     Validates <see cref="ElementFilterViewModel.GetAddableTypeLabels" /> returns the
    ///     labels not currently active, preserving the master ordinal ordering.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_GetAddableTypeLabels_ExcludesActiveChips()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");

        // Act
        var addable = filter.GetAddableTypeLabels();

        // Assert
        Assert.Equal(new[] { "package", "part def" }, addable);
    }

    /// <summary>
    ///     Validates <see cref="ElementFilterViewModel.BeginAddableTypeFilterSearch" /> resets the
    ///     search text to empty and populates <see cref="ElementFilterViewModel.AddableTypeFilterCandidates" />
    ///     with the full addable set (mirroring <see cref="ElementFilterViewModel.GetAddableTypeLabels" />)
    ///     each time the add-filter flyout opens, discarding any leftover search text from a previous
    ///     opening.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_BeginAddableTypeFilterSearch_ResetsSearchAndPopulatesFullSet()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");
        filter.AddableTypeFilterSearchText = "leftover search text";

        // Act
        filter.BeginAddableTypeFilterSearch();

        // Assert
        Assert.Equal("", filter.AddableTypeFilterSearchText);
        Assert.Equal(new[] { "package", "part def" }, filter.AddableTypeFilterCandidates);
    }

    /// <summary>
    ///     Validates that setting <see cref="ElementFilterViewModel.AddableTypeFilterSearchText" />
    ///     narrows <see cref="ElementFilterViewModel.AddableTypeFilterCandidates" /> with a
    ///     case-insensitive substring match, mirroring <see cref="ElementFilterViewModel.SearchText" />'s
    ///     existing substring-match semantics for the main displayed list.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_AddableTypeFilterSearchText_NarrowsCandidatesCaseInsensitively()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates);
        filter.BeginAddableTypeFilterSearch();

        // Act
        filter.AddableTypeFilterSearchText = "DEF";

        // Assert
        Assert.Equal(new[] { "part def" }, filter.AddableTypeFilterCandidates);
    }

    /// <summary>
    ///     Validates <see cref="ElementFilterViewModel.TryCommitAddableTypeFilterSearch" /> adds the
    ///     current search's single matching candidate as a new active filter chip and returns
    ///     <see langword="true" />.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_TryCommitAddableTypeFilterSearch_MatchFound_AddsChipAndReturnsTrue()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates);
        filter.BeginAddableTypeFilterSearch();
        filter.AddableTypeFilterSearchText = "package";

        // Act
        var committed = filter.TryCommitAddableTypeFilterSearch();

        // Assert
        Assert.True(committed);
        Assert.Contains("package", filter.ActiveTypeFilters);
    }

    /// <summary>
    ///     Validates <see cref="ElementFilterViewModel.TryCommitAddableTypeFilterSearch" /> is a no-op
    ///     that returns <see langword="false" /> when the current search text matches no addable
    ///     label.
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_TryCommitAddableTypeFilterSearch_NoMatch_ReturnsFalse()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates);
        filter.BeginAddableTypeFilterSearch();
        filter.AddableTypeFilterSearchText = "not-a-real-label";

        // Act
        var committed = filter.TryCommitAddableTypeFilterSearch();

        // Assert
        Assert.False(committed);
        Assert.Empty(filter.ActiveTypeFilters);
    }

    /// <summary>
    ///     Validates that <see cref="ElementFilterViewModel.SetCandidates" /> can be called
    ///     multiple times, and that the second call fully replaces the filter's state (chips
    ///     reset, displayed list recomputed).
    /// </summary>
    [Fact]
    public void ElementFilterViewModel_SetCandidates_SecondCall_ReplacesState()
    {
        // Arrange
        var filter = new ElementFilterViewModel();
        filter.SetCandidates(MixedCandidates, defaultTypeFilterLabel: "part");
        filter.SearchText = "engine";

        // Act
        filter.SearchText = null;
        filter.SetCandidates([("Other::Foo", "other")], defaultTypeFilterLabel: null);

        // Assert
        Assert.Equal(new[] { "other" }, filter.AvailableTypeLabels);
        Assert.Empty(filter.ActiveTypeFilters);
        Assert.Equal(new[] { "Other::Foo" }, filter.DisplayedItems);
    }
}
