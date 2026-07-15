### MainWindowShell

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/MainWindowShellTests.cs` exercise
`MainWindowShell` directly. The suite drives the shell with temporary workspaces, real collaborator units, and local log
files so region synchronization, tab management, and failure guards are verified. The scenario list below follows the
authoritative mappings in `docs/reqstream/sysml2-workbench/app-shell-subsystem/main-window-shell.yaml` and describes the
implemented tests in present tense.

#### Test Environment

Tests run under the standard .NET test runner with temporary workspace and log directories plus real collaborator units.
No external services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/MainWindowShellTests.cs` that
  correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/app-shell-subsystem/main-window-shell.yaml` using the real paths and collaborators
  described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**Startup_ArrangesPrimaryWorkspaceAndDiagramRegions**: Opening a workspace arranges the primary workspace, diagram, and
diagnostics regions: the catalog, diagnostics list, and canvas host all reflect the freshly loaded workspace. Verified
by `MainWindowShellTests.Startup_ArrangesPrimaryWorkspaceAndDiagramRegions`.

**OpenViews_ManagesTabbedPresentation**: Opening tabs manages tabbed presentation: selecting a predefined view and then
previewing a custom view each open a distinct tab, without duplicating an already-open tab; selecting an already-open
predefined view again keeps the tab count unchanged and activates that tab. Verified by
`MainWindowShellTests.OpenViews_ManagesTabbedPresentation`.

**PreviewCustomView_WhenActiveTabIsCustomPreview_UpdatesInPlace**: Previewing a custom view while a custom-view-preview
tab is already active re-renders in place, reusing the same tab identity rather than opening a second tab. Verified by
`MainWindowShellTests.PreviewCustomView_WhenActiveTabIsCustomPreview_UpdatesInPlace`.

**PreviewCustomView_WhenActiveTabIsPredefinedView_OpensNewTab**: Previewing a custom view while a predefined-view tab
is active opens a brand-new, distinct custom-preview tab, leaving the predefined-view tab untouched. Verified by
`MainWindowShellTests.PreviewCustomView_WhenActiveTabIsPredefinedView_OpensNewTab`.

**PreviewCustomView_WithNoTabsOpen_OpensNewTab**: Previewing a custom view with zero tabs open opens exactly one new,
active custom-preview tab. Verified by `MainWindowShellTests.PreviewCustomView_WithNoTabsOpen_OpensNewTab`.

**OpenNewCustomPreviewTab_OpensEmptyActiveTab_AndSubsequentPreviewUpdatesIt**: The "+ New Diagram Tab" affordance opens
an empty, active tab, and a subsequent preview call updates that same tab in place. Verified by
`MainWindowShellTests.OpenNewCustomPreviewTab_OpensEmptyActiveTab_AndSubsequentPreviewUpdatesIt`.

**CloseDiagramTab_RemovesTab_AndReassignsActiveTab**: Closing a diagram tab removes it and reassigns the active tab to
a neighbor; closing the final tab leaves no open tabs, a null active tab, and an idle (empty) canvas. Verified by
`MainWindowShellTests.CloseDiagramTab_RemovesTab_AndReassignsActiveTab`.

**NotifyActiveDiagramTab_UnknownId_IsIgnored**: Notifying the shell of an unknown/stale tab id is ignored rather than
clearing a still-valid active tab id. Verified by `MainWindowShellTests.NotifyActiveDiagramTab_UnknownId_IsIgnored`.

**SelectPredefinedView_TabsHaveIndependentCanvases**: Each diagram tab owns a fully independent canvas: opening two
predefined views produces two distinct canvas host instances, and zooming one does not affect the other's zoom level.
Verified by `MainWindowShellTests.SelectPredefinedView_TabsHaveIndependentCanvases`.

**SessionStateChanges_SynchronizeVisibleRegions**: Reloading the workspace after an external change resynchronizes
visible shell regions (diagnostics and catalog) and resets stale active-view state. Verified by
`MainWindowShellTests.SessionStateChanges_SynchronizeVisibleRegions`.
