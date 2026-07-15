## Dock

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/DockTests.cs` verify Dock's layout composition and Avalonia hosting behavior directly: a plain xUnit `[Fact]` walks the real `WorkbenchDockFactory`-produced dock tree to confirm the Phase-0 panel view models (including a dynamically added diagram document) are genuinely reachable, and several `[AvaloniaFact]` tests host a real `Dock.Avalonia.Controls.DockControl` inside a headless `Window` to confirm Avalonia can render the composed layout without throwing, that a closed Tool panel can be restored to its original dock, that the diagram document area persists in the layout tree even with zero open diagram tabs, and that diagram documents can be dynamically added, removed, and focus-tracked at runtime.

### Test Scenarios

**CreateLayout_ComposesFourPanelDockables**: `WorkbenchDockFactory.CreateLayout()` is called with real Tool panel view model instances, a `DiagramDocumentViewModel` is added dynamically via `AddDockable`, and the resulting `IRootDock` is walked recursively, confirming all four panel view models (predefined views, custom view builder, diagnostics, diagram) are present as dockables in the composed tree.

**DockControl_HostsWorkbenchLayout**: A real `DockControl` is assigned the factory-produced layout (with one diagram document dynamically added) and shown inside a headless `Window`, proving Dock's Avalonia control genuinely accepts and hosts the composed layout.

**RestoreDockable_ReopensClosedToolInOriginalDock**: A real `WorkbenchDockFactory` layout is hosted in a headless `DockControl`/`Window`, `CloseDockable` is called on the predefined-views panel, confirming it leaves its original `ToolDock`'s `VisibleDockables` and is tracked in `IRootDock.HiddenDockables` (proving `HideToolsOnClose` hides rather than destroys it), and then `RestoreDockable` is called, confirming the exact same view model instance is back in its original `ToolDock`'s `VisibleDockables`.

**DocumentDock_StaysInLayoutTree_AfterLastDocumentCloses**: A `WorkbenchDockFactory` layout is built with zero initial diagram documents, one is added via `AddDockable` and hosted in a real `DockControl`/`Window`, then closed via `CloseDockable`; confirms the same `DiagramDock` instance is still reachable from the root's dockable tree with `VisibleDockables.Count == 0`, rather than the container itself collapsing or disappearing - directly verifying the `IsCollapsable = false` research finding end to end through the real Dock control.

**AddDockable_And_CloseDockable_DynamicallyManageDiagramDocuments**: Two `DiagramDocumentViewModel`s are added dynamically via `AddDockable` to a real, hosted `DockControl`'s layout, confirming both are reachable and counted; one is then closed via `CloseDockable`, confirming it is removed while the other remains, and that `DiagramTabClosed` fires exactly once with the closed view model as its argument.

**FocusedDockableChanged_FiresForActiveDockableChangesOnDiagramDocuments**: Two `DiagramDocumentViewModel`s are added to a `WorkbenchDockFactory` layout, and `SetFocusedDockable` is called to move focus to the second one, confirming `FocusedDockableChanged` fires with that document as its argument - the mechanism `MainWindowView` relies on to forward Dock's own focus tracking to `MainWindowShell.NotifyActiveDiagramTab`.

**DiagramDock_EmptyArea_HasNoVisibleBorder**: A real `MainWindowView` (hosting the actual `DockControl` and its `DockControl.Styles` border-removal fix) is shown headless and rendered to a bitmap with zero diagram tabs open, then again after one trivial diagram tab is added via `AddDockable`, sampling pixel colors at the diagram `DocumentControl`'s `PART_Border` location in both frames. Confirms the border pixel is indistinguishable from its surrounding blank content when zero tabs are open (no visible border), while remaining visibly distinct from its content when a tab is open (the with-tab-open appearance is unchanged).
