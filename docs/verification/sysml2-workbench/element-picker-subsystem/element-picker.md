### ElementPicker

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/ElementPickerViewModelTests.cs`
construct `ElementPickerViewModel` directly against inline candidate lists - it has no Avalonia,
`WorkspaceModel`, or shell dependency, so no UI thread or workspace fixture is required. Tests in
`ElementTypeLabelerTests.cs` construct `SysmlNode` fixtures directly (via their public
no-argument constructors and init properties) and assert against `ElementTypeLabeler.GetTypeLabel`. The scenario
list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/element-picker-subsystem/element-picker.yaml` and describes the
implemented tests in present tense. `ElementPickerView`'s code-behind (control wiring, chip-button
click handlers, "+" flyout) is not independently unit-tested: it only forwards to already-covered
view-model methods and to Avalonia's own binding infrastructure, verified in composition by
`AvaloniaTests.QueryDialog_RunDescribeAndCopyAsMarkdown_PlacesRenderedMarkdownOnClipboard` and by
the existing `ViewBuilderDialogViewModelTests` suite which drives the composed picker.

#### Test Environment

Tests run under the standard .NET test runner. No temporary workspace, no logging directory, and
no external services are required.

#### Acceptance Criteria

- All implemented tests in
  `test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/ElementPickerViewModelTests.cs`
  and `ElementTypeLabelerTests.cs` that correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/element-picker-subsystem/element-picker.yaml` using the real
  paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit
  assertion rather than a speculative or placeholder verification statement.

#### Test Scenarios

**Construction_HasEmptyInitialState**: A newly-constructed `ElementPickerViewModel` reports empty
`AvailableTypeLabels`, `DisplayedItems`, and `ActiveTypeFilters`, no `SearchText`, and no
`SelectedQualifiedName`. Verified by
`ElementPickerViewModelTests.Construction_HasEmptyInitialState`.

**SetCandidates_NullCandidates_Throws**: Calling `SetCandidates(null!)` throws
`ArgumentNullException`, matching the same defensive posture used elsewhere in the code base.
Verified by `ElementPickerViewModelTests.SetCandidates_NullCandidates_Throws`.

**SetCandidates_AvailableTypeLabels_IsDistinctAndSorted**: After `SetCandidates` is called with
duplicate type labels across candidates, `AvailableTypeLabels` contains each label exactly once and
is sorted ordinally. Verified by
`ElementPickerViewModelTests.SetCandidates_AvailableTypeLabels_IsDistinctAndSorted`.

**SetCandidates_DefaultLabelPresent_PrepopulatesChip**: When `SetCandidates` is called with a
`defaultTypeFilterLabel` that appears in the new candidates, `ActiveTypeFilters` is pre-populated
with exactly that one chip. Verified by
`ElementPickerViewModelTests.SetCandidates_DefaultLabelPresent_PrepopulatesChip`.

**SetCandidates_DefaultLabelAbsent_LeavesChipsEmpty**: When the `defaultTypeFilterLabel` is not
present in the new candidates (or is `null`), `ActiveTypeFilters` is left empty, applying no type
restriction by default. Verified by
`ElementPickerViewModelTests.SetCandidates_DefaultLabelAbsent_LeavesChipsEmpty`.

**SetCandidates_SecondCall_ReplacesState**: A second call to `SetCandidates` replaces the whole
candidate/labels/chips/selection state - the prior selection is cleared, the prior chip set is
recomputed, and only the new candidates appear in `DisplayedItems`. Verified by
`ElementPickerViewModelTests.SetCandidates_SecondCall_ReplacesState`.

**DisplayedItems_DefaultPartChip_ShowsOnlyPartUsages**: With the default `"part"` chip active,
`DisplayedItems` contains only the candidates whose type label is `"part"`. Verified by
`ElementPickerViewModelTests.DisplayedItems_DefaultPartChip_ShowsOnlyPartUsages`.

**DisplayedItems_NoChips_ShowsAllCandidates**: With no active chips, `DisplayedItems` contains
every candidate in the caller-supplied order. Verified by
`ElementPickerViewModelTests.DisplayedItems_NoChips_ShowsAllCandidates`.

**DisplayedItems_MultipleChips_AppliesOrSemantics**: Activating two chips shows candidates matching
either label while excluding a candidate whose label matches neither, confirming OR semantics
across chips. Verified by
`ElementPickerViewModelTests.DisplayedItems_MultipleChips_AppliesOrSemantics`.

**DisplayedItems_SearchText_AppliesAndSemanticsWithChips**: With chips active, setting `SearchText`
further narrows `DisplayedItems` to only the candidates matching both the type filter and the text
search, confirming AND semantics between the two filters. Verified by
`ElementPickerViewModelTests.DisplayedItems_SearchText_AppliesAndSemanticsWithChips`.

**DisplayedItems_SearchText_IsCaseInsensitive**: The search-text filter is case-insensitive against
qualified names. Verified by
`ElementPickerViewModelTests.DisplayedItems_SearchText_IsCaseInsensitive`.

**AddTypeFilter_DuplicateLabel_KeepsSingleChip**: Calling `AddTypeFilter` twice with the same label
results in exactly one chip in `ActiveTypeFilters`, confirming dedupe-safe behavior. Verified by
`ElementPickerViewModelTests.AddTypeFilter_DuplicateLabel_KeepsSingleChip`.

**RemoveTypeFilter_PresentAndAbsentLabels_BehavesGracefully**: Removing an active chip clears it,
and a subsequent removal of a label that is not currently active is a no-op rather than a throw.
Verified by `ElementPickerViewModelTests.RemoveTypeFilter_PresentAndAbsentLabels_BehavesGracefully`.

**GetAddableTypeLabels_ExcludesActiveChips**: `GetAddableTypeLabels()` returns
`AvailableTypeLabels` minus every currently-active chip, so the "+" add-flyout never re-offers a
label that is already active. Verified by
`ElementPickerViewModelTests.GetAddableTypeLabels_ExcludesActiveChips`.

**SelectedQualifiedName_RoundTrips**: Assigning `SelectedQualifiedName` and reading it back returns
the assigned value, providing a two-way observable seam for view bindings. Verified by
`ElementPickerViewModelTests.SelectedQualifiedName_RoundTrips`.

**GetTypeLabel_NullNode_Throws**: `ElementTypeLabeler.GetTypeLabel(null!)` throws
`ArgumentNullException`, matching the defensive posture of the other static helpers in this repo.
Verified by `ElementTypeLabelerTests.GetTypeLabel_NullNode_Throws`.

**GetTypeLabel_DefinitionNode_ReturnsDefinitionKeyword**: A `SysmlDefinitionNode` returns its
`DefinitionKeyword` verbatim (for example, `"part def"`). Verified by
`ElementTypeLabelerTests.GetTypeLabel_DefinitionNode_ReturnsDefinitionKeyword`.

**GetTypeLabel_FeatureNode_ReturnsFeatureKeyword**: A `SysmlFeatureNode` returns its
`FeatureKeyword` verbatim (for example, `"part"`). Verified by
`ElementTypeLabelerTests.GetTypeLabel_FeatureNode_ReturnsFeatureKeyword`.

**GetTypeLabel_ConnectionNode_ReturnsConnectionKeyword**: A `SysmlConnectionNode` returns its
`ConnectionKeyword` verbatim. Verified by
`ElementTypeLabelerTests.GetTypeLabel_ConnectionNode_ReturnsConnectionKeyword`.

**GetTypeLabel_FixedLiteralNode_ReturnsExpectedLiteral**: The fixed-literal node kinds
(`SysmlImportNode` → `"import"`, `SysmlPackageNode` → `"package"`, `SysmlViewNode` → `"view"`,
`SysmlViewpointNode` → `"viewpoint"`, `SysmlTransitionNode` → `"transition"`, `SysmlSatisfyNode` →
`"satisfy"`, `SysmlDependencyNode` → `"dependency"`) each return their canonical literal. Verified
by `ElementTypeLabelerTests.GetTypeLabel_FixedLiteralNode_ReturnsExpectedLiteral`.

**GetTypeLabel_UnknownSubtype_UsesFallbackFromTypeName**: A `SysmlNode` subtype that isn't in the
taxonomy table falls back to the runtime type name with the `"Sysml"` prefix and `"Node"` suffix
stripped and lowercased. Verified by
`ElementTypeLabelerTests.GetTypeLabel_UnknownSubtype_UsesFallbackFromTypeName`.
