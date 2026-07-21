## ElementPickerSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/` exercise the
subsystem's units in isolation: `ElementPickerViewModelTests` covers `ElementPickerViewModel`
against caller-supplied candidate lists with no Avalonia, `WorkspaceModel`, or shell dependency,
and `ElementTypeLabelerTests` covers `ElementTypeLabeler`'s full node-kind taxonomy against a
direct set of `SysmlNode` fixtures. The scenario list is enumerated per-unit in the child
`element-picker.md` verification document to keep the mapping to
`docs/reqstream/sysml2-workbench/element-picker-subsystem/element-picker.yaml` explicit.

### Test Environment

Tests run under the standard .NET test runner. No temporary workspace, no logging directory, and
no external services are required; every fixture is built inline as an `IReadOnlyList` or
directly-constructed `SysmlNode`.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/` that
  correspond to the scenarios enumerated in the child unit's verification document pass with zero
  failures.
- The assertions exercised by those scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/element-picker-subsystem.yaml` and its child
  `element-picker.yaml` using the real paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit
  assertion rather than a speculative or placeholder verification statement.
