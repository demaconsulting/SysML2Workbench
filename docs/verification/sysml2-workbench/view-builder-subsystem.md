## ViewBuilderSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystemTests.cs` exercise ViewBuilderSubsystem's units
(ViewDefinitionModel, SysmlSnippetGenerator) together. The suite combines real definition editing, workspace validation,
and snippet export behavior across both subsystem units. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/view-builder-subsystem.yaml` and describes the implemented tests in present tense.

### Test Environment

Tests run under the standard .NET test runner with temporary workspace files for validation and in-memory custom-view
definitions for export. No external services are required.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystemTests.cs` that correspond to
  the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/view-builder-subsystem.yaml` using the real paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

### Test Scenarios

**EditDefinition_TracksCustomViewInputs**: Editing a custom view definition tracks the user's kind, target, and filter
inputs, and that the definition validates cleanly against a real loaded workspace. Verified by
`ViewBuilderSubsystemTests.EditDefinition_TracksCustomViewInputs`.

**ExportDefinition_GeneratesSysmlSnippet**: A fully edited custom-view definition can be exported as a valid SysML
snippet. Verified by `ViewBuilderSubsystemTests.ExportDefinition_GeneratesSysmlSnippet`.
