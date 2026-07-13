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
diagrams, builder surfaces, or related shell content.

#### Key Methods

**OpenWorkspace**: Loads a new workspace into the shell.

- *Parameters*: `string rootPath` — user-selected folder.
- *Returns*: `void` — shell state updates in place.
- *Preconditions*: `rootPath` exists and is readable.
- *Postconditions*: WorkspaceSubsystem is initialized, the view catalog is
  refreshed, diagnostics are displayed, and prior tab state is reset or
  migrated according to policy.

**SelectPredefinedView**: Renders a predefined view selected by the user.

- *Parameters*: `string viewId` — identifier from ViewCatalogPresenter.
- *Returns*: `void` — active diagram state updates in place.
- *Preconditions*: A workspace is loaded and the view identifier is present in
  the current catalog.
- *Postconditions*: `ActivePredefinedView` is updated and the resulting SVG is
  loaded into SvgCanvasHost.

**PreviewCustomView**: Renders the current GUI-authored custom view.

- *Parameters*: `ViewDefinitionModel definition` — normalized custom-view
  state.
- *Returns*: `void` — active diagram state updates in place.
- *Preconditions*: The definition validates against the current workspace.
- *Postconditions*: `ActiveCustomView` is updated and a preview diagram is
  shown.

#### Error Handling

MainWindowShell handles user-cancelled operations, empty workspace selections,
and recoverable UI-state transitions locally. It propagates subsystem
initialization failures, render failures, and unrecoverable workspace access
errors because the user must be informed that the requested workflow could not
complete. Logged failures remain visible in the UI rather than being hidden
behind background retries.

#### Dependencies

- **WorkspaceSubsystem** — loads and refreshes the workspace state.
- **ViewCatalogPresenter** — supplies predefined view choices.
- **ViewDefinitionModel** — captures custom-view authoring state.
- **SysmlSnippetGenerator** — exports custom-view definitions as SysML text.
- **LayoutInvoker** — renders predefined and custom views.
- **SvgCanvasHost** — displays the active diagram.
- **DiagnosticsListView** — displays workspace diagnostics.
- **RollingFileLogger** — records shell-level operational failures.
- **Avalonia** — provides the window, tab, and application-lifetime framework.

#### Callers

N/A - entry point, called by the host environment.
