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

`WorkbenchDockFactory` composes the three Tool panel view models
(`PredefinedViewsToolViewModel`, `CustomViewBuilderToolViewModel`,
`DiagnosticsToolViewModel`) into a `ProportionalDock`/`ToolDock`/`DocumentDock`
tree approximating the legacy fixed layout's default proportions, with the
`DocumentDock` initially empty; `DiagramDocumentViewModel` instances are added
to and removed from it dynamically at runtime as diagram tabs open and close
(see "Diagram Document Tabs" below). `MainWindowView` builds this layout once
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

### Diagram Document Tabs

Unlike the three Tool panels, the diagram area hosts zero or more
independently closable `Document`s - one `DiagramDocumentViewModel` per open
`WorkbenchTab` - added and removed dynamically at runtime rather than fixed
at layout-construction time. This depends on two further Dock APIs beyond
the ones the Tool panels use:

- **`IDockable.IsCollapsable = false`** — set on the `DocumentDock` instance
  `WorkbenchDockFactory.CreateLayout()` builds (exposed as `DiagramDock`).
  Dock's default document-close path (`CloseDockable` → `RemoveDockable(...,
  collapse: true)` → `CollapseDock(owner)`) removes an emptied `DocumentDock`
  from its parent `ProportionalDock` branch once its last document closes,
  unless the `DocumentDock`'s own `IsCollapsable` is `false`, in which case
  `CollapseDock` returns immediately and the (now empty) `DocumentDock`
  stays exactly where it is in the layout tree. This is what keeps the
  diagram area visibly present even when zero diagram tabs are open, rather
  than a Tool-panel-style "closed and gone until restored" state - there is
  no restore path for a `Document`, so an empty-but-present container is the
  only way to avoid a dead end.
- **`IDocumentDock.EmptyContent = null`** — `Dock.Model.Mvvm.Controls.DocumentDock`
  defaults `EmptyContent` to the literal string `"No documents open"`,
  rendered centered in the document area whenever it has zero visible
  dockables. Cleared to `null` on `DiagramDock` so the diagram area with
  zero open tabs is a plain blank area (matching Visual Studio's editor
  region with no files open), not a placeholder message.
- **`MainWindowView.axaml`'s `DockControl.Styles` override for the empty-state
  border** — the Fluent Dock theme's `DocumentControl` control template
  (`Dock.Avalonia.Themes.Fluent/Controls/DocumentControl.axaml`) draws its
  content area inside a `Border` named `PART_Border`, whose 1px
  `DockDocumentContentBorderBrush`/`DockDocumentContentBorderThickness` is
  applied unconditionally by that template, regardless of whether the dock
  has any visible documents. Headless-rendered pixel capture
  (`test/OtsSoftwareTests/DockTests.cs`,
  `DiagramDock_EmptyArea_HasNoVisibleBorder`) confirmed this border is
  genuinely drawn identically in both states - it is not conditionally
  hidden or masked by tab content - it is simply imperceptible once a
  diagram tab's opaque content fills the area, but stands out sharply as a
  visible gray box around the otherwise-blank area once `EmptyContent =
  null` leaves zero open tabs with nothing else painted there. Since Dock's
  model classes (`DocumentDock`/`IDockable`) expose no per-instance hook for
  overriding template-level border resources (unlike `EmptyContent`, a
  bindable model property), the fix lives in the Avalonia control layer:
  `MainWindowView.axaml`'s single `dock:DockControl` declares a
  `DockControl.Styles` override targeting
  `DocumentControl[HasVisibleDockables=False] /template/ Border#PART_Border`
  (`HasVisibleDockables` is `DocumentControl`'s own styled property, the same
  one its template already uses to toggle `PART_ContentPresenter`/
  `PART_EmptyContentHost` visibility) and sets that border's
  `BorderThickness` to `0` only in the zero-visible-documents state. Because
  there is exactly one `DockControl` (and therefore exactly one
  `DocumentDock`/`DiagramDock`) in the app today, this is scoped as narrowly
  as `DiagramDock`-only without needing any change to
  `WorkbenchDockFactory.cs`, and it leaves the with-tab-open appearance
  completely untouched (verified by the same headless test's pixel
  comparison against the with-tab-open state).
- **`IFactory.FocusedDockableChanged`/`OnDockableClosed`** — `MainWindowView`
  subscribes to `WorkbenchDockFactory`'s inherited `FocusedDockableChanged`
  event to forward Dock's own tab-focus tracking to
  `MainWindowShell.NotifyActiveDiagramTab` whenever focus lands on a diagram
  document (focus changes onto a Tool panel are ignored), and
  `WorkbenchDockFactory` overrides `OnDockableClosed` to raise a
  `DiagramTabClosed` event when a diagram document closes through Dock's own
  chrome, which `MainWindowView` uses to call
  `MainWindowShell.CloseDiagramTab`.

`Document`s and `Tool`s therefore have deliberately different close
semantics in this codebase: a closed `Tool` is hidden and restorable via the
View menu (see above), while a closed diagram `Document` is removed outright
with no restore path - closing a diagram tab is always a safe operation
(zero tabs open is a supported, first-class state), and reopening one is one
click away via the catalog or the "+ New Diagram Tab" button, so no
restore mechanism is needed.
