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
