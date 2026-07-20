### ViewBuilderDialog

#### Verification Approach

Tests in
`test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/ViewBuilderDialogViewModelTests.cs` construct
`ViewBuilderDialogViewModel` directly against a real (non-mocked) `MainWindowShell` and a temporary on-disk
workspace - it has no Avalonia dependency itself, so no UI thread or dialog `Window` is needed to exercise its
composition and commit logic. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/app-shell-subsystem/view-builder-dialog.yaml` and describes the implemented tests
in present tense. `ViewBuilderDialogView`'s code-behind (control wiring, the OK/Cancel button click handlers) is
not independently unit-tested: it only forwards to the already-covered view model methods and calls `Close()`,
verified by code review rather than a dedicated Avalonia UI-automation test, matching the precedent set by
`about-dialog.md`.

#### Test Environment

Tests run under the standard .NET test runner, using a temporary workspace folder and log directory plus real
collaborator units (`WorkspaceModel`, `FileWatcher`, `DiagnosticsAggregator`, `ViewCatalogPresenter`,
`LayoutInvoker`, `DiagnosticsListView`, `SysmlSnippetGenerator`, `RollingFileLogger`). No external services are
required.

#### Acceptance Criteria

- All implemented tests in
  `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/ViewBuilderDialogViewModelTests.cs` that correspond
  to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/app-shell-subsystem/view-builder-dialog.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**Construction_RefreshesAvailableExposeTargetsFromWorkspace**: Constructing the view model over a workspace with
two elements available to expose populates `AvailableExposeTargets` with both qualified names and reports
`IsWorkspaceEmpty` as `false`. Verified by
`ViewBuilderDialogViewModelTests.Construction_RefreshesAvailableExposeTargetsFromWorkspace`.

**Construction_EmptyWorkspace_IsWorkspaceEmptyAndNoTargets**: Constructing the view model over a zero-source
workspace reports `IsWorkspaceEmpty` as `true` and an empty `AvailableExposeTargets` list. Verified by
`ViewBuilderDialogViewModelTests.Construction_EmptyWorkspace_IsWorkspaceEmptyAndNoTargets`.

**AddExposeTarget_AddsToDefinitionAndRaisesPreviewChanged**: Adding an expose target appends it to
`Definition.ExposeTargets` and raises `PreviewChanged` exactly once. Verified by
`ViewBuilderDialogViewModelTests.AddExposeTarget_AddsToDefinitionAndRaisesPreviewChanged`.

**RemoveExposeTarget_RemovesFromDefinitionAndRaisesPreviewChanged**: Removing a previously-added expose target
removes it from `Definition.ExposeTargets`. Verified by
`ViewBuilderDialogViewModelTests.RemoveExposeTarget_RemovesFromDefinitionAndRaisesPreviewChanged`.

**SetExposeRecursionKind_ChangesRecursionKindAndRaisesPreviewChanged**: Changing an expose target's recursion
kind updates the corresponding entry in `Definition.ExposeTargets`. Verified by
`ViewBuilderDialogViewModelTests.SetExposeRecursionKind_ChangesRecursionKindAndRaisesPreviewChanged`.

**SetExposeBracketFilter_SetsFilterAndRaisesPreviewChanged**: Setting an expose target's bracket-filter
expression updates the corresponding entry in `Definition.ExposeTargets`. Verified by
`ViewBuilderDialogViewModelTests.SetExposeBracketFilter_SetsFilterAndRaisesPreviewChanged`.

**SetViewKind_UpdatesDefinitionAndRaisesPreviewChanged**: Changing the view kind updates `Definition.ViewKind`
and raises `PreviewChanged` exactly once. Verified by
`ViewBuilderDialogViewModelTests.SetViewKind_UpdatesDefinitionAndRaisesPreviewChanged`.

**SetFilterExpression_UpdatesDefinitionAndRaisesPreviewChanged**: Changing the filter expression updates
`Definition.FilterExpression` and raises `PreviewChanged` exactly once. Verified by
`ViewBuilderDialogViewModelTests.SetFilterExpression_UpdatesDefinitionAndRaisesPreviewChanged`.

**RenderPreview_IncompleteDefinition_SetsStatusMessageInsteadOfThrowing**: Rendering an incomplete definition (no
view kind or expose targets yet) sets a non-null `StatusMessage` and leaves `PreviewCanvas` with no loaded
content, rather than throwing. Verified by
`ViewBuilderDialogViewModelTests.RenderPreview_IncompleteDefinition_SetsStatusMessageInsteadOfThrowing`.

**RenderPreview_ValidDefinition_LoadsPreviewCanvasAndClearsStatusMessage**: Rendering a valid, complete
definition loads content into `PreviewCanvas`, clears `StatusMessage`, and never touches the shell's real
`OpenTabs`/`ActiveTabId`. Verified by
`ViewBuilderDialogViewModelTests.RenderPreview_ValidDefinition_LoadsPreviewCanvasAndClearsStatusMessage`.

**TryCommit_ValidDefinition_OpensExactlyOneNewTabAndReturnsTrue**: Committing a valid definition opens exactly
one new tab on the shell, rendered with that definition, made the active tab, and returns `true` with a `null`
error. Verified by
`ViewBuilderDialogViewModelTests.TryCommit_ValidDefinition_OpensExactlyOneNewTabAndReturnsTrue`.

**TryCommit_InvalidDefinition_ReturnsFalseSetsStatusMessageAndDoesNotAddTab**: Committing an invalid definition
returns `false`, sets a matching `StatusMessage`/`error`, and leaves zero tabs open - the Risk #5
open-then-render-then-close-on-failure sequence never leaks a partial/empty tab. Verified by
`ViewBuilderDialogViewModelTests.TryCommit_InvalidDefinition_ReturnsFalseSetsStatusMessageAndDoesNotAddTab`.

**EditingWithoutCommitting_LeavesShellTabStateUntouched**: A full editing session (view kind, expose target,
filter expression, display name) that is never committed leaves the shell's `OpenTabs`, `ActiveTabId`, and
`ActiveCustomView` completely untouched, matching Cancel's zero-side-effect contract. Verified by
`ViewBuilderDialogViewModelTests.EditingWithoutCommitting_LeavesShellTabStateUntouched`.

**Construction_FreshInstance_DoesNotCarryOverPriorInstanceSelections**: A newly constructed view model, built
over the same shell right after a prior instance was edited, starts from a completely clean `Definition` - no
view kind, expose targets, filter expression, or display name carried over. Verified by
`ViewBuilderDialogViewModelTests.Construction_FreshInstance_DoesNotCarryOverPriorInstanceSelections`.
