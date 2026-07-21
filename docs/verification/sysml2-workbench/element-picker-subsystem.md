## ElementPickerSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/` exercise the
subsystem's units in isolation: `ElementFilterViewModelTests` covers `ElementFilterViewModel`
(the selection-free chip/search filter) against caller-supplied candidate lists,
`ElementPickerViewModelTests` covers `ElementPickerViewModel` (which composes an
`ElementFilterViewModel` internally and adds selection) against the same kind of candidate lists,
and `ElementTypeLabelerTests` covers `ElementTypeLabeler`'s full node-kind taxonomy against a
direct set of `SysmlNode` fixtures - none of the three require Avalonia, `WorkspaceModel`, or a
shell dependency. The scenario list is enumerated per-unit in the child `element-filter.md` and
`element-picker.md` verification documents to keep the mapping to
`docs/reqstream/sysml2-workbench/element-picker-subsystem/element-filter.yaml` and
`docs/reqstream/sysml2-workbench/element-picker-subsystem/element-picker.yaml` explicit.

### Test Environment

Tests run under the standard .NET test runner. No temporary workspace, no logging directory, and
no external services are required; every fixture is built inline as an `IReadOnlyList` or
directly-constructed `SysmlNode`.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/` that
  correspond to the scenarios enumerated in the child units' verification documents pass with zero
  failures.
- The assertions exercised by those scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/element-picker-subsystem.yaml` and its child
  `element-filter.yaml`/`element-picker.yaml` using the real paths and collaborators described
  above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit
  assertion rather than a speculative or placeholder verification statement.
