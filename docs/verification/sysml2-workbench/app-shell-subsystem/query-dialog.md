### QueryDialog

#### Verification Approach

Tests in
`test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/QueryDialogViewModelTests.cs` construct
`QueryDialogViewModel` directly against a real (non-mocked) `MainWindowShell` and a temporary
on-disk workspace - it has no Avalonia dependency itself, so no UI thread or dialog `Window` is
needed to exercise its state, `BuildOptions` shape, `RunElementQuery` dispatch, or clipboard
composition. A single Avalonia-headless end-to-end test in
`test/OtsSoftwareTests/AvaloniaTests.cs` opens the real dialog through the real Query menu item,
runs the Describe verb end-to-end, and asserts the rendered Markdown reached the headless
platform's real clipboard. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/app-shell-subsystem/query-dialog.yaml` and describes the
implemented tests in present tense. `QueryDialogView`'s code-behind (control wiring, tab-index
management, verb-panel visibility toggles, and the OK/close button handlers) is not independently
unit-tested: it only forwards to already-covered view-model methods and to Avalonia's own binding
infrastructure, verified in composition by the end-to-end scenario below.

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
shell reports `IsWorkspaceEmpty` as `true` and leaves both pickers empty. Verified by
`QueryDialogViewModelTests.Construction_EmptyShell_ReportsWorkspaceEmpty`.

**Construction_LoadedWorkspace_PopulatesBothPickers**: Constructing the view model over a workspace
with declarations populates the Browse and Element Query pickers with those declarations and
reports `IsWorkspaceEmpty` as `false`. Verified by
`QueryDialogViewModelTests.Construction_LoadedWorkspace_PopulatesBothPickers`.

**IncludeStdlibToggle_RefreshesBothPickers**: Toggling `IncludeStdlib` refreshes both pickers'
candidate lists to reflect the new stdlib-inclusion rule. Verified by
`QueryDialogViewModelTests.IncludeStdlibToggle_RefreshesBothPickers`.

**BrowseTab_BuildsClientSideListResult**: The Browse tab's `CurrentResult` is a client-built
`QueryResult` with `Verb="list"`, no target `Element`, and one entry per `DisplayedItem`, without
calling `QueryEngine.List` or `QueryEngine.Find`. Verified by
`QueryDialogViewModelTests.BrowseTab_BuildsClientSideListResult`.

**BrowseTab_SearchTextEdit_RegeneratesResultLive**: Editing the Browse picker's search text
recomputes `CurrentResult` and `CurrentResultRows` live without an explicit "Run" click. Verified
by `QueryDialogViewModelTests.BrowseTab_SearchTextEdit_RegeneratesResultLive`.

**RunElementQuery_NoSelection_ReportsStatusMessage**: Clicking Run with the Element Query picker's
`SelectedQualifiedName` unset reports the failure through a non-null `StatusMessage`, does not
throw, and leaves any prior `CurrentResult` unchanged. Verified by
`QueryDialogViewModelTests.RunElementQuery_NoSelection_ReportsStatusMessage`.

**RunElementQuery_EmptyWorkspace_ReportsStatusMessage**: Clicking Run against a zero-source shell
reports the failure through a non-null `StatusMessage` and does not throw. Verified by
`QueryDialogViewModelTests.RunElementQuery_EmptyWorkspace_ReportsStatusMessage`.

**RunElementQuery_Describe_DispatchesThroughEngine**: Selecting a qualified name in the picker,
setting `SelectedVerb=Describe`, and calling `RunElementQuery` produces a `CurrentResult` whose
`Verb` is `"describe"` and whose target `Element` matches the selection - i.e. the query really
went through `QueryEngine.Execute`. Verified by
`QueryDialogViewModelTests.RunElementQuery_Describe_DispatchesThroughEngine`.

**BuildOptions_HierarchyVerb_AttachesDirection**: With `SelectedVerb=Hierarchy`, `BuildOptions`
attaches the current `HierarchyDirection` value to `QueryOptions.Direction`. Verified by
`QueryDialogViewModelTests.BuildOptions_HierarchyVerb_AttachesDirection`.

**BuildOptions_NonHierarchyVerb_OmitsDirection**: With any verb other than Hierarchy, `BuildOptions`
leaves `QueryOptions.Direction` `null`, matching the engine's expectation. Verified by
`QueryDialogViewModelTests.BuildOptions_NonHierarchyVerb_OmitsDirection`.

**BuildOptions_ImpactVerbWithWalkDepth_ParsesWalkDepth**: With `SelectedVerb=Impact` and
`WalkDepthText` set to a parseable non-negative integer, `BuildOptions` sets `QueryOptions.WalkDepth`
to that integer. Verified by
`QueryDialogViewModelTests.BuildOptions_ImpactVerbWithWalkDepth_ParsesWalkDepth`.

**BuildOptions_ImpactVerbWithInvalidWalkDepth_LeavesNull**: With `SelectedVerb=Impact` and
`WalkDepthText` that does not parse (blank, non-numeric, negative), `BuildOptions` leaves
`QueryOptions.WalkDepth` `null`, so a typo does not silently coerce to zero. Verified by
`QueryDialogViewModelTests.BuildOptions_ImpactVerbWithInvalidWalkDepth_LeavesNull`.

**BuildOptions_PropagatesIncludeStdlib**: `BuildOptions` always carries the current `IncludeStdlib`
flag to `QueryOptions.IncludeStdlib`, regardless of verb. Verified by
`QueryDialogViewModelTests.BuildOptions_PropagatesIncludeStdlib`.

**ElementScopedVerbs_HasExpectedTenVerbs**: The static `ElementScopedVerbs` list contains exactly
the ten element-scoped `QueryVerb` values (`Uses`, `UsedBy`, `Dependencies`, `Impact`, `Describe`,
`Hierarchy`, `Requirements`, `Interface`, `Connections`, `States`) and does not contain the
workspace-scoped `List` or `Find` verbs. Verified by
`QueryDialogViewModelTests.ElementScopedVerbs_HasExpectedTenVerbs`.

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

**CopyMethods_NoResult_AreNoOps**: With `CurrentResult=null`, both copy methods are safe no-ops -
they do not touch the clipboard and do not throw. Verified by
`QueryDialogViewModelTests.CopyMethods_NoResult_AreNoOps`.

**QueryDialog_RunDescribeAndCopyAsMarkdown_PlacesRenderedMarkdownOnClipboard**: An Avalonia-headless
end-to-end scenario that opens the main window, clicks the Query menu's "Run Query..." item,
switches to the Element Query tab, selects a qualified name, runs the Describe verb, clicks "Copy
as Markdown", and asserts that the headless platform's real clipboard now contains the expected
rendered Markdown. Verified by
`AvaloniaTests.QueryDialog_RunDescribeAndCopyAsMarkdown_PlacesRenderedMarkdownOnClipboard`.
