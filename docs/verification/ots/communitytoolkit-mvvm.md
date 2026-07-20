## CommunityToolkit.Mvvm

### Verification Approach

CommunityToolkit.Mvvm's `[ObservableProperty]` source generator has no dedicated OTS test file of its own; instead, its behavior is verified indirectly through the Dock and Avalonia OTS integration tests that exercise the generated properties and change-notification partial methods on the real panel view models, because the generator's output is only meaningfully observable through the view models it augments.

### Test Scenarios

**DockTests.CreateLayout_ComposesThreePanelDockables**: Constructing the real `PredefinedViewsToolViewModel`, `DiagnosticsToolViewModel`, and `WorkspacePanelToolViewModel` instances and composing them into a Dock layout proves the CommunityToolkit.Mvvm-generated observable properties compile and function as genuine Dock dockables.

**AvaloniaTests.MainWindow_HostsDiagramAndDiagnosticsPanels**: Selecting a predefined view through the real shell and UI controls exercises the CommunityToolkit.Mvvm-generated `OnSelectedViewChanged` partial method, confirming the generated change notification genuinely drives the diagram refresh.
