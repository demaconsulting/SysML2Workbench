### DiagnosticsListView

![DiagnosticsPanelSubsystem Structure](DiagnosticsPanelSubsystemView.svg)

#### Purpose

DiagnosticsListView adapts aggregated workspace diagnostics into a selectable,
filterable list suitable for the diagnostics panel in the main window.

#### Data Model

**VisibleDiagnostics**: `IReadOnlyList<SysmlDiagnostic>` — diagnostics
currently shown after filtering.

**SelectedDiagnostic**: `SysmlDiagnostic?` — currently highlighted diagnostic
entry.

**SeverityFilter**: `IReadOnlySet<DiagnosticSeverity>` — enabled severities
used to limit the visible list.

**SearchText**: `string?` — optional free-text filter applied to diagnostic
messages or file paths.

#### Key Methods

**BindDiagnostics**: Replaces the list contents with a new aggregate snapshot.

- *Parameters*: `IReadOnlyList<SysmlDiagnostic> diagnostics` — latest workspace
  diagnostics.
- *Returns*: `void` — local presentation state updates in place.
- *Preconditions*: `diagnostics` is ordered deterministically by the caller.
- *Postconditions*: `VisibleDiagnostics` reflects the supplied snapshot after
  filters are applied.

**ApplyFilters**: Recomputes the visible list from current filter state.

- *Parameters*: `IReadOnlySet<DiagnosticSeverity> severities` — enabled
  severities; `string? searchText` — optional text filter.
- *Returns*: `IReadOnlyList<SysmlDiagnostic>` — filtered diagnostics.
- *Preconditions*: A diagnostics snapshot has been bound.
- *Postconditions*: `VisibleDiagnostics` and `SelectedDiagnostic` remain
  consistent with the applied filters.

**SelectDiagnostic**: Marks one diagnostic as active.

- *Parameters*: `SysmlDiagnostic diagnostic` — diagnostic chosen by the user.
- *Returns*: `void` — selection state updates in place.
- *Preconditions*: The supplied diagnostic is present in `VisibleDiagnostics`.
- *Postconditions*: `SelectedDiagnostic` references the chosen entry.

#### Error Handling

DiagnosticsListView handles empty result sets, invalid filter combinations, and
stale selections locally by clearing or reducing the visible list without
throwing. It propagates upstream binding failures because it cannot recover
from the absence of a coherent diagnostic snapshot. UI rendering faults are
logged and surfaced through the shell rather than hidden inside the control.

#### Dependencies

- **DiagnosticsAggregator** — supplies the aggregated diagnostics to display.
- **Avalonia** — provides the list control, binding, and user-input framework.
- **MainWindowShell** — hosts the panel and consumes selection changes.
- **SysML2Tools** — defines the `SysmlDiagnostic` contract displayed by the UI.

#### Callers

- **MainWindowShell**
