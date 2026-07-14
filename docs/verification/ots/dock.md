## Dock

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/DockTests.cs` verify Dock's layout composition and Avalonia hosting behavior directly: a plain xUnit `[Fact]` walks the real `WorkbenchDockFactory`-produced dock tree to confirm the four Phase-0 panel view models are genuinely reachable, and two `[AvaloniaFact]` tests host a real `Dock.Avalonia.Controls.DockControl` inside a headless `Window` to confirm Avalonia can render the composed layout without throwing, and that a closed Tool panel can be restored to its original dock.

### Test Scenarios

**CreateLayout_ComposesFourPanelDockables**: `WorkbenchDockFactory.CreateLayout()` is called with real panel view model instances and its resulting `IRootDock` is walked recursively, confirming all four panel view models (predefined views, custom view builder, diagnostics, diagram) are present as dockables in the composed tree.

**DockControl_HostsWorkbenchLayout**: A real `DockControl` is assigned the factory-produced layout and shown inside a headless `Window`, proving Dock's Avalonia control genuinely accepts and hosts the composed layout.

**RestoreDockable_ReopensClosedToolInOriginalDock**: A real `WorkbenchDockFactory` layout is hosted in a headless `DockControl`/`Window`, `CloseDockable` is called on the predefined-views panel, confirming it leaves its original `ToolDock`'s `VisibleDockables` and is tracked in `IRootDock.HiddenDockables` (proving `HideToolsOnClose` hides rather than destroys it), and then `RestoreDockable` is called, confirming the exact same view model instance is back in its original `ToolDock`'s `VisibleDockables`.
