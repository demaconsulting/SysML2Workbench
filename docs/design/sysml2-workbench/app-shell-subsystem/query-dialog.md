### QueryDialog

![AppShellSubsystem Structure](AppShellSubsystemView.svg)

#### Purpose

QueryDialog is the modal dialog opened from the main window's Query menu
("_Run Query...") that lets the user browse or query the currently-loaded
workspace through a single, always-visible adaptive form — there is no
`TabControl` and no explicit "Run" gesture anywhere in this design:

- A "Query Type" combo box offers eleven choices: a merged **List** entry
  first (a purely-client-side filter over every workspace declaration,
  respecting the global "Include standard library" checkbox), followed by
  the ten element-scoped `QueryVerb` operations (`Describe`, `Uses`,
  `UsedBy`, `Dependencies`, `Impact`, `Hierarchy`, `Requirements`,
  `Interface`, `Connections`, `States`) dispatched through
  `DemaConsulting.SysML2Tools.Query.QueryEngine.Execute`. The user never
  sees `QueryVerb.Find`: "List" always resolves to the client-side filter,
  never to a `QueryEngine.List`/`Find` call.
- Below the combo, one of two controls is shown: for **List**, a
  selection-free filter control (chip row + search box only); for every
  other Query Type, the full element picker (the same chip row + search
  box, plus a selectable candidate list used as the target-element
  selector).
- Verb-specific extra controls appear only when relevant: a "Direction"
  combo for `Hierarchy`, and a "Walk depth" text box for `Impact`.
- Every interaction — Query Type selection, chip add/remove, search text,
  element selection, Direction change, Walk depth text change, or the
  Include-standard-library toggle — immediately recomputes and displays the
  result synchronously; there is no "Run Query" button.

Results feed the same shared results panel: a summary bullet list plus a
`Grid`-based (not `DataGrid`) entries table with columns for Qualified Name,
Kind, Detail, and a Direction column shown only when the current result is a
`dependencies` verb result. There is no toolbar: "Copy as Markdown" and
"Copy as JSON" are right-click `ContextMenu` items on the results panel,
wired to `QueryResultRenderer.RenderMarkdown`/`RenderJson` via the same
`AvaloniaClipboardService` pattern `DiagramDocumentView` uses for its "Copy
as SysML" action, and enabled through a `HasCurrentResult` binding mirroring
`DiagramDocumentViewModel.CanCopyAsSysml`.

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

**FilterOnly**: an `ElementPickerSubsystem.ElementFilterViewModel` instance
used exclusively when `SelectedQueryType` is `List`: its `DisplayedItems` is
the client-side result source. Has no selection concept, matching "List"'s
lack of a target-element concept.

**Picker**: an `ElementPickerSubsystem.ElementPickerViewModel` instance used
for the ten element-scoped Query Types (every `QueryVerb` other than the
merged `List` entry): its `SelectedQualifiedName` is the target element
(its `DisplayedItems` remains visible purely as a filter aid narrowing which
candidates are selectable).

**QueryTypes**: `IReadOnlyList<QueryVerb>` — the eleven Query Type options
the combo box binds to, `List` first, never containing `Find`.

**IncludeStdlib**: `bool` — the global "Include standard library" checkbox
state, threaded through both `RefreshFromWorkspace` (as a candidate-filter
choice) and `BuildOptions` (as the `QueryOptions.IncludeStdlib` value for
every engine call).

**IsWorkspaceEmpty**: `bool` — `true` when the shell's current workspace has
zero sources, used to show the dialog's own empty-state hint alongside the
checkbox.

**SelectedQueryType**: `QueryVerb` — the currently-selected Query Type;
defaults to `List` (the always-available, no-selection-required option).

**HierarchyDirection**: `string?` — the direction option accepted by the
Hierarchy verb (`"up"`, `"down"`, or `"both"`); defaults to `"both"`. Only
attached to `QueryOptions.Direction` when `SelectedQueryType` is `Hierarchy`.

**WalkDepthText**: `string?` — the free-text input for the Impact verb's
optional walk-depth bound. Only parsed for `Impact`, and only when it parses
cleanly as a non-negative integer.

**CurrentResult**: `QueryResult?` — the most recently produced `QueryResult`
(either List's client-built list result or the engine's response), or
`null` when an element-scoped Query Type has no selection yet. Feeds the
shared results panel.

**CurrentResultRows**: `IReadOnlyList<QueryResultRow>` — a flattened,
view-friendly projection of `CurrentResult.Entries` (empty strings replacing
nullable fields, `Direction` mapped to a human-readable label, notes joined
into a tooltip string), bound directly by the results-panel `ItemsControl`.

**StatusMessage**: `string?` — user-visible error/hint text set on every
recoverable failure or prompt (no workspace, no selection yet, engine
argument rejection). Cleared on a successful recompute.

**HasCurrentResult**: `bool` — computed mirror of `CurrentResult is not
null`, bound by the results panel's right-click context-menu items'
`IsEnabled` so the copy actions are only enabled when there is something to
copy.

**ClipboardService**: `IClipboardService?` — the same clipboard-write seam
`DiagramDocumentViewModel` uses. Assigned by `QueryDialogView` to an
`AvaloniaClipboardService` anchored on the dialog's own `Window`.

#### Key Methods

**RefreshFromWorkspace**: Refreshes both `FilterOnly`'s and `Picker`'s
candidate lists (from the same candidate set) and `IsWorkspaceEmpty` from
the shell's current workspace, then recomputes the result.

- *Parameters*: none.
- *Returns*: `void` — both `FilterOnly.SetCandidates` and `Picker.SetCandidates`
  are called with the full candidate list (no default chip for either), and
  `RecomputeResult` runs immediately afterward.
- *Postconditions*: Called once at construction and again on every
  `IncludeStdlib` toggle; the dialog does not itself subscribe to
  `MainWindowShell.SourcesChanged`, matching the sibling dialogs' pattern.
  Because `ElementPickerViewModel.SetCandidates` unconditionally clears any
  prior `SelectedQualifiedName`, a stale selection can never linger past a
  workspace-derived refresh.

**RecomputeResult**: Recomputes `CurrentResult`/`CurrentResultRows` for the
currently selected `SelectedQueryType`. This is the redesign's entire
"no explicit Run gesture" mechanism, invoked automatically by every relevant
property change (Query Type, Hierarchy direction, Walk depth text, and — via
`FilterOnly`'s and `Picker`'s `PropertyChanged` subscriptions —
`FilterOnly.DisplayedItems`/`Picker.DisplayedItems`/`Picker.SelectedQualifiedName`).

- *Parameters*: none.
- *Returns*: `void` — `CurrentResult`, `CurrentResultRows`, and
  `StatusMessage` update in place.
- *Postconditions*: For `List`, delegates to `BuildListResult`. For every
  other Query Type: with no workspace, reports the existing empty-workspace
  message; with no `Picker.SelectedQualifiedName`, reports a helpful
  (non-error) prompt naming the Query Type and clears any stale prior
  result; otherwise dispatches through `QueryEngine.Execute`, gracefully
  catching `ArgumentException`. Never throws.

**BuildListResult**: Rebuilds `CurrentResult` and `CurrentResultRows` from
`FilterOnly`'s `DisplayedItems`.

- *Parameters*: none.
- *Returns*: `void` — `CurrentResult` becomes a `QueryResult` with
  `Verb="list"`, `Element=null`, one entry per displayed item.
- *Postconditions*: "List" is deliberately a purely-client-side filter; it
  does NOT call `QueryEngine.List`/`Find`. Invoked by `RecomputeResult`
  whenever `SelectedQueryType` is `List`.

**BuildOptions**: Builds a `QueryOptions` instance reflecting the current
form's verb-specific state.

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

QueryDialog treats every recoverable failure or informational prompt - no
workspace, no selection yet for an element-scoped Query Type, a picker
qualified name that vanished under a mid-session workspace change, or an
engine `ArgumentException` (thrown when the verb requires an element and
none is resolvable, or when the verb value itself is out of range) - as a
locally recoverable condition surfaced through `StatusMessage` rather than
an unhandled exception. Unlike the prior "leave the last result visible"
behavior, every one of these paths also clears `CurrentResult`/
`CurrentResultRows` so the results panel never shows a stale result left
over from a previous Query Type or selection.

#### Dependencies

- **MainWindowShell** — supplies `CurrentWorkspace` for candidate resolution
  and for the workspace argument passed to `QueryEngine.Execute`.
- **ElementPickerSubsystem** — `ElementFilter` via `FilterOnly` (used for
  "List"); `ElementPicker` via `Picker` (used for the other ten
  element-scoped Query Types).
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
