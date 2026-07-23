# Getting Started

SysML2Workbench is a read-only desktop viewer for SysML v2 textual models,
with a GUI builder for ad-hoc custom views. This guide walks through the
Phase 0 workflow: opening a workspace, browsing predefined views, building a
custom view, and using the diagnostics panel and log file. This section
assumes SysML2Workbench is already installed; see the Installation section
for setup prerequisites.

## Opening a Workspace

1. Launch the application (`dotnet run --project
   src/DemaConsulting.SysML2Workbench.Desktop`, or run the published
   executable).
2. Build up your workspace from any combination of files and folders:
   - Choose **File > Open Folder...** and pick a folder containing `.sysml`
     files, or
   - Choose **File > Open File...** and pick a single `.sysml` file, or
   - Drag files and/or folders from your file manager onto the main window
     or the **Workspace** panel.

   Each addition is additive - adding a second folder does not replace the
   first, so a workspace can be assembled from several unrelated folders
   and/or individual files at once. If a file happens to fall both inside an
   added folder and is also added individually, it is only loaded once;
   overlapping folders are deduplicated the same way.
3. For each added folder, SysML2Workbench discovers every `.sysml` file
   under it (matching the same glob-based discovery the SysML2Tools CLI
   uses); each added file is loaded directly. All discovered/added files are
   merged, parsed, and `import` relationships resolved across the whole set,
   exactly as the CLI would.
4. Once loaded, the **Workspace** panel (left side, dockable) lists every
   added source as a tree - folders expand to show the files discovered
   under them, files appear as non-expandable entries - and lets you add
   more sources or remove one via its toolbar or a right-click/selection
   plus **Remove**. The **Predefined Views** list is populated with every
   view usage declared in the workspace, and the **Diagnostics** panel
   (bottom) lists any parser or reference-resolution problems found across
   the whole workspace. Panels are docked in this default arrangement but can
   be resized, floated, or closed like any other dockable panel. If you
   close the **Workspace**, **Predefined Views**, or **Diagnostics** panel,
   reopen it from the **View** menu - selecting it there restores the panel
   to its original dock without losing any in-progress state.
5. Removing every source returns the workspace to its empty starting state:
   the **Predefined Views** and **Diagnostics** panels each show a friendly
   "workspace is empty" message instead of rendering against nothing, and
   any open diagram tabs are closed. This is the same state the application
   starts in before any source is added, not an error condition. Choosing
   **File > Close All** does the same thing in one step - it clears every
   added source at once, without needing to remove them one at a time.

While a workspace is open, SysML2Workbench watches every added file and
folder independently for external changes. If you edit a `.sysml` file in
another editor, add a new file under a watched folder, or `git pull` new
changes, the workspace is incrementally reloaded and the active diagram and
diagnostics list refresh automatically - no need to reopen anything. A
change under one added folder never affects another added folder's watch
scope.

## Browsing Predefined Views

Select any entry in the **Predefined Views** list to render it. Supported
kinds are General, Interconnection, State Transition, Action Flow, Sequence,
and Grid diagrams - the same kinds produced by the SysML2Tools CLI.

Each rendered view opens in its own diagram tab in the center of the window,
identified by the view's name. Selecting a predefined view that is already
open switches focus to its existing tab instead of opening a duplicate.
Diagram tabs are closed like any other dockable document - click the close
("x") chrome on the tab. You can have any number of diagram tabs open at
once, or none at all; the diagram area itself always stays visible even with
every tab closed, ready to host the next view you open.

The rendered diagram appears as an interactive SVG in its tab's canvas:

- **Zoom**: scroll the mouse wheel over the diagram.
- **Pan**: click and drag with the mouse.

Each diagram tab has its own independent pan/zoom state, so switching
between tabs never disturbs another tab's view of its diagram.

Every open diagram tab - whether it renders a predefined view or a custom
view preview - supports right-click > **Copy as SysML** to copy that tab's
`view { ... }` SysML v2 text to the clipboard. The option is disabled when
no concrete view definition can be derived for the tab, for example an
unscoped predefined view or a custom-view preview tab that has not yet been
rendered.

## Viewing a File's Raw Source

Double-click any `.sysml` file in the **Workspace** panel's tree to open its
raw source text, read-only, with SysML v2 syntax highlighting, in its own
tab in the same tabbed area as diagram tabs. Double-clicking the same file
again switches focus to its already-open tab instead of opening a
duplicate. This is a view-only capability - editing and saving `.sysml`
files is a future phase (see "Out of Scope" below).

## Building a Custom View

Choose **View > Custom View Builder...** to open the View Builder dialog. The
dialog shows a live SVG preview on the left and a set of tabs on the right for
composing the view; every edit on the right immediately re-renders the
preview on the left, so you always see the current result before committing
to it.

1. On the **View Kind** tab, pick a kind (General, Interconnection, State
   Transition, Action Flow, Sequence, or Grid).
2. On the **Expose Targets** tab, narrow the picker if needed, then pick an
   element or package and click **Add** to add it to the exposed-targets
   list. The picker starts filtered to a single **part** type chip (the most
   common starting point); click **✕** on a chip to remove it, or click **+**
   to open a flyout listing every other type label present in the workspace
   (for example *part def*, *package*, *dependency*) and pick one to add
   another chip - active chips combine so an element matching any one of
   them is shown, and removing every chip lifts the type restriction
   entirely. Use the **Filter by name...** search box alongside the chips to
   further narrow the list to qualified names containing the typed text,
   case-insensitively; the type-chip and name-search filters combine
   together. Repeat **Add** for every element or package you want exposed -
   custom views support multiple exposed elements, just like a hand-written
   SysML view. For each row, choose its **recursion kind** - *This element
   only*, *This element + everything below (::\*\*)* (the default), *Direct
   children only (::\*)*, or *All descendants, not itself (::\*::\*\*)* - and
   optionally enter a **bracket-filter expression** (enabled only for the two
   recursive kinds) to narrow that target's exposed membership. Click
   **Remove** to drop a row entirely.
3. On the **Filter & Name** tab, optionally enter a **Filter Expression** to
   narrow what is included, and optionally enter a **View Name** - otherwise
   a default name is used.
4. Click **OK** to commit the current definition as a brand-new diagram tab
   in the main window, and the dialog closes. Click **Cancel** at any point
   to discard the whole editing session instead - the dialog simply closes
   and nothing changes in the main window, no matter how much you had
   composed.

Every diagram tab produced this way - like a predefined-view tab - supports
right-click > **Copy as SysML** to copy that tab's `view { ... }` SysML v2
text to the clipboard. Each `expose` target is emitted using its exact
qualified name and selected recursion kind/bracket filter; Phase 0 does not
support renaming an exposed target (there is no `as` alias support),
regardless of whether a custom view name was supplied. Paste this into a
`.sysml` file to promote the ephemeral preview into a permanent,
version-controlled view definition.

Custom views built in the GUI are **session-only** - they are not saved to
disk by SysML2Workbench itself. The "Copy as SysML" snippet is the only way
to persist a custom view, by pasting it into your own model file.

## Running a Query

Choose **Query > Run Query...** to open the Query dialog. The dialog is a
single form (no tabs, no "Run" button) with a shared results panel on the
right; use the **Include standard library** checkbox at the top to control
whether stdlib qualified names appear as candidates, and the **Query Type**
combo box below it to choose what to see:

- **List** (the default) is a live client-side filter over the whole
  workspace, shown by a dedicated filter-only control - the chip row (with
  **+** to add and **✕** to remove type filters) and the **Filter by
  name...** search box, the same way as in the Custom View Builder - with
  no selectable list, since **List** has no target-element concept at all.
  The results panel updates as you type, showing one entry per candidate
  that matches.
- The other ten entries (Describe, Uses, Used By, Dependencies, Impact,
  Hierarchy, Requirements, Interface, Connections, States) each need a
  target element: the dialog instead shows the element picker (the same
  chip row and search box, plus a selectable candidate list) below the
  Query Type combo - pick an element there to use it as the query's target.
  Two of them expose extra controls: **Hierarchy** shows a Direction
  dropdown (*up*, *down*, *both*), and **Impact** shows an optional Walk
  Depth text box (leave blank for no bound).

Every change updates the results panel immediately - there is no Run
button. Changing the Query Type, adding or removing a chip, editing the
search text, picking an element, changing Direction or Walk Depth, or
toggling **Include standard library** all recompute the results panel
synchronously. If you choose one of the ten element-scoped query types
before picking an element, the panel shows a prompt asking you to select
one instead of leaving a stale result on screen.

The results panel shows a bullet-list summary and a table of matching
entries with Qualified Name, Kind, and Detail columns; the **Dependencies**
verb adds a Direction column. Any per-entry notes appear as a tooltip on
that entry's row.

Right-click the results panel and choose **Copy as Markdown** or **Copy as
JSON** to place the rendered result on the clipboard for pasting into a
review, a bug report, or another tool. Query results are session-only -
closing the dialog discards them.

## Diagnostics Panel

The **Diagnostics** panel lists every parser and reference-resolution problem
found anywhere in the currently loaded workspace, refreshed automatically on
every reload (initial open or external file change). Each entry shows the
affected file, location, severity, and message.

## Local Log File

SysML2Workbench writes a local rolling log file recording notable operations
(workspace opens/reloads, view renders, errors) for troubleshooting and bug
reports. No telemetry is ever sent over the network - the log stays entirely
on your machine, under:

- Windows: `%LOCALAPPDATA%\SysML2Workbench\logs\`
- Linux/macOS: the platform's equivalent local application data folder.

Attach the relevant log file when filing a bug report.

## Out of Scope (Phase 0)

The following are intentionally not implemented in this release:

- Git awareness or version-control integration.
- Click-to-navigate between diagnostics/diagram and source text.
- Text editing or structural/graphical editing of `.sysml` files.
- Telemetry, crash reporting, or authentication.
- Performance engineering for very large models.
