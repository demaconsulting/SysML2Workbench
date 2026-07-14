## Dock

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/DockTests.cs` verify Dock's layout composition and Avalonia hosting behavior directly: a plain xUnit `[Fact]` walks the real `WorkbenchDockFactory`-produced dock tree to confirm the four Phase-0 panel view models are genuinely reachable, and an `[AvaloniaFact]` hosts a real `Dock.Avalonia.Controls.DockControl` inside a headless `Window` to confirm Avalonia can render the composed layout without throwing.

### Test Scenarios

**CreateLayout_ComposesFourPanelDockables**: `WorkbenchDockFactory.CreateLayout()` is called with real panel view model instances and its resulting `IRootDock` is walked recursively, confirming all four panel view models (predefined views, custom view builder, diagnostics, diagram) are present as dockables in the composed tree.

**DockControl_HostsWorkbenchLayout**: A real `DockControl` is assigned the factory-produced layout and shown inside a headless `Window`, proving Dock's Avalonia control genuinely accepts and hosts the composed layout.
