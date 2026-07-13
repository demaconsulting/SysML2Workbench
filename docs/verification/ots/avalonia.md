## Avalonia

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/AvaloniaTests.cs` verify the real desktop shell hosting behavior using Avalonia's headless test platform (`Avalonia.Headless.XUnit`'s `[AvaloniaFact]`), which runs the actual `App`/`MainWindowView` composition under a headless windowing platform rather than a mocked UI harness, because the framework itself is the integration boundary being qualified.

### Test Scenarios

**Startup_HostsDesktopShellControls**: A real `MainWindowView` is constructed over a fully composed `MainWindowShell` and shown under the headless platform. The dependency proves it hosts the menu, predefined-views list, and custom-view builder controls (ComboBox, multi-select ListBox, buttons) as real, discoverable Avalonia controls attached to the window's visual tree.

**MainWindow_HostsDiagramAndDiagnosticsPanels**: The same window's diagram `Image` control and diagnostics `ListBox` are confirmed present before any workspace is opened; after opening a workspace and selecting a predefined view through the shell, the shared canvas state backing the diagram control reports loaded content, proving Avalonia genuinely hosts the interactive diagram surface alongside the diagnostics panel.
