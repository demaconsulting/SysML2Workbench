### MainWindowShell

![AppShellSubsystem Structure](AppShellSubsystemView.svg)

#### Purpose

MainWindowShell is the desktop composition root that coordinates workspace
lifecycle, view selection, diagram display, diagnostics presentation, and
snippet export within a single windowed user experience.

#### Data Model

**CurrentWorkspace**: `WorkspaceSnapshot?` — currently loaded workspace and its
revision metadata.

**ActivePredefinedView**: `ViewDescriptor?` — selected catalog view, if the
user is in predefined-view mode.

**ActiveCustomView**: `ViewDefinitionModel?` — current custom-view state, if
the user is composing a preview.

**OpenTabs**: `IReadOnlyList<WorkbenchTab>` — tabs representing rendered
diagrams, builder surfaces, or related shell content. Each `WorkbenchTab`
owns its own `SvgCanvasHost`, so multiple open diagram tabs have fully
independent zoom/pan/content state.

**ActiveTabId**: `string?` — identifier of the diagram tab currently
active/focused, or `null` when no diagram tab is open. Updated both by shell
operations that open or focus a tab, and by the UI layer forwarding Dock's
own focus-change signal via `NotifyActiveDiagramTab`.

#### Key Methods

**OpenWorkspace**: Loads a new workspace into the shell.

- *Parameters*: `string rootPath` — user-selected folder.
- *Returns*: `void` — shell state updates in place.
- *Preconditions*: `rootPath` exists and is readable.
- *Postconditions*: WorkspaceSubsystem is initialized, the view catalog is
  refreshed, diagnostics are displayed, and every open tab is closed
  (`ActiveTabId` becomes `null`) since a reloaded workspace invalidates every
  currently-rendered diagram. The file watcher is retargeted to `rootPath` on
  every call, not only the first: any previously watched root is torn down and
  its pending change state discarded, so a second or later workspace open
  never leaves the watcher bound to a stale folder.

**SelectPredefinedView**: Renders a predefined view selected by the user.

- *Parameters*: `string viewId` — identifier from ViewCatalogPresenter.
- *Returns*: `void` — active diagram state updates in place.
- *Preconditions*: A workspace is loaded and the view identifier is present in
  the current catalog.
- *Postconditions*: `ActivePredefinedView` is updated; a tab identified by the
  view's qualified name is opened (or, if already open, reused) and rendered
  into; that tab becomes `ActiveTabId` either way, so selecting an
  already-open predefined view again switches focus to it without
  duplicating the tab.

**PreviewCustomView**: Renders the current GUI-authored custom view.

- *Parameters*: `ViewDefinitionModel definition` — normalized custom-view
  state.
- *Returns*: `void` — active diagram state updates in place.
- *Preconditions*: The definition validates against the current workspace.
- *Postconditions*: `ActiveCustomView` is updated. If the currently active tab
  is itself a custom-view-preview tab, it is re-rendered in place (same tab
  identity and canvas); otherwise a brand-new custom-view-preview tab is
  opened and made active - this covers both "the active tab is a predefined
  view" and "no tab is open at all".

**OpenNewCustomPreviewTab**: Opens a brand-new, empty custom-view-preview tab
and makes it active, without rendering anything into it.

- *Parameters*: none.
- *Returns*: `WorkbenchTab` — the newly opened tab.
- *Postconditions*: A new tab is appended to `OpenTabs` and becomes
  `ActiveTabId`. Backs the "+ New Diagram Tab" affordance; a subsequent
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
`OpenWorkspace`, which awaits workspace loading with `ConfigureAwait(false)` -
may themselves resume on a background thread pool thread once the load
completes.

#### Dependencies

- **WorkspaceSubsystem** — loads and refreshes the workspace state.
- **ViewCatalogPresenter** — supplies predefined view choices.
- **ViewDefinitionModel** — captures custom-view authoring state.
- **SysmlSnippetGenerator** — exports custom-view definitions as SysML text.
- **LayoutInvoker** — renders predefined and custom views.
- **SvgCanvasHost** — displays the active diagram.
- **DiagnosticsListView** — displays workspace diagnostics.
- **RollingFileLogger** — records shell-level operational failures.
- **IUiDispatcher** — marshals `TabsChanged` notifications onto the thread
  required by UI-facing subscribers; defaults to an immediate, synchronous
  dispatcher when none is supplied.
- **Avalonia** — provides the window, tab, and application-lifetime framework.

#### Callers

N/A - entry point, called by the host environment.
