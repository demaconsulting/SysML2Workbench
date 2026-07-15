### SysmlSnippetGenerator

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystem/SysmlSnippetGeneratorTests.cs` exercise
`SysmlSnippetGenerator` directly. The suite uses in-memory definitions and string assertions to verify emitted SysML
text, identifier sanitization, and invalid-definition guards. The scenario list below follows the authoritative mappings
in `docs/reqstream/sysml2-workbench/view-builder-subsystem/sysml-snippet-generator.yaml` and describes the implemented
tests in present tense.

#### Test Environment

Tests run under the standard .NET test runner with in-memory view definitions and string assertions only. No external
services are required.

#### Acceptance Criteria

- All implemented tests in
  `test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystem/SysmlSnippetGeneratorTests.cs` that correspond to the
  scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/view-builder-subsystem/sysml-snippet-generator.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**ExportDefinition_EmitsSysmlViewSnippet**: A normalized custom-view definition produces a copy-pasteable SysML view
snippet. Verified by `SysmlSnippetGeneratorTests.ExportDefinition_EmitsSysmlViewSnippet`.

**ExportDefinition_PreservesKindTargetsAndFilter**: The generated snippet preserves the selected view kind, every expose
target, and the optional filter expression. Verified by
`SysmlSnippetGeneratorTests.ExportDefinition_PreservesKindTargetsAndFilter`.

**FormatExposeClause_EachRecursionKind_EmitsCorrectExposeStatement**: Formatting a single expose clause emits the
correct textual form for each of the four recursion kinds, with and without a bracket-filter expression (six textual
forms in total). Verified by `SysmlSnippetGeneratorTests.FormatExposeClause_EachRecursionKind_EmitsCorrectExposeStatement`.
