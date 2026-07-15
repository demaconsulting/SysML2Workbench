## CommunityToolkit.Mvvm

SysML2Workbench uses CommunityToolkit.Mvvm's observable-property source
generator for the Dock panel view models introduced alongside AvaloniaUI/Dock.

### Purpose

CommunityToolkit.Mvvm was chosen because Dock's MVVM view models
(`Tool`/`Document` subclasses) already implement `INotifyPropertyChanged`,
and CommunityToolkit's `[ObservableProperty]` source generator lets the four
panel view models expose bindable state (available views, diagnostics,
expose-target lists, status messages) without hand-writing property backing
fields and change notification, keeping each panel view model thin.

### Features Used

- **`[ObservableProperty]` source generator** — generates bindable
  properties with change notification on `PredefinedViewsToolViewModel`,
  `CustomViewBuilderToolViewModel`, and `DiagnosticsToolViewModel`.
- **Generated `On<Property>Changed` partial methods** — used to react to a
  predefined-view selection change and forward it to the shell.

### Integration Pattern

Each panel view model is a partial class deriving from a Dock `Tool` base
class, with `[ObservableProperty]`-attributed fields for the state its view
binds to in XAML. No other CommunityToolkit.Mvvm feature (commands,
messaging, `ObservableObject` base class) is used, since Dock's own base
classes already supply property-change notification and the panels have no
need for a messaging bus.
