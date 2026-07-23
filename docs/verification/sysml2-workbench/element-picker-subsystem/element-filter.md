### ElementFilter

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/ElementFilterViewModelTests.cs`
construct `ElementFilterViewModel` directly against inline candidate lists - it has no Avalonia,
`WorkspaceModel`, or shell dependency, so no UI thread or workspace fixture is required. The
scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/element-picker-subsystem/element-filter.yaml` and describes the
implemented tests in present tense, mirroring the equivalent `ElementPickerViewModelTests` scenarios
minus any selection-related assertion, since this view model has no selection concept.
`ElementFilterView`'s code-behind (control wiring, chip-button click handlers, "+" flyout) is not
independently unit-tested: it only forwards to already-covered view-model methods and to Avalonia's
own binding infrastructure, verified in composition by the same `AvaloniaTests` Query dialog
end-to-end test that exercises the embedded picker view, and by `QueryDialogViewModelTests`'s
"List" Query Type scenarios which drive a standalone `ElementFilterViewModel` (`FilterOnly`).

#### Test Environment

Tests run under the standard .NET test runner. No temporary workspace, no logging directory, and
no external services are required.

#### Acceptance Criteria

- All implemented tests in
  `test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/ElementFilterViewModelTests.cs`
  that correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/element-picker-subsystem/element-filter.yaml` using the real
  paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit
  assertion rather than a speculative or placeholder verification statement.

#### Test Scenarios

**Construction_HasEmptyInitialState**: A newly-constructed `ElementFilterViewModel` reports empty
`AvailableTypeLabels`, `DisplayedItems`, and `ActiveTypeFilters`, and no `SearchText`. Verified by
`ElementFilterViewModelTests.Construction_HasEmptyInitialState`.

**SetCandidates_NullCandidates_Throws**: Calling `SetCandidates(null!)` throws
`ArgumentNullException`, matching the same defensive posture used elsewhere in the code base.
Verified by `ElementFilterViewModelTests.SetCandidates_NullCandidates_Throws`.

**SetCandidates_AvailableTypeLabels_IsDistinctAndSorted**: After `SetCandidates` is called with
duplicate type labels across candidates, `AvailableTypeLabels` contains each label exactly once and
is sorted ordinally. Verified by
`ElementFilterViewModelTests.SetCandidates_AvailableTypeLabels_IsDistinctAndSorted`.

**SetCandidates_DefaultLabelPresent_PrepopulatesChip**: When `SetCandidates` is called with a
`defaultTypeFilterLabel` that appears in the new candidates, `ActiveTypeFilters` is pre-populated
with exactly that one chip. Verified by
`ElementFilterViewModelTests.SetCandidates_DefaultLabelPresent_PrepopulatesChip`.

**SetCandidates_DefaultLabelAbsent_LeavesChipsEmpty**: When the `defaultTypeFilterLabel` is not
present in the new candidates (or is `null`), `ActiveTypeFilters` is left empty, applying no type
restriction by default. Verified by
`ElementFilterViewModelTests.SetCandidates_DefaultLabelAbsent_LeavesChipsEmpty`.

**SetCandidates_SecondCall_ReplacesState**: A second call to `SetCandidates` replaces the whole
candidate/labels/chips state - the prior chip set is recomputed and only the new candidates appear
in `DisplayedItems`. Verified by `ElementFilterViewModelTests.SetCandidates_SecondCall_ReplacesState`.

**DisplayedItems_DefaultPartChip_ShowsOnlyPartUsages**: With the default `"part"` chip active,
`DisplayedItems` contains only the candidates whose type label is `"part"`. Verified by
`ElementFilterViewModelTests.DisplayedItems_DefaultPartChip_ShowsOnlyPartUsages`.

**DisplayedItems_NoChips_ShowsAllCandidates**: With no active chips, `DisplayedItems` contains
every candidate in the caller-supplied order. Verified by
`ElementFilterViewModelTests.DisplayedItems_NoChips_ShowsAllCandidates`.

**DisplayedItems_MultipleChips_AppliesOrSemantics**: Activating two chips shows candidates matching
either label while excluding a candidate whose label matches neither, confirming OR semantics
across chips. Verified by
`ElementFilterViewModelTests.DisplayedItems_MultipleChips_AppliesOrSemantics`.

**DisplayedItems_SearchText_AppliesAndSemanticsWithChips**: With chips active, setting `SearchText`
further narrows `DisplayedItems` to only the candidates matching both the type filter and the text
search, confirming AND semantics between the two filters. Verified by
`ElementFilterViewModelTests.DisplayedItems_SearchText_AppliesAndSemanticsWithChips`.

**DisplayedItems_SearchText_IsCaseInsensitive**: The search-text filter is case-insensitive against
qualified names. Verified by `ElementFilterViewModelTests.DisplayedItems_SearchText_IsCaseInsensitive`.

**AddTypeFilter_DuplicateLabel_KeepsSingleChip**: Calling `AddTypeFilter` twice with the same label
results in exactly one chip in `ActiveTypeFilters`, confirming dedupe-safe behavior. Verified by
`ElementFilterViewModelTests.AddTypeFilter_DuplicateLabel_KeepsSingleChip`.

**RemoveTypeFilter_PresentAndAbsentLabels_BehavesGracefully**: Removing an active chip clears it,
and a subsequent removal of a label that is not currently active is a no-op rather than a throw.
Verified by `ElementFilterViewModelTests.RemoveTypeFilter_PresentAndAbsentLabels_BehavesGracefully`.

**GetAddableTypeLabels_ExcludesActiveChips**: `GetAddableTypeLabels()` returns
`AvailableTypeLabels` minus every currently-active chip, so the "+" add-flyout never re-offers a
label that is already active. Verified by
`ElementFilterViewModelTests.GetAddableTypeLabels_ExcludesActiveChips`.

**BeginAddableTypeFilterSearch_ResetsSearchAndPopulatesFullSet**: Opening the "+" add-flyout
resets `AddableTypeFilterSearchText` to empty and populates `AddableTypeFilterCandidates` with the
full addable (not yet active) label set. Verified by
`ElementFilterViewModelTests.ElementFilterViewModel_BeginAddableTypeFilterSearch_ResetsSearchAndPopulatesFullSet`.

**AddableTypeFilterSearchText_NarrowsCandidatesCaseInsensitively**: Setting
`AddableTypeFilterSearchText` narrows `AddableTypeFilterCandidates` to only the addable labels whose
text contains the search text, case-insensitively. Verified by
`ElementFilterViewModelTests.ElementFilterViewModel_AddableTypeFilterSearchText_NarrowsCandidatesCaseInsensitively`.

**TryCommitAddableTypeFilterSearch_MatchFound_AddsChipAndReturnsTrue**: When the current search
narrows to at least one addable candidate, `TryCommitAddableTypeFilterSearch()` adds the top
matching label as a new chip in `ActiveTypeFilters` and returns `true`. Verified by
`ElementFilterViewModelTests.ElementFilterViewModel_TryCommitAddableTypeFilterSearch_MatchFound_AddsChipAndReturnsTrue`.

**TryCommitAddableTypeFilterSearch_NoMatch_ReturnsFalse**: When the current search matches no
addable candidate, `TryCommitAddableTypeFilterSearch()` leaves `ActiveTypeFilters` unchanged and
returns `false`. Verified by
`ElementFilterViewModelTests.ElementFilterViewModel_TryCommitAddableTypeFilterSearch_NoMatch_ReturnsFalse`.
