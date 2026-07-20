### MainWindowShell

![AppShellSubsystem Structure](AppShellSubsystemView.svg)

#### Purpose

MainWindowShell is the desktop composition root that coordinates workspace
lifecycle, view selection, diagram display, diagnostics presentation, and
snippet export within a single windowed user experience.

#### Data Model

**CurrentWorkspace**: `WorkspaceSnapshot` — currently loaded workspace and its
revision metadata. Never `null`: initialized at construction to a valid empty
snapshot (zero sources, zero files, stdlib-only) rather than left unset until
the first source is added, so every consumer can safely read
`CurrentWorkspace.Sources` without a null guard.

**ActivePredefinedView**: `ViewDescriptor?` — selected catalog view, if the
user is in predefined-view mode.

**ActiveCustomView**: `ViewDefinitionModel?` — current custom-view state, if
the user is composing a preview.

**OpenTabs**: `IReadOnlyList<WorkbenchTab>` — tabs representing rendered
diagrams, builder surfaces, read-only source-text views, or related shell
content. Each `WorkbenchTab` owns its own `SvgCanvasHost` (unused/never
loaded for a source-text tab), so multiple open diagram tabs have fully
independent zoom/pan/content state.

**ActiveTabId**: `string?` — identifier of the diagram tab currently
active/focused, or `null` when no diagram tab is open. Updated both by shell
operations that open or focus a tab, and by the UI layer forwarding Dock's
own focus-change signal via `NotifyActiveDiagramTab`.

**CurrentSourceIdToFiles**: `IReadOnlyDictionary<string, IReadOnlyList<string>>`
— per-source file lists from the most recent workspace resolution, used by
WorkspacePanel to build its tree without owning its own `WorkspaceSourceSet`
instance.

Each `WorkbenchTab` also carries a `SourceDefinition: ViewDefinitionModel?` —
the definition that produced the tab's currently rendered diagram, or `null`
when none could be derived (an unscoped predefined view with zero expose
members, or a brand-new custom-preview tab that has not rendered anything
yet, or a source-text tab, which has no rendered diagram at all). This backs
the tab's "Copy as SysML" context-menu action.

`WorkbenchTab` also carries a `FilePath: string?` — the absolute path of the
file a source-text tab displays; `null` for predefined-view and
custom-view-preview tabs. `WorkbenchTabKind` has a corresponding
`SourceText` member alongside `PredefinedView` and `CustomViewPreview`.
`WorkbenchTab.Id` for a source-text tab is the file's own absolute path,
which both gives it a stable dedupe key (opening the same file twice
re-focuses the existing tab rather than duplicating it) and lets
`GetTabFilePath` resolve it without exposing `OpenTabs` internals further.

#### Key Methods

**AddFileSource**: Adds a single file as a new workspace source.

- *Parameters*: `string path` — file to add.
- *Returns*: `WorkspaceSnapshot` — the reloaded workspace snapshot.
- *Postconditions*: The path is registered on the shell's owned
  `WorkspaceSourceSet`, the source set is re-resolved, `WorkspaceModel` is
  reloaded against the new resolution, the file watcher begins watching the
  new source (a non-recursive, filtered watcher for a file source), the
  resulting snapshot is applied (existing tabs are cleared, since a reload
  invalidates every currently-rendered diagram), and `SourcesChanged` is
  raised. Idempotent: adding the same path twice does not duplicate the
  source.

**AddFolderSource**: Adds a folder as a new workspace source.

- *Parameters*: `string path` — folder to add.
- *Returns*: `WorkspaceSnapshot` — the reloaded workspace snapshot.
- *Postconditions*: Identical to `AddFileSource`, except the source is a
  recursively watched folder and its files are discovered via
  `WorkspaceSourceSet.Resolve()`'s default glob options.

**RemoveSource**: Removes a previously registered source.

- *Parameters*: `string sourceId` — id of the source to remove.
- *Returns*: `WorkspaceSnapshot` — the reloaded workspace snapshot.
- *Postconditions*: The source is removed from the source set (a no-op for
  an unknown id), the set is re-resolved, `WorkspaceModel` is reloaded, the
  now-unwatched source's watcher is disposed, the resulting snapshot is
  applied, and `SourcesChanged` is raised. Removing the last remaining source
  produces a valid empty snapshot (zero sources, zero files) - the same
  first-class empty state the shell starts in at construction - and still
  correctly clears every open tab, not only when shrinking to a smaller
  nonzero file set.

#### Additional Key Methods

**SelectPredefinedView**: Renders a predefined view selected by the user.

- *Parameters*: `string viewId` — identifier from ViewCatalogPresenter.
- *Returns*: `void` — active diagram state updates in place.
- *Preconditions*: `CurrentWorkspace.Sources.Count > 0` and the view
  identifier is present in the current catalog.
- *Postconditions*: `ActivePredefinedView` is updated; a tab identified by the
  view's qualified name is opened (or, if already open, reused) and rendered
  into; that tab becomes `ActiveTabId` either way, so selecting an
  already-open predefined view again switches focus to it without
  duplicating the tab.

**PreviewCustomView**: Renders the current GUI-authored custom view.

- *Parameters*: `ViewDefinitionModel definition` — normalized custom-view
  state.
- *Returns*: `void` — active diagram state updates in place.
- *Preconditions*: `CurrentWorkspace.Sources.Count > 0` and the definition
  validates against the current workspace.
- *Postconditions*: `ActiveCustomView` is updated. If the currently active tab
  is itself a custom-view-preview tab, it is re-rendered in place (same tab
  identity and canvas); otherwise a brand-new custom-view-preview tab is
  opened and made active - this covers both "the active tab is a predefined
  view" and "no tab is open at all". Backs `ViewBuilderDialog`'s OK commit
  path (immediately preceded by `OpenNewCustomPreviewTab`).

**RenderCustomViewPreview**: Renders a custom-view definition as a live
preview without mutating any open-tab state.

- *Parameters*: `ViewDefinitionModel definition` — normalized custom-view
  state.
- *Returns*: `string` — rendered SVG markup for the given definition.
- *Preconditions*: `CurrentWorkspace.Sources.Count > 0` and the definition
  validates against the current workspace (same guard clauses as
  `PreviewCustomView`).
- *Postconditions*: None on shell state: unlike `PreviewCustomView`, this
  method does not mutate `OpenTabs`, `ActiveTabId`, or `ActiveCustomView`,
  and does not raise `TabsChanged`. Backs `ViewBuilderDialog`'s left-hand
  live preview pane, which re-renders on every in-progress edit while the
  dialog is open, before the user has committed via OK.

**OpenNewCustomPreviewTab**: Opens a brand-new, empty custom-view-preview tab
and makes it active, without rendering anything into it.

- *Parameters*: none.
- *Returns*: `WorkbenchTab` — the newly opened tab.
- *Postconditions*: A new tab is appended to `OpenTabs` and becomes
  `ActiveTabId`. Backs `ViewBuilderDialog`'s OK commit path; a subsequent
  `PreviewCustomView` call updates this same tab in place.

**CloseDiagramTab**: Closes the diagram tab with the given identifier.

- *Parameters*: `string tabId` — identifier of the tab to close.
- *Returns*: `void` — shell state updates in place.
- *Postconditions*: The tab is removed from `OpenTabs`. If it was the active
  tab, a neighboring tab becomes active, or `ActiveTabId` becomes `null` if no
  tabs remain.

**NotifyActiveDiagramTab**: Notifies the shell that a diagram tab has gained
UI focus.

- *Parameters*: `string? tabId` — identifier of the newly focused diagram
  tab.
- *Returns*: `void` — `ActiveTabId` updates in place.
- *Postconditions*: `ActiveTabId` is updated, unless `tabId` does not refer to
  a currently open tab (an unknown or stale id is ignored rather than
  clearing a still-valid `ActiveTabId`). Called by the Avalonia-aware UI layer
  when Dock reports a focus change onto a diagram document - never by
  `MainWindowShell` itself, which stays Dock-agnostic.

**OpenSourceTextTab**: Opens a read-only source-text tab for a given file
path.

- *Parameters*: `string filePath` — absolute path of the `.sysml` file to
  display.
- *Returns*: `WorkbenchTab` — the newly opened tab, or the already-open tab
  for the same path if one exists.
- *Postconditions*: If a source-text tab for `filePath` is already open, it
  is reused (its `FilePath` is left unchanged) and refocused rather than
  duplicated; otherwise a new `WorkbenchTabKind.SourceText` tab is appended,
  titled with the file's own name (`Path.GetFileName`) and identified by the
  file's absolute path. Either way the tab becomes `ActiveTabId` and
  `TabsChanged` is raised. Backs `WorkspacePanelToolView`'s double-click
  handler; the view built for a source-text tab
  (`SourceTextDocumentView`/`SourceTextDocumentViewModel`, see "Supporting
  Types" below) reads the file's contents directly rather than through
  `MainWindowShell`, which never loads file text itself.

**GetTabFilePath**: Resolves the file path of an open source-text tab.

- *Parameters*: `string tabId` — identifier of an open tab.
- *Returns*: `string?` — the tab's `FilePath`, or `null` when `tabId` does
  not refer to a currently open tab, or refers to a tab of a different kind
  (whose `FilePath` is always `null`). Mirrors the existing `GetTabCanvas`
  lookup pattern.
- *Postconditions*: None (read-only). Lets `SourceTextDocumentViewModel`
  resolve which file it displays without `MainWindowShell` needing to
  expose `OpenTabs` internals further.

**CanExportTabAsSysml**: Reports whether an open diagram tab has a derivable
source definition and can export its diagram as a SysML snippet.

- *Parameters*: `string tabId` — identifier of an open tab.
- *Returns*: `bool` — `true` when the tab exists and its `SourceDefinition` is
  ready to export.
- *Postconditions*: None (read-only). Backs the enabled/disabled state of
  every diagram tab's "Copy as SysML" context-menu item.

**ExportTabAsSysmlSnippet**: Generates copy-pasteable SysML `view` text for
the diagram currently rendered in an open tab.

- *Parameters*: `string tabId` — identifier of an open tab.
- *Returns*: `string?` — the SysML snippet, or `null` when
  `CanExportTabAsSysml` would report `false` for that tab.
- *Postconditions*: A `null` result is logged at `Info` level with the reason
  (an expected, valid outcome, not a failure) rather than thrown; a
  successful export is also logged at `Info` level, mirroring
  `ExportCustomViewSnippet`'s existing style.

#### Supporting Types

**SourceTextDocumentViewModel**/**SourceTextDocumentView**: the Dock
`Document`/view pair backing a source-text tab, following the same
convention already used for `DiagramDocumentViewModel`/`DiagramDocumentView`
(neither pair is a separately documented unit). `SourceTextDocumentViewModel`
resolves its file path via `GetTabFilePath`, sets its `Title` to
`Path.GetFileName(FilePath)`, and eagerly loads `Text` via `File.ReadAllText`
once at construction, substituting a friendly in-editor error message for a
deleted or locked file instead of throwing (no caching, no file-watch, no
write-back). `SourceTextDocumentView` hosts a read-only AvaloniaEdit
`TextEditor` with SysML v2 syntax highlighting (see
`docs/design/ots/avaloniaedit.md`).

#### Error Handling

MainWindowShell handles user-cancelled operations, empty workspace selections,
and recoverable UI-state transitions locally. It propagates subsystem
initialization failures, render failures, and unrecoverable workspace access
errors because the user must be informed that the requested workflow could not
complete. Logged failures remain visible in the UI rather than being hidden
behind background retries.

#### Notifications

**TabsChanged**: Raised whenever the set of open tabs, or which tab is
active, changes - a tab is opened, closed, re-rendered in place, or the
workspace is reloaded (which clears every tab). The Avalonia-aware UI layer
(`MainWindowView`) subscribes to this to reconcile Dock's `DocumentDock` with
`OpenTabs`, since `MainWindowShell` itself has no Dock dependency and cannot
add or remove Dock dockables directly. The notification is raised through an
injected `IUiDispatcher`, so subscribers are guaranteed to observe it on the
dispatcher's target thread even though methods that trigger it - most notably
`AddFolderSourceAsync`, which awaits workspace loading with
`ConfigureAwait(false)` - may themselves resume on a background thread pool
thread once the load completes.

**SourcesChanged**: Raised whenever the set of workspace sources changes - a
source is added or removed and the resulting resolution has been applied.
Parallel in structure and marshaling behavior to `TabsChanged`: raised through
the same injected `IUiDispatcher`. `WorkspacePanelToolViewModel` subscribes to
this to rebuild its tree, and `CurrentSourceIdToFiles` reflects the change by
the time subscribers observe the notification.

#### Dependencies

- **WorkspaceSubsystem** — loads and refreshes the workspace state.
- **WorkspaceSourceSet** — owned by the shell; mutated by
  `AddFileSourceAsync`/`AddFolderSourceAsync`/`RemoveSourceAsync` and
  re-resolved before every `WorkspaceModel` load, reload, and watcher diff.
- **ViewCatalogPresenter** — supplies predefined view choices.
- **ViewDefinitionModel** — captures custom-view authoring state.
- **SysmlSnippetGenerator** — exports custom-view definitions as SysML text.
- **LayoutInvoker** — renders predefined and custom views.
- **SvgCanvasHost** — displays the active diagram.
- **DiagnosticsListView** — displays workspace diagnostics.
- **RollingFileLogger** — records shell-level operational failures.
- **IUiDispatcher** — marshals `TabsChanged` and `SourcesChanged`
  notifications onto the thread required by UI-facing subscribers; defaults
  to an immediate, synchronous dispatcher when none is supplied.
- **Avalonia** — provides the window, tab, and application-lifetime framework.
- **AvaloniaEdit** — provides the read-only, syntax-highlighted `TextEditor`
  control hosted by `SourceTextDocumentView` (see
  `docs/design/ots/avaloniaedit.md`).

#### Callers

N/A - entry point, called by the host environment.
