## DiagnosticsPanelSubsystem

![DiagnosticsPanelSubsystem Structure](DiagnosticsPanelSubsystemView.svg)

### Overview

DiagnosticsPanelSubsystem presents workspace diagnostics in a dedicated UI
surface. Its boundary starts with the aggregated diagnostic list produced by
WorkspaceSubsystem and ends with a filtered, selectable list shown to the user.
It contains one unit, DiagnosticsListView. Navigation to source text is
intentionally out of scope in this phase, but the subsystem preserves enough
structure to support that future extension.

### Interfaces

**Diagnostics Display API**: In-process interface for binding and filtering
workspace diagnostics.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Accepts an ordered list of diagnostics, applies severity or text
  filters, and exposes the currently selected diagnostic to the shell.
- *Constraints*: The panel must remain usable even when the workspace contains
  many errors and warnings.

**Workspace Diagnostic Feed**: The aggregated diagnostic source from the
workspace.

- *Type*: In-process .NET API.
- *Role*: Consumer.
- *Contract*: Consumes a stable list of `SysmlDiagnostic` entries and related
  file context produced by DiagnosticsAggregator.
- *Constraints*: The panel must not mutate or reorder the source feed in a way
  that loses determinism.

### Design

1. DiagnosticsListView receives the flattened workspace diagnostic list from
   DiagnosticsAggregator.
2. The view applies local filtering and selection state needed for
   presentation.
3. AppShellSubsystem binds the filtered list into the main window and may
   reflect counts or selection details elsewhere in the UI.
4. Because click-to-navigate is deferred, the current design exposes selection
   events without requiring source-editor integration.
