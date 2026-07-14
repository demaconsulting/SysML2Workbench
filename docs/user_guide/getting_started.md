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
2. Choose **File > Open Workspace...** and pick the folder containing your
   `.sysml` files.
3. SysML2Workbench discovers every `.sysml` file under the folder (matching
   the same glob-based discovery the SysML2Tools CLI uses), parses them, and
   resolves `import` relationships across files, exactly as the CLI would.
4. Once loaded, the **Predefined Views** list (left panel) is populated with
   every view usage declared in the workspace, and the **Diagnostics** panel
   (bottom) lists any parser or reference-resolution problems found across
   the whole workspace. Panels are docked in this default arrangement but can
   be resized, floated, or closed like any other dockable panel. If you
   close the **Predefined Views**, **Custom View Builder**, or
   **Diagnostics** panel, reopen it from the **View** menu - selecting it
   there restores the panel to its original dock without losing any
   in-progress state (for example, a partially-built custom view).

While a workspace is open, SysML2Workbench watches the folder for external
changes. If you edit a `.sysml` file in another editor, or `git pull` new
changes, the workspace is incrementally reloaded and the active diagram and
diagnostics list refresh automatically - no need to reopen the folder.

## Browsing Predefined Views

Select any entry in the **Predefined Views** list to render it. Supported
kinds are General, Interconnection, State Transition, Action Flow, Sequence,
and Grid diagrams - the same kinds produced by the SysML2Tools CLI.

The rendered diagram appears as an interactive SVG in the center canvas:

- **Zoom**: scroll the mouse wheel over the diagram.
- **Pan**: click and drag with the mouse.

## Building a Custom View

The **Custom View Builder** panel (right side, dockable) lets you construct an
ad-hoc view without writing SysML syntax:

1. Pick a **View Kind** from the dropdown (General, Interconnection, State
   Transition, Action Flow, Sequence, or Grid).
2. In the **Available** list, select an element or package from the loaded
   workspace and click **Add →** to move it into **Selected**. Repeat for
   every element or package you want exposed - custom views support multiple
   exposed elements, just like a hand-written SysML view.
3. For each row in **Selected**, choose its **recursion kind** from the
   dropdown - *This element only*, *This element + everything below
   (::\*\*)* (the default), *Direct children only (::\*)*, or *All
   descendants, not itself (::\*::\*\*)* - and optionally enter a
   **bracket-filter expression** (enabled only for the two recursive kinds)
   to narrow that target's exposed membership. Click **Remove** to drop a
   row entirely.
4. Optionally enter a **Filter Expression** to narrow what is included.
5. Optionally enter a **View Name** - otherwise a default name is used.
6. Click **Preview** to render the custom view live in the center canvas,
   with the same pan/zoom controls as a predefined view.
7. Click **Copy as SysML** to copy valid `view ... expose ...` SysML v2 text
   to the clipboard. Each `expose` target is emitted using its exact
   qualified name and selected recursion kind/bracket filter; Phase 0 does
   not support renaming an exposed target (there is no `as` alias support),
   regardless of whether a custom view name was supplied. Paste this into a
   `.sysml` file to promote the
   ephemeral preview into a permanent, version-controlled view definition.

Custom views built in the GUI are **session-only** - they are not saved to
disk by SysML2Workbench itself. The "Copy as SysML" snippet is the only way
to persist a custom view, by pasting it into your own model file.

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
