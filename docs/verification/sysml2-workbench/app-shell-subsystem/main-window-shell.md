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

**OpenNewCustomPreviewTab_OpensEmptyActiveTab_AndSubsequentPreviewUpdatesIt**: Opening a new custom-preview tab opens
an empty, active tab, and a subsequent preview call updates that same tab in place. Verified by
`MainWindowShellTests.OpenNewCustomPreviewTab_OpensEmptyActiveTab_AndSubsequentPreviewUpdatesIt`.

**RenderCustomViewPreview_DoesNotMutateOpenTabsOrActiveTab**: Rendering a live custom-view preview returns SVG
markup without mutating `OpenTabs`, `ActiveTabId`, or `ActiveCustomView` - unlike `PreviewCustomView`, it never
leaks a real tab into the shell's tracked state. Verified by
`MainWindowShellTests.RenderCustomViewPreview_DoesNotMutateOpenTabsOrActiveTab`.

**RenderCustomViewPreview_NoWorkspaceOpened_ThrowsInvalidOperationException**: Rendering a live custom-view preview
while zero workspace sources are open throws `InvalidOperationException`, matching `SelectPredefinedView`'s own
empty-workspace guard. Verified by
`MainWindowShellTests.RenderCustomViewPreview_NoWorkspaceOpened_ThrowsInvalidOperationException`.

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

**AddFolderSourceAsync_SecondDistinctFolder_IsAdditiveAndWatchesBothSources**: Adding a second, distinct folder
source is additive - both folders' files are merged into the workspace and both sources are independently watched,
without disturbing the first source. Verified by
`MainWindowShellTests.AddFolderSourceAsync_SecondDistinctFolder_IsAdditiveAndWatchesBothSources`.

**SourcesChanged_RaisedOnAdd_NotRaisedAtConstruction**: `SourcesChanged` is raised when a source is added, but is
not raised merely by constructing the shell (which establishes its initial empty snapshot without any source
mutation). Verified by `MainWindowShellTests.SourcesChanged_RaisedOnAdd_NotRaisedAtConstruction`.

**Construction_EstablishesValidEmptySnapshot**: Constructing the shell establishes a valid, non-null
`CurrentWorkspace` with zero sources and zero files, rather than leaving workspace state unset until the first
source is added. Verified by `MainWindowShellTests.Construction_EstablishesValidEmptySnapshot`.

**RemoveSourceAsync_DownToZeroSources_ProducesEmptySnapshotAndUnwatchesEverything**: Removing the last remaining
source produces a valid empty snapshot (zero sources, zero files), clears every open tab, and unwatches every
source's file watcher. Verified by
`MainWindowShellTests.RemoveSourceAsync_DownToZeroSources_ProducesEmptySnapshotAndUnwatchesEverything`.

**SelectPredefinedView_NoWorkspaceOpened_ThrowsInvalidOperationException**: Selecting a predefined view while zero
workspace sources are open throws `InvalidOperationException` rather than rendering against an empty workspace.
Verified by `MainWindowShellTests.SelectPredefinedView_NoWorkspaceOpened_ThrowsInvalidOperationException`.

**ExportTabAsSysmlSnippet_PredefinedViewTab_ReturnsSnippet**: A rendered predefined-view diagram tab exports a
SysML snippet reflecting its derived view definition, and `CanExportTabAsSysml` reports it as exportable. Verified
by `MainWindowShellTests.ExportTabAsSysmlSnippet_PredefinedViewTab_ReturnsSnippet`.

**ExportTabAsSysmlSnippet_CustomPreviewTab_ReturnsSnippet**: A rendered custom-view-preview diagram tab exports a
SysML snippet reflecting the definition it was previewed from. Verified by
`MainWindowShellTests.ExportTabAsSysmlSnippet_CustomPreviewTab_ReturnsSnippet`.

**ExportTabAsSysmlSnippet_UnknownTabId_ReturnsNull**: An unknown tab id is reported as not exportable and returns
`null` rather than throwing. Verified by `MainWindowShellTests.ExportTabAsSysmlSnippet_UnknownTabId_ReturnsNull`.

**ExportTabAsSysmlSnippet_PredefinedViewWithNoExposeMembers_ReturnsNull**: A predefined view with zero expose
members (a valid, unscoped "expose everything" view) cannot be exported, since there is no finite expose list to
serialize - it is reported gracefully rather than throwing. Verified by
`MainWindowShellTests.ExportTabAsSysmlSnippet_PredefinedViewWithNoExposeMembers_ReturnsNull`.

**CanExportTabAsSysml_MirrorsExportability**: `CanExportTabAsSysml` mirrors the same true/false outcomes as
`ExportTabAsSysmlSnippet` across the exportable and unknown-tab cases. Verified by
`MainWindowShellTests.CanExportTabAsSysml_MirrorsExportability`.

**OpenSourceTextTab_NewFile_CreatesOneNewActiveTab**: Opening a source-text tab for a file path that has no
already-open tab creates exactly one new tab, of kind `SourceText`, and makes it active. Verified by
`MainWindowShellTests.OpenSourceTextTab_NewFile_CreatesOneNewActiveTab`.

**OpenSourceTextTab_SamePathTwice_RefocusesExistingTabWithoutDuplicating**: Opening a source-text tab for the same
file path a second time, after another tab has taken focus, reuses the existing tab and refocuses it rather than
duplicating it - the open-tab count is unchanged, and `TabsChanged` is raised on both the first and second calls.
Verified by `MainWindowShellTests.OpenSourceTextTab_SamePathTwice_RefocusesExistingTabWithoutDuplicating`.

**GetTabFilePath_ReturnsPathForSourceTextTab_AndNullOtherwise**: `GetTabFilePath` returns the correct file path for
an open source-text tab, and `null` for a tab id that does not refer to any currently open tab, or that refers to
a tab of a different kind. Verified by
`MainWindowShellTests.GetTabFilePath_ReturnsPathForSourceTextTab_AndNullOtherwise`.
