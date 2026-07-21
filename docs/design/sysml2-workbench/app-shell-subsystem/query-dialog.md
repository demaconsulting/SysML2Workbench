### QueryDialog

![AppShellSubsystem Structure](AppShellSubsystemView.svg)

#### Purpose

QueryDialog is the modal dialog opened from the main window's Query menu
("_Run Query...") that lets the user browse or query the currently-loaded
workspace. It exposes two tabs:

- **Browse** — a purely-client-side filter over every workspace declaration
  (respecting the global "Include standard library" checkbox), regenerating
  a shared results panel live as the user types.
- **Element Query** — a target-plus-verb form that dispatches one of the ten
  element-scoped `QueryVerb` operations through
  `DemaConsulting.SysML2Tools.Query.QueryEngine.Execute` when the user clicks
  "Run Query".

Results in both tabs feed the same shared results panel: a summary bullet
list plus a `Grid`-based (not `DataGrid`) entries table with columns for
Qualified Name, Kind, Detail, and a Direction column shown only when the
current result is a `dependencies` verb result. "Copy as Markdown" and "Copy
as JSON" buttons write the rendered result to the clipboard via
`QueryResultRenderer.RenderMarkdown`/`RenderJson`, using the same
`AvaloniaClipboardService` pattern `DiagramDocumentView` uses for its "Copy
as SysML" action.

It is documented as one unit covering both `QueryDialogViewModel` (the
Avalonia-free composition and dispatch state) and `QueryDialogView` (the
Avalonia `Window` shown via `ShowDialog`), matching the pairing convention
used by the sibling `ViewBuilderDialog` unit. Like the other dialogs in this
subsystem, `QueryDialog` is not a Dock tool: it is a short-lived, owned
dialog `Window`, constructed fresh every time the user opens it.

#### Data Model

**Shell**: `MainWindowShell` — the shared application shell, used to resolve
candidate elements (`CurrentWorkspace.Workspace.Declarations`) and to run
queries against (`CurrentWorkspace.Workspace` handed to
`QueryEngine.Execute`).

**BrowsePicker** / **ElementQueryPicker**: two
`ElementPickerSubsystem.ElementPickerViewModel` instances, one per tab. The
Browse tab's picker feeds a live client-side result; the Element Query
tab's picker resolves the target element for the verb.

**IncludeStdlib**: `bool` — the global "Include standard library" checkbox
state, threaded through both `RefreshFromWorkspace` (as a candidate-filter
choice) and `BuildOptions` (as the `QueryOptions.IncludeStdlib` value for
every engine call).

**IsWorkspaceEmpty**: `bool` — `true` when the shell's current workspace has
zero sources, used to show the dialog's own empty-state hint alongside the
checkbox.

**SelectedVerb**: `QueryVerb` — the currently-selected element-scoped verb;
defaults to `Describe`.

**HierarchyDirection**: `string?` — the direction option accepted by the
Hierarchy verb (`"up"`, `"down"`, or `"both"`); defaults to `"both"`. Only
attached to `QueryOptions.Direction` when `SelectedVerb` is `Hierarchy`.

**WalkDepthText**: `string?` — the free-text input for the Impact verb's
optional walk-depth bound. Only parsed for `Impact`, and only when it parses
cleanly as a non-negative integer.

**CurrentResult**: `QueryResult?` — the most recently produced `QueryResult`
(either the Browse tab's client-built list result or the engine's response).
Feeds the shared results panel and enables the copy buttons.

**CurrentResultRows**: `IReadOnlyList<QueryResultRow>` — a flattened,
view-friendly projection of `CurrentResult.Entries` (empty strings replacing
nullable fields, `Direction` mapped to a human-readable label, notes joined
into a tooltip string), bound directly by the results-panel `ItemsControl`.

**StatusMessage**: `string?` — user-visible error/hint text set on every
recoverable failure (no workspace, no selection, engine argument rejection).
Cleared on a successful run.

**ClipboardService**: `IClipboardService?` — the same clipboard-write seam
`DiagramDocumentViewModel` uses. Assigned by `QueryDialogView` to an
`AvaloniaClipboardService` anchored on the dialog's own `Window`.

#### Key Methods

**RefreshFromWorkspace**: Refreshes both pickers' candidate lists and
`IsWorkspaceEmpty` from the shell's current workspace.

- *Parameters*: none.
- *Returns*: `void` — both pickers' `SetCandidates` are called with the same
  candidate list (no default chip on either).
- *Postconditions*: Called once at construction and again on every
  `IncludeStdlib` toggle; the dialog does not itself subscribe to
  `MainWindowShell.SourcesChanged`, matching the sibling dialogs' pattern.

**BuildBrowseResult**: Rebuilds `CurrentResult` and `CurrentResultRows` from
the Browse tab's `DisplayedItems`.

- *Parameters*: none.
- *Returns*: `void` — `CurrentResult` becomes a `QueryResult` with
  `Verb="list"`, `Element=null`, one entry per displayed item.
- *Postconditions*: The Browse tab is deliberately a purely-client-side
  filter; it does NOT call `QueryEngine.List`/`Find`. Invoked automatically
  whenever `BrowsePicker.DisplayedItems` changes so the results panel stays
  live.

**RunElementQuery**: Runs the Element Query tab's currently-configured verb
against `ElementQueryPicker.SelectedQualifiedName`.

- *Parameters*: none.
- *Returns*: `void` — `CurrentResult`, `CurrentResultRows`, and
  `StatusMessage` update in place.
- *Postconditions*: Every recoverable failure surface (no workspace, no
  selection, unknown qualified name, engine `ArgumentException`) is
  reported through `StatusMessage`; never throws.

**BuildOptions**: Builds a `QueryOptions` instance reflecting the current
verb-specific state.

- *Parameters*: `string qualifiedName` — the resolved target's qualified name.
- *Returns*: `QueryOptions` — always carries `IncludeStdlib`; attaches
  `Direction` only for `Hierarchy`; parses `WalkDepth` only for `Impact` and
  only when the text parses cleanly.

**CopyResultAsMarkdownAsync** / **CopyResultAsJsonAsync**: Copy
`CurrentResult` to the clipboard through `QueryResultRenderer` and
`ClipboardService`.

- *Parameters*: none.
- *Returns*: `Task` — completes once the clipboard write has finished.
- *Postconditions*: A safe no-op (rather than an exception) when
  `CurrentResult` or `ClipboardService` is `null`.

#### Error Handling

QueryDialog treats every recoverable failure - no workspace, no selection,
a picker qualified name that vanished under a mid-session workspace change,
or an engine `ArgumentException` (thrown when the verb requires an element
and none is resolvable, or when the verb value itself is out of range) - as
a locally recoverable condition surfaced through `StatusMessage` rather
than an unhandled exception. `CurrentResult` is left unchanged on failure,
so a prior successful result stays visible while the user fixes the
selection or verb configuration.

#### Dependencies

- **MainWindowShell** — supplies `CurrentWorkspace` for candidate resolution
  and for the workspace argument passed to `QueryEngine.Execute`.
- **ElementPickerSubsystem** — the two composed picker instances.
- **DemaConsulting.SysML2Tools.Query** — `QueryEngine.Execute` runs the
  verb, `QueryResultRenderer.RenderMarkdown`/`RenderJson` produces the
  clipboard text.
- **IClipboardService** / **AvaloniaClipboardService** — the clipboard-write
  seam shared with `DiagramDocumentViewModel`.
- **Avalonia** — `QueryDialogView` is a `Window` shown modally via
  `ShowDialog`, mirroring `AboutDialogView` and `ViewBuilderDialogView`.

#### Callers

- **MainWindowView** — constructs `QueryDialogView` and calls
  `ShowDialog(this)` when the user selects the Query menu's "Run Query..."
  item, so the dialog is owned by and centers over the main window.
