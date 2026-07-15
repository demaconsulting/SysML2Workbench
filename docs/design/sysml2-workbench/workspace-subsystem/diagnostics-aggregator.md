### DiagnosticsAggregator

![WorkspaceSubsystem Structure](WorkspaceSubsystemView.svg)

#### Purpose

DiagnosticsAggregator collects file-level parser and semantic diagnostics from
the workspace and publishes a stable workspace-wide view that UI consumers can
display without understanding the underlying load pipeline.

#### Data Model

**DiagnosticsByFile**: `IReadOnlyDictionary<string, IReadOnlyList<SysmlDiagnostic>>`
— grouped diagnostics keyed by normalized file path.

**OrderedDiagnostics**: `IReadOnlyList<SysmlDiagnostic>` — flattened diagnostic
list sorted for deterministic presentation.

**SeverityCounts**: `IReadOnlyDictionary<string, int>` — summary counts used by
the UI to show workspace health at a glance.

**LastUpdatedUtc**: `DateTimeOffset` — timestamp of the last successful
aggregation pass.

#### Key Methods

**ReplaceFileDiagnostics**: Updates the diagnostics for one file.

- *Parameters*: `string path` — normalized file path;
  `IReadOnlyList<SysmlDiagnostic> diagnostics` — latest diagnostics for that
  file.
- *Returns*: `void` — aggregation state is updated in place.
- *Preconditions*: The workspace file set is known.
- *Postconditions*: The file's prior diagnostic state is replaced and the
  aggregate view is marked dirty.

**RebuildAggregate**: Recomputes the flattened workspace diagnostic view.

- *Parameters*: `None` — operates on the current grouped state.
- *Returns*: `IReadOnlyList<SysmlDiagnostic>` — ordered diagnostics ready for
  presentation.
- *Preconditions*: Grouped diagnostics have been initialized.
- *Postconditions*: `OrderedDiagnostics`, `SeverityCounts`, and `LastUpdatedUtc`
  are refreshed together.

**GetVisibleDiagnostics**: Returns the current aggregate for consumers.

- *Parameters*: `None` — uses the current in-memory state.
- *Returns*: `IReadOnlyList<SysmlDiagnostic>` — read-only diagnostics snapshot.
- *Preconditions*: None.
- *Postconditions*: Consumers receive a stable list that will not mutate during
  iteration.

#### Error Handling

DiagnosticsAggregator treats malformed or partial diagnostic inputs as local
data-shape issues and normalizes them into the aggregate list where possible.
If the upstream workspace model cannot provide any diagnostic data at all, the
absence is surfaced as an empty aggregate plus a propagated reload failure at
the caller boundary. Aggregation never suppresses the original workspace
exception that caused diagnostics to be stale.

#### Dependencies

- **WorkspaceModel** — supplies per-file parser and semantic results.
- **DiagnosticsPanelSubsystem** — consumes the flattened output for display.
- **RollingFileLogger** — records repeated aggregation failures or inconsistent
  input.
- **SysML2Tools** — defines the `SysmlDiagnostic` contract used throughout the
  aggregation pipeline.

#### Callers

- **WorkspaceSubsystem**
- **DiagnosticsListView**
- **MainWindowShell**
