### WorkspacePanel

![AppShellSubsystem Structure](AppShellSubsystemView.svg)

#### Purpose

WorkspacePanel is the Dock tool panel that lets the user see, add, and remove
the workspace's file and folder sources as a tree, and accepts drag-and-drop
of files/folders as an alternative to picker dialogs. It is documented as one
unit covering both `WorkspacePanelToolViewModel` (the presentation/command
logic) and `WorkspacePanelToolView` (the Avalonia `TreeView`-based Dock tool
view), matching the pairing convention used by the other two tool panels
(`PredefinedViewsToolViewModel`/`View`, `DiagnosticsToolViewModel`/`View`).

#### Data Model

**RootNodes**: `IReadOnlyList<WorkspaceTreeNode>` — one `WorkspaceSourceNode`
per workspace source, in the same order as `MainWindowShell.CurrentWorkspace.Sources`.

**WorkspaceTreeNode**: abstract base with `required string Id { get; init; }`
— the tree control's item key — and `abstract MaterialIconKind IconKind`, the
icon shown alongside the node.

**WorkspaceSourceNode**: `WorkspaceTreeNode` with `required WorkspaceSource Source`
and `required IReadOnlyList<WorkspaceTreeNode> Children` — a source's node.
`Children` is empty for a `File`-kind source (a leaf, no expand arrow); for a
`Folder`-kind source it holds one entry per top-level subfolder
(`WorkspaceFolderNode`) or file (`WorkspaceFileNode`) directly under that
folder, preserving its on-disk hierarchy rather than flattening every
discovered file into a single list.

**WorkspaceFolderNode**: `WorkspaceTreeNode` with `required string Name` (the
folder's own last path segment, not its full path) and
`required IReadOnlyList<WorkspaceTreeNode> Children` — an intermediate,
purely-display grouping node for one subfolder discovered under a
`Folder`-kind source. It carries no identity used for removal (only whole
sources are removable) or file-watching (the owning source's watch scope
already covers everything beneath it).

**WorkspaceFileNode**: `WorkspaceTreeNode` with `required string FilePath`,
`required string SourceId`, and a computed `Name` (`Path.GetFileName(FilePath)`,
shown in the tree since ancestor nodes already convey the file's directory) —
a leaf node for one file. `FilePath` is a stable identity intended for a
future, out-of-scope, double-click read-only file viewer; it is preserved
even though nothing else currently reads it beyond display.

**SelectedNode**: `WorkspaceTreeNode?` — the tree node currently selected by
the user, used to resolve which source `RemoveSelected` acts on.

**IsEmpty**: `bool` — `true` when `RootNodes` is empty (zero workspace
sources), used to show the panel's root-level empty-state message.

**StatusMessage**: `string?` — set when a command (currently only Remove)
fails, so the view can surface the failure inline without a modal dialog.

#### Key Methods

**RebuildTree**: Rebuilds `RootNodes` from the shell's current workspace
state.

- *Parameters*: `None` — reads `MainWindowShell.CurrentWorkspace.Sources` and
  `MainWindowShell.CurrentSourceIdToFiles`.
- *Returns*: `void` — `RootNodes` and `IsEmpty` update in place.
- *Postconditions*: One `WorkspaceSourceNode` exists per current source, each
  with the correct `Children` shape for its kind; overlap dedupe already
  resolved upstream by `WorkspaceSourceSet.Resolve()` is reflected as a single
  attribution in the tree shape (a deduplicated file appears once, under its
  attributed owning source, not once per overlapping source). A folder
  source's flat, per-source file list is grouped by each file's position
  relative to that source's own path, via a private `BuildFolderChildren`
  helper, so the resulting `Children` mirror the folder's on-disk
  hierarchy — subfolders (as `WorkspaceFolderNode`s, sorted before files) and
  files (as `WorkspaceFileNode`s), each level sorted alphabetically by name.
  Called eagerly from the constructor and every time
  `MainWindowShell.SourcesChanged` fires.

**AddFile** / **AddFolder**: Commands that raise `RequestAddFile` /
`RequestAddFolder` so the Avalonia-aware view can fulfill them with a real
picker (this view model has no direct `StorageProvider` access) and call back
into `MainWindowShell.AddFileSourceAsync` / `AddFolderSourceAsync`.

- *Parameters*: `None`.
- *Returns*: `void` — an event is raised; the view performs the actual add.

**RemoveSelected**: Removes the source owning `SelectedNode`.

- *Parameters*: `None` — uses `SelectedNode`.
- *Returns*: `Task` — completes once the shell has removed the source and
  reapplied the resulting snapshot.
- *Postconditions*: If nothing is selected, this is a no-op. Otherwise
  resolves the owning source id (`WorkspaceSourceNode.Source.Id` or
  `WorkspaceFileNode.SourceId`) and calls
  `MainWindowShell.RemoveSourceAsync(sourceId)`; a thrown failure is caught
  and surfaced via `StatusMessage` rather than propagated, since a failed
  remove should not crash the shell.

#### Error Handling

WorkspacePanel treats a failed `RemoveSelectedAsync` as a locally recoverable
condition surfaced through `StatusMessage`, since the shell's workspace state
remains valid regardless of whether the remove succeeded. It does not perform
its own file-system validation for drag-and-drop drops: the view checks
`File.Exists`/`Directory.Exists` per dropped path and calls
`MainWindowShell.AddFileSourceAsync`/`AddFolderSourceAsync` exactly as the
picker-driven commands do, so add-time failures (for example a folder that
disappears between the drop and the add call) surface through the same shell
exception path as picker-driven adds.

#### Dependencies

- **MainWindowShell** — sole source of truth for `CurrentWorkspace.Sources`
  and `CurrentSourceIdToFiles`; owns `AddFileSourceAsync`,
  `AddFolderSourceAsync`, `RemoveSourceAsync`, and `SourcesChanged`, all of
  which this unit calls or subscribes to but never reimplements.
- **Avalonia** — `WorkspacePanelToolView` is a `TreeView`-based Dock tool
  view; the view (not the view model) owns `StorageProvider` pickers and
  `DragDrop` handling.
- **Dock** — `WorkspacePanelToolViewModel` is a `Dock.Model.Mvvm.Controls.Tool`,
  hosted in `WorkbenchDockFactory`'s layout alongside the other two panels.

#### Callers

- **WorkbenchDockFactory** — constructs the Dock layout tab hosting this
  panel.
- **MainWindowView** — constructs the view model instance shared with the
  Dock factory and wires `RequestAddFile`/`RequestAddFolder` to real pickers.
