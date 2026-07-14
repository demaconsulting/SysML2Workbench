## Dock

SysML2Workbench uses AvaloniaUI/Dock as the resizable, floatable, closable
panel-docking framework for the desktop application shell, replacing a fixed
four-region `DockPanel` layout.

### Purpose

Dock was chosen because it provides a mature, MVVM-friendly docking layout
engine on top of Avalonia, letting the shell's four Phase-0 panels (predefined
views, custom view builder, diagnostics, and the diagram surface) become
independently resizable, floatable, and closable without the workbench
implementing its own splitter/panel-management code.

### Features Used

- **`Dock.Model.Mvvm` layout model** — `Factory`, `Tool`, `Document`,
  `ToolDock`, `DocumentDock`, and `ProportionalDock` compose the panel layout
  and its default proportions.
- **`Dock.Avalonia` `DockControl`** — hosts the composed layout inside the
  main window and supplies the resizing, floating, and closing chrome.
- **`Dock.Avalonia.Themes.Fluent` `DockFluentTheme`** — styles the Dock
  chrome to match the existing Fluent theme.
- **Convention-based view resolution** — a hand-written `IDataTemplate`
  (Dock's documented "Option B") maps each panel view model to its view by
  naming convention, avoiding a source-generator dependency.

### Integration Pattern

`WorkbenchDockFactory` composes the four panel view models
(`PredefinedViewsToolViewModel`, `CustomViewBuilderToolViewModel`,
`DiagnosticsToolViewModel`, `DiagramDocumentViewModel`) into a
`ProportionalDock`/`ToolDock`/`DocumentDock` tree approximating the legacy
fixed layout's default proportions. `MainWindowView` builds this layout once
per window and binds it to a `DockControl`; `MainWindowShell` itself has no
Dock dependency, preserving its existing zero-Avalonia-dependency design.
Layout serialization/persistence is out of scope for this phase.

### Restoring Closed Tools

`WorkbenchDockFactory` sets `HideToolsOnClose = true` (a `FactoryBase`
setting), so closing a `Tool` through Dock's own chrome (the tab's "x"
button) calls `HideDockable` instead of permanently removing it: the
dockable is taken out of its owning `ToolDock`'s `VisibleDockables`, its
`OriginalOwner` is recorded, and it is tracked in `IRootDock.HiddenDockables`
without the view model instance ever being destroyed or recreated. `Tool`
exposes this as a normal, bindable `IsOpen` bool that Dock keeps in sync in
both directions.

`MainWindowView`'s "View" menu is the UI entry point for bringing a hidden
`Tool` back: each menu item's `Click` handler calls, on the same
`WorkbenchDockFactory` instance, `RestoreDockable(tool)` (a safe no-op if the
tool isn't currently hidden) followed by `SetActiveDockable(tool)` and
`SetFocusedDockable(ownerDock, tool)`. `RestoreDockable` re-adds the exact
existing dockable instance to its original `ToolDock`, so any in-progress
panel state (for example, an unsaved custom-view-builder definition) is
preserved across a close/restore cycle. Each menu item's `IsChecked` is
bound one-way to the panel's `IsOpen` property purely as a visual indicator;
the `Click` handler never hides an already-open panel, so clicking the
menu item for a visible panel only (re)focuses it.
