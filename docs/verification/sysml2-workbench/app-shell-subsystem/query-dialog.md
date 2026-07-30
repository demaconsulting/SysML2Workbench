### QueryDialog

#### Verification Approach

Tests in
`test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/QueryDialogViewModelTests.cs` construct
`QueryDialogViewModel` directly against a real (non-mocked) `MainWindowShell` and a temporary
on-disk workspace - it has no Avalonia dependency itself, so no UI thread or dialog `Window` is
needed to exercise its state, `BuildOptions` shape, or clipboard composition. This design has no
explicit "Run" method: every scenario drives the auto-recompute contract by assigning the property
that should trigger it (`SelectedQueryType`, `Picker.SelectedQualifiedName`, `HierarchyDirection`,
`WalkDepthText`, `IncludeConnections`, `IncludeStdlib`) and asserting `CurrentResult`/`StatusMessage`
update immediately,
with no intervening method call anywhere in the test bodies. The pure row-projection scenarios
instead call the public `BuildRow` function directly with a hand-built result entry, so the mapping
from engine metadata to displayed columns can be asserted exactly - including that the depth comes
from the entry's machine-readable value rather than from its human-readable detail text - without
depending on which entries a particular engine version happens to return. Two scenarios cannot be
reached from a view-model-only test at all, because the optional-column logic lives in the view's
code-behind and addresses the grid's columns by index; those live in
`test/DemaConsulting.SysML2Workbench.UiTests/AppShellSubsystem/QueryDialogUiTests.cs`, which shows
the real dialog `Window` on the Avalonia headless platform. A single Avalonia-headless end-to-end
test in `test/OtsSoftwareTests/AvaloniaTests.cs` opens the real dialog through the real Query menu
item, confirms the dialog opens on "List" with the selection-free filter control visible and the
element picker hidden, selects the Describe Query Type (confirming the visibility flip) and an
element on the now-visible picker, and right-clicks the results panel to invoke "Copy as Markdown"
through its context menu, asserting the rendered Markdown reached the headless platform's real
clipboard. The scenario list below follows
the authoritative mappings in
`docs/reqstream/sysml2-workbench/app-shell-subsystem/query-dialog.yaml` and describes the
implemented tests in present tense. `QueryDialogView`'s code-behind (control wiring, Query-Type
combo/panel visibility toggles, and the close button handler) is not independently unit-tested: it
only forwards to already-covered view-model methods and to Avalonia's own binding infrastructure,
verified in composition by the end-to-end scenario below.

#### Test Environment

View-model tests run under the standard .NET test runner, using a temporary workspace folder and
log directory plus real collaborator units (`WorkspaceModel`, `FileWatcher`,
`DiagnosticsAggregator`, `ViewCatalogPresenter`, `LayoutInvoker`, `DiagnosticsListView`,
`SysmlSnippetGenerator`, `RollingFileLogger`). The end-to-end test runs under the standard
`Avalonia.Headless.XUnit` platform. No external services are required.

#### Acceptance Criteria

- All implemented tests in
  `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/QueryDialogViewModelTests.cs` and
  the single referenced test in `test/OtsSoftwareTests/AvaloniaTests.cs` that correspond to the
  scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/app-shell-subsystem/query-dialog.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit
  assertion rather than a speculative or placeholder verification statement.

#### Test Scenarios

**Construction_EmptyShell_ReportsWorkspaceEmpty**: Constructing the view model over a zero-source
shell reports `IsWorkspaceEmpty` as `true`, defaults `SelectedQueryType` to `List`, and leaves
`FilterOnly` empty (with a non-null, empty client-side list result). Verified by
`QueryDialogViewModelTests.Construction_EmptyShell_ReportsWorkspaceEmpty`.

**Construction_LoadedWorkspace_PopulatesSinglePicker**: Constructing the view model over a
workspace with declarations populates both `FilterOnly` and `Picker` with those declarations (the
same candidate list is used for both) and reports `IsWorkspaceEmpty` as `false`. Verified by
`QueryDialogViewModelTests.Construction_LoadedWorkspace_PopulatesSinglePicker`.

**ListQueryType_BuildsClientSideListResult**: With `SelectedQueryType=List`, `CurrentResult` is a
client-built `QueryResult` with `Verb="list"`, no target `Element`, and one entry per
`DisplayedItem`, without calling `QueryEngine.List` or `QueryEngine.Find`. Verified by
`QueryDialogViewModelTests.ListQueryType_BuildsClientSideListResult`.

**ListQueryType_SearchTextEdit_RegeneratesResultLive**: Editing `FilterOnly`'s search text while
`List` is selected recomputes `CurrentResult` and `CurrentResultRows` live, with no explicit "Run"
gesture. Verified by
`QueryDialogViewModelTests.ListQueryType_SearchTextEdit_RegeneratesResultLive`.

**RecomputeResult_ElementScopedVerbNoSelection_ReportsPromptAndClearsRows**: Selecting an
element-scoped Query Type (e.g. `Describe`) with the picker's `SelectedQualifiedName` unset reports
a helpful, non-error `StatusMessage` naming the Query Type, and clears `CurrentResult`/
`CurrentResultRows` rather than leaving a stale prior result. Verified by
`QueryDialogViewModelTests.RecomputeResult_ElementScopedVerbNoSelection_ReportsPromptAndClearsRows`.

**RecomputeResult_EmptyWorkspace_ReportsStatusMessage**: Selecting an element-scoped Query Type
against a zero-source shell reports the failure through a non-null `StatusMessage` and does not
throw. Verified by
`QueryDialogViewModelTests.RecomputeResult_EmptyWorkspace_ReportsStatusMessage`.

**RecomputeResult_DescribeWithSelection_DispatchesThroughEngineImmediately**: Selecting
`SelectedQueryType=Describe` and then assigning a qualified name to `Picker.SelectedQualifiedName`
immediately produces a `CurrentResult` whose `Verb` is `"describe"` and whose target `Element`
matches the selection - with no intervening method call - proving the query dispatched through
`QueryEngine.Execute` via the auto-recompute contract alone. Verified by
`QueryDialogViewModelTests.RecomputeResult_DescribeWithSelection_DispatchesThroughEngineImmediately`.

**SwitchingQueryType_FromDescribeToList_ShowsListResultImmediately**: After a Describe result is
showing, switching `SelectedQueryType` to `List` immediately shows the client-side list result
instead of the stale Describe result. Verified by
`QueryDialogViewModelTests.SwitchingQueryType_FromDescribeToList_ShowsListResultImmediately`.

**SwitchingQueryType_FromListToDescribeNoSelection_ShowsSelectPrompt**: From the default `List`
state (which always has a result), switching to `SelectedQueryType=Describe` with no picker
selection immediately shows the "select an element" prompt rather than a stale `List` result or a
thrown exception. Verified by
`QueryDialogViewModelTests.SwitchingQueryType_FromListToDescribeNoSelection_ShowsSelectPrompt`.

**HierarchyDirectionChange_WithSelection_RecomputesImmediately**: With `SelectedQueryType=Hierarchy`
and an active selection, changing `HierarchyDirection` recomputes `CurrentResult` immediately with
no stale state or thrown exception. Verified by
`QueryDialogViewModelTests.HierarchyDirectionChange_WithSelection_RecomputesImmediately`.

**WalkDepthTextChange_WithSelection_RecomputesImmediately**: With `SelectedQueryType=Impact` and an
active selection, editing `WalkDepthText` recomputes `CurrentResult` immediately. Verified by
`QueryDialogViewModelTests.WalkDepthTextChange_WithSelection_RecomputesImmediately`.

**BuildOptions_HierarchyVerb_AttachesDirection**: With `SelectedQueryType=Hierarchy`, `BuildOptions`
attaches the current `HierarchyDirection` value to `QueryOptions.Direction`. Verified by
`QueryDialogViewModelTests.BuildOptions_HierarchyVerb_AttachesDirection`.

**BuildOptions_NonHierarchyVerb_OmitsDirection**: With any Query Type other than Hierarchy,
`BuildOptions` leaves `QueryOptions.Direction` `null`, matching the engine's expectation. Verified
by `QueryDialogViewModelTests.BuildOptions_NonHierarchyVerb_OmitsDirection`.

**BuildOptions_ImpactVerbWithWalkDepth_ParsesWalkDepth**: With `SelectedQueryType=Impact` and
`WalkDepthText` set to a parseable non-negative integer, `BuildOptions` sets `QueryOptions.WalkDepth`
to that integer. Verified by
`QueryDialogViewModelTests.BuildOptions_ImpactVerbWithWalkDepth_ParsesWalkDepth`.

**BuildOptions_ImpactVerbWithInvalidWalkDepth_LeavesNull**: With `SelectedQueryType=Impact` and
`WalkDepthText` that does not parse (blank, non-numeric, negative), `BuildOptions` leaves
`QueryOptions.WalkDepth` `null`, so a typo does not silently coerce to zero. Verified by
`QueryDialogViewModelTests.BuildOptions_ImpactVerbWithInvalidWalkDepth_LeavesNull`.

**BuildOptions_PropagatesIncludeStdlib**: `BuildOptions` always carries the current `IncludeStdlib`
flag to `QueryOptions.IncludeStdlib`, regardless of Query Type. Verified by
`QueryDialogViewModelTests.BuildOptions_PropagatesIncludeStdlib`.

**BuildOptions_ImpactVerbWithIncludeConnections_SetsIncludeConnections**: With
`SelectedQueryType=Impact` and `IncludeConnections` set, `BuildOptions` requests connection-aware
impact analysis, so the user's opt-in to connector (`connect`/`bind`) traversal actually reaches the
engine. Verified by
`QueryDialogViewModelTests.BuildOptions_ImpactVerbWithIncludeConnections_SetsIncludeConnections`.

**BuildOptions_NonImpactVerb_OmitsIncludeConnections**: With any Query Type other than Impact
(exercised over Describe, Uses, and Hierarchy), `BuildOptions` leaves connection awareness unset even
when the checkbox was left ticked from a prior Impact query, so switching Query Type never changes
another verb's option payload. Verified by
`QueryDialogViewModelTests.BuildOptions_NonImpactVerb_OmitsIncludeConnections`.

**IncludeConnectionsChange_WithSelection_RecomputesImmediately**: With `SelectedQueryType=Impact` and
an active selection, toggling `IncludeConnections` recomputes `CurrentResult` immediately, with no
intervening method call and no stale state or thrown exception. Verified by
`QueryDialogViewModelTests.IncludeConnectionsChange_WithSelection_RecomputesImmediately`.

**BuildRow_TraversalEntry_ProjectsDepthRelationAndVia**: A result entry carrying a traversal depth, a
resolved relation kind, and a connection roll-up far endpoint projects all three into the displayed
row (as `"2"`, `"Connect"`, and the far endpoint's qualified name). The entry's detail text
deliberately claims a different depth, so an implementation that parsed the depth out of the detail
text instead of reading the engine's machine-readable value would fail this scenario. Verified by
`QueryDialogViewModelTests.BuildRow_TraversalEntry_ProjectsDepthRelationAndVia`.

**BuildRow_NonTraversingEntry_LeavesMetadataEmpty**: A result entry from a verb that does not
traverse projects empty strings (never the text `"null"`) for depth, relation, and far endpoint, and
a null notes tooltip - the regression guard that keeps the data-driven column visibility hiding all
three and the results panel rendering exactly as it did before those columns existed. Verified by
`QueryDialogViewModelTests.BuildRow_NonTraversingEntry_LeavesMetadataEmpty`.

**QueryDialogView_TraversalColumns_AreShownOnlyWhenRowsCarryThoseValues**: The real dialog `Window`
is shown on the Avalonia headless platform and given first a result whose rows carry no traversal
metadata, then one carrying a depth and a relation but no roll-up far endpoint. The Depth, Relation,
and Via columns stay hidden for the first, and for the second only Depth and Relation appear - so
each column is proven to track its own value independently rather than switching as a group. This
covers the requirement's display clause, which the row-projection scenarios above cannot reach
because the visibility logic lives in the view's code-behind. Verified by
`QueryDialogUiTests.QueryDialogView_TraversalColumns_AreShownOnlyWhenRowsCarryThoseValues`.

**QueryDialogView_EntriesDataGrid_DeclaresColumnsInIndexOrderCodeBehindAssumes**: The results grid's
column headers are asserted to appear in the exact order the code-behind assumes when it toggles
columns positionally. `DataGridColumn` does not participate in Avalonia's compiled-bindings field
generation, so the code-behind has no alternative to index-based access; a reordering or insertion in
the AXAML would otherwise silently toggle the wrong column, raising no exception and producing no
binding error. This scenario converts that failure mode into a test failure. Verified by
`QueryDialogUiTests.QueryDialogView_EntriesDataGrid_DeclaresColumnsInIndexOrderCodeBehindAssumes`.

**IncludeStdlibToggle_RefreshesPickerAndRecomputesResult**: Toggling `IncludeStdlib` refreshes both
`FilterOnly`'s and `Picker`'s candidate lists to reflect the new stdlib-inclusion rule, and
recomputes the current result (clearing a now-unselected Describe result to the select-element
prompt rather than leaving it stale, then confirming the pipeline still functions correctly after
re-selecting). Verified by
`QueryDialogViewModelTests.IncludeStdlibToggle_RefreshesPickerAndRecomputesResult`.

**QueryTypes_HasExpectedElevenEntries**: The static `QueryTypes` list contains exactly the eleven
Query Type options (`List` first, followed by the ten element-scoped `QueryVerb` values) and never
contains the workspace-scoped `Find` verb, which "List" always merges into instead. Verified by
`QueryDialogViewModelTests.QueryTypes_HasExpectedElevenEntries`.

**HierarchyDirectionOptions_HasExpectedThree**: The static `HierarchyDirectionOptions` list contains
exactly `"up"`, `"down"`, and `"both"`. Verified by
`QueryDialogViewModelTests.HierarchyDirectionOptions_HasExpectedThree`.

**CopyResultAsMarkdownAsync_WritesRenderedMarkdownToClipboard**: With a non-null `CurrentResult` and
a stub `IClipboardService`, `CopyResultAsMarkdownAsync` writes the concatenated
`QueryResultRenderer.RenderMarkdown` output to the clipboard. Verified by
`QueryDialogViewModelTests.CopyResultAsMarkdownAsync_WritesRenderedMarkdownToClipboard`.

**CopyResultAsJsonAsync_WritesRenderedJsonToClipboard**: With a non-null `CurrentResult` and a stub
`IClipboardService`, `CopyResultAsJsonAsync` writes the `QueryResultRenderer.RenderJson` output to
the clipboard. Verified by
`QueryDialogViewModelTests.CopyResultAsJsonAsync_WritesRenderedJsonToClipboard`.

**CopyMethods_NoResult_AreNoOps**: With `CurrentResult=null` (reached here via the natural
no-selection path for an element-scoped Query Type), both copy methods are safe no-ops - they do
not touch the clipboard and do not throw. Verified by
`QueryDialogViewModelTests.CopyMethods_NoResult_AreNoOps`.

**QueryDialog_SelectDescribeAndCopyAsMarkdown_PlacesRenderedMarkdownOnClipboard**: An
Avalonia-headless end-to-end scenario that opens the main window, clicks the Query menu's "Run
Query..." item, selects "Describe" on the single form's Query Type combo, selects a qualified name
on the one always-visible picker (producing the result immediately, with no "Run" gesture),
right-clicks the results panel to open its context menu, clicks "Copy as Markdown", and asserts
that the headless platform's real clipboard now contains the expected rendered Markdown. Verified
by
`AvaloniaTests.QueryDialog_SelectDescribeAndCopyAsMarkdown_PlacesRenderedMarkdownOnClipboard`.
