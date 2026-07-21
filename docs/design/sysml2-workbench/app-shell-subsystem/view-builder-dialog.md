### ViewBuilderDialog

![AppShellSubsystem Structure](AppShellSubsystemView.svg)

#### Purpose

ViewBuilderDialog is the modal dialog opened from the main window's View menu
that lets the user compose a custom SysML view - view kind, expose targets,
and filter expression - with a live-updating SVG preview, then commit it as a
brand-new diagram tab or cancel with zero side effects. It is documented as
one unit covering both `ViewBuilderDialogViewModel` (the Avalonia-free
composition state and commit logic) and `ViewBuilderDialogView` (the Avalonia
`Window` shown via `ShowDialog`), matching the pairing convention used by the
other dialog unit in this subsystem (`AboutDialogViewModel`/`View`). Like
`AboutDialog`, ViewBuilderDialog is not a Dock tool: it is a short-lived,
owned dialog `Window`, constructed fresh every time the user opens it, so it
carries no state over from a prior session - unlike the docked
`CustomViewBuilderToolViewModel` it supersedes, which lived for the whole
application session.

#### Data Model

**Shell**: `MainWindowShell` — the shared application shell, used to resolve
expose-target candidates, render the live preview, and (only on commit)
open/populate/roll back a real diagram tab.

**Definition**: `ViewDefinitionModel` — the custom-view definition being
composed by this dialog session. Mutated directly by the expose-target row
controls' event handlers, the same way the deleted docked panel mutated its
own `BuilderDefinition`.

**PreviewCanvas**: `SvgCanvasHost` — the dialog's own diagram surface, holding
whatever SVG `RenderPreview` most recently produced. Never shared with, nor
derived from, any tab tracked by `Shell`.

**AvailableExposeTargets**: `IReadOnlyList<string>` — qualified names offered
by the "Expose Targets" tab's add-target picker, drawn from
`Shell.CurrentWorkspace.Workspace.Declarations`, excluding stdlib names and
node kinds that would fail `ViewDefinitionModel` validation (views,
viewpoints, imports, metadata, transitions, connections). Remains the
master, unfiltered, sorted list; `DisplayedExposeTargets` (below) is what the
picker `ListBox` actually binds to.

**AvailableExposeTargetTypeLabels**: `IReadOnlyList<string>` — every distinct
human-readable "type label" present among the current `AvailableExposeTargets`
candidates, sorted ordinally. Each candidate's label is computed from its
underlying `SysmlNode` subtype: a `SysmlDefinitionNode` yields its
`DefinitionKeyword` (e.g. `"part def"`), a `SysmlFeatureNode` yields its
`FeatureKeyword` (e.g. `"part"`), and `SysmlPackageNode`/`SysmlDependencyNode`/
`SysmlSatisfyNode` yield the fixed literals `"package"`/`"dependency"`/
`"satisfy"` respectively (none of these three expose a keyword-like property
of their own). Any other node kind not already excluded from the picker falls
back to a defensively derived label (its runtime type name with a leading
`Sysml` and/or trailing `Node` stripped, lowercased), so an unrecognized
future node kind is labeled reasonably rather than crashing or silently
vanishing from the picker.

**ActiveExposeTypeFilters**: `ObservableCollection<string>` — the type-label
chips currently applied over the picker, combined with OR semantics (an item
is shown when its type label is any one of these); empty means no type
restriction (every candidate's type is shown). `RefreshFromWorkspace`
pre-populates this with just `"part"` when that label is present in the
current workspace, since narrowing to part usages is the most common
starting point; otherwise it starts empty. Mutated only through
`AddExposeTypeFilter`/`RemoveExposeTypeFilter` (or, defensively, any other
direct mutation, which is also observed) rather than replaced wholesale, so
the view's chip-row `ItemsControl` can bind to this instance directly.

**ExposeTargetSearchText**: `string?` — the picker's free-text search box
value, two-way bound from the view. A non-empty value narrows
`DisplayedExposeTargets` to qualified names containing it as a
case-insensitive substring, applied with AND semantics against whatever the
active type filter already narrowed to.

**DisplayedExposeTargets**: `IReadOnlyList<string>` — the picker `ListBox`'s
actual data source: `AvailableExposeTargets`, narrowed first by
`ActiveExposeTypeFilters` (OR semantics) and then by `ExposeTargetSearchText`
(AND semantics), preserving the master list's ordinal sort order. Recomputed
whenever any of the three inputs changes.

**StatusMessage**: `string?` — set when `RenderPreview` or `TryCommit` fails,
so the view can surface the failure inline without a modal message box.

**IsWorkspaceEmpty**: `bool` — `true` when the shell's current workspace has
zero sources, used to show the dialog's own empty-state message instead of an
unusable, target-less picker.

#### Key Methods

**RefreshFromWorkspace**: Refreshes `AvailableExposeTargets`,
`AvailableExposeTargetTypeLabels`, `ActiveExposeTypeFilters`,
`DisplayedExposeTargets`, and `IsWorkspaceEmpty` from the shell's current
workspace state.

- *Parameters*: none.
- *Returns*: `void` — all listed properties update in place.
- *Postconditions*: Called once at construction; the dialog is short-lived
  (opened, edited, closed) so it does not itself subscribe to
  `MainWindowShell.SourcesChanged` the way the old long-lived panel view
  model did. Re-applies the `"part"`-default chip rule fresh each call.

**AddExposeTypeFilter** / **RemoveExposeTypeFilter**: Add or remove a type
label from `ActiveExposeTypeFilters` and recompute `DisplayedExposeTargets`.

- *Parameters*: the type label to add or remove.
- *Returns*: `void` — `ActiveExposeTypeFilters` and `DisplayedExposeTargets`
  update in place.
- *Postconditions*: `AddExposeTypeFilter` is dedupe-safe (adding an
  already-active label is a no-op beyond the recompute); `RemoveExposeTypeFilter`
  no-ops when the label is not currently active, rather than throwing.

**AddExposeTarget** / **RemoveExposeTarget** / **SetExposeRecursionKind** /
**SetExposeBracketFilter**: Port the deleted `CustomViewBuilderToolViewModel`'s
add/edit/remove expose-target semantics onto `Definition`.

- *Parameters*: qualified name, recursion kind, and/or optional bracket-filter
  expression, matching each operation's purpose.
- *Returns*: `void` — `Definition` updates in place.
- *Postconditions*: Each mutator calls `RenderPreview()` immediately
  afterward, so every expose-target edit is reflected in the live preview.

**SetViewKind** / **SetFilterExpression** / **SetDisplayName**: Update the
corresponding `Definition` property and re-render the live preview.

- *Parameters*: the new value (or `null`/whitespace to clear, for the latter
  two).
- *Returns*: `void` — `Definition` updates in place.
- *Postconditions*: Each setter calls `RenderPreview()` immediately
  afterward. `SetDisplayName` does not itself affect the rendered SVG shape,
  but still triggers a preview refresh so every edit behaves consistently.

**RenderPreview**: Renders `Definition` via
`MainWindowShell.RenderCustomViewPreview` and loads the result into
`PreviewCanvas`.

- *Parameters*: none.
- *Returns*: `void` — `PreviewCanvas` and `StatusMessage` update in place;
  `PreviewChanged` is raised.
- *Postconditions*: Never throws: an incomplete or invalid definition (for
  example no view kind selected yet) is reported through `StatusMessage`
  instead, since this method runs after every single control edit and a
  mid-edit definition is routinely incomplete. On failure, `PreviewCanvas` is
  cleared via `SvgCanvasHost.Clear` so a previously-rendered SVG never
  lingers on screen once the current configuration no longer corresponds to
  it.

**TryCommit**: Commits the current `Definition` as a brand-new diagram tab.

- *Parameters*: `out string? error` — failure reason when the method returns
  `false`.
- *Returns*: `bool` — `true` when a new tab was opened and successfully
  rendered; `false` when the definition failed to render.
- *Postconditions*: Calls `Shell.OpenNewCustomPreviewTab()` then
  `Shell.PreviewCustomView(Definition)` inside one try/catch. On success, the
  new tab remains open and becomes active. On failure, `Shell.CloseDiagramTab`
  rolls back the just-opened empty tab so no partial/empty tab is ever left
  behind, and the failure is reported via both `error` and `StatusMessage`.
  Only this method - the dialog's OK button - ever creates a real tab on
  `Shell`; the Cancel path performs zero calls into `Shell` at all.

#### Error Handling

ViewBuilderDialog treats every render failure - whether from an in-progress
edit (`RenderPreview`) or from the commit attempt (`TryCommit`) - as a locally
recoverable condition surfaced through `StatusMessage` rather than an
unhandled exception, since a mid-edit or invalid definition is an expected,
routine state while the user is composing a view. `TryCommit`'s rollback
guarantees the shell's tracked tab state never reflects a failed commit: the
open-then-render-then-close-on-failure sequence means a caller can retry
after fixing the definition without first cleaning up a stray empty tab.

#### Dependencies

- **MainWindowShell** — supplies `CurrentWorkspace` for expose-target
  candidates, `RenderCustomViewPreview` for the side-effect-free live
  preview, and `OpenNewCustomPreviewTab`/`PreviewCustomView`/
  `CloseDiagramTab` for the commit-with-rollback sequence.
- **ViewDefinitionModel** — the custom-view definition being composed;
  validates itself against the workspace when rendered.
- **SvgCanvasHost** — displays the dialog's own live preview.
- **Avalonia** — `ViewBuilderDialogView` is a `Window` shown modally via
  `ShowDialog`, mirroring `AboutDialogView`'s pattern; its right-hand
  `TabControl` presents "View Kind", "Expose Targets", and "Filter & Name"
  tabs.

#### Callers

- **MainWindowView** — constructs `ViewBuilderDialogView` and calls
  `ShowDialog(this)` when the user selects the View menu's
  "Custom View Builder..." item, so the dialog is owned by and centers over
  the main window.
