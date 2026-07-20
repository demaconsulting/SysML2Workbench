### ViewDefinitionModel

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystem/ViewDefinitionModelTests.cs` exercise
`ViewDefinitionModel` directly. The suite validates definitions against a real loaded workspace and checks both
successful and diagnostic-producing inputs. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/view-builder-subsystem/view-definition-model.yaml` and describes the implemented tests
in present tense.

#### Test Environment

Tests run under the standard .NET test runner with temporary workspace files and in-memory definition edits. No external
services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystem/ViewDefinitionModelTests.cs`
  that correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/view-builder-subsystem/view-definition-model.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**ChangeViewKind_StoresCurrentSelection**: Changing the view kind stores the current selection. Verified by
`ViewDefinitionModelTests.ChangeViewKind_StoresCurrentSelection`.

**AddExposeTarget_StoresMultipleExposeTargetsWithoutDuplicates**: Selecting expose targets stores multiple targets in
the requested order, without duplicates, each defaulting to `MembershipRecursive`. Verified by
`ViewDefinitionModelTests.AddExposeTarget_StoresMultipleExposeTargetsWithoutDuplicates`.

**RemoveExposeTarget_RemovesOnlyMatchingSelection**: Removing an expose target drops only that selection. Verified by
`ViewDefinitionModelTests.RemoveExposeTarget_RemovesOnlyMatchingSelection`.

**RemoveExposeTarget_UnknownQualifiedName_IsNoOp**: Removing a qualified name that was never added is a no-op.
Verified by `ViewDefinitionModelTests.RemoveExposeTarget_UnknownQualifiedName_IsNoOp`.

**RemoveExposeTarget_KnownQualifiedNameWrongRecursionKind_IsNoOp**: Removing a known qualified name under a
recursion kind it does not currently use is a no-op. Verified by
`ViewDefinitionModelTests.RemoveExposeTarget_KnownQualifiedNameWrongRecursionKind_IsNoOp`.

**AddExposeTarget_ExactQualifiedNameAndKindTwice_IsNoOp**: Adding the exact same qualified name and recursion kind
pair twice remains a no-op, preserving any prior configuration. Verified by
`ViewDefinitionModelTests.AddExposeTarget_ExactQualifiedNameAndKindTwice_IsNoOp`.

**AddExposeTarget_SameQualifiedNameDifferentRecursionKind_AddsBothSelections**: The same qualified name can be
added twice with two different recursion kinds, and both selections appear in `ExposeTargets`, matching the valid
SysML v2 pattern of exposing the same package both exactly and recursively. Verified by
`ViewDefinitionModelTests.AddExposeTarget_SameQualifiedNameDifferentRecursionKind_AddsBothSelections`.

**RemoveExposeTarget_OneOfTwoSameQualifiedNameSelections_LeavesOtherUntouched**: Removing one of two
same-qualified-name selections leaves the sibling selection untouched. Verified by
`ViewDefinitionModelTests.RemoveExposeTarget_OneOfTwoSameQualifiedNameSelections_LeavesOtherUntouched`.

**SetExposeRecursionKind_ChangesSelectedTargetKind**: Changing a selected target's recursion kind updates only that
target. Verified by `ViewDefinitionModelTests.SetExposeRecursionKind_ChangesSelectedTargetKind`.

**SetExposeRecursionKind_OneOfTwoSameQualifiedNameSelections_DoesNotAffectOther**: Changing the recursion kind of
one of two same-qualified-name selections does not affect the sibling selection. Verified by
`ViewDefinitionModelTests.SetExposeRecursionKind_OneOfTwoSameQualifiedNameSelections_DoesNotAffectOther`.

**SetExposeRecursionKind_TargetKindAlreadyTakenBySibling_IsNoOp**: Changing a selection's recursion kind to one
already used by a sibling selection for the same qualified name is a no-op, rather than throwing or creating a
duplicate pair. Verified by
`ViewDefinitionModelTests.SetExposeRecursionKind_TargetKindAlreadyTakenBySibling_IsNoOp`.

**SetExposeBracketFilter_SetsAndClearsExpression**: Setting and clearing a target's bracket-filter expression works,
and setting a filter on an unknown qualified name, or a known qualified name under an unused recursion kind, is a
no-op. Verified by `ViewDefinitionModelTests.SetExposeBracketFilter_SetsAndClearsExpression`.

**ValidateAgainstWorkspace_ValidBracketFilterOnRecursiveTarget_ReturnsNoDiagnostics**: A valid bracket-filter
expression on a recursive target produces no diagnostics. Verified by
`ViewDefinitionModelTests.ValidateAgainstWorkspace_ValidBracketFilterOnRecursiveTarget_ReturnsNoDiagnostics`.

**ValidateAgainstWorkspace_InvalidBracketFilterExpression_ReturnsDiagnostic**: An unparsable bracket-filter expression
is reported as a diagnostic. Verified by
`ViewDefinitionModelTests.ValidateAgainstWorkspace_InvalidBracketFilterExpression_ReturnsDiagnostic`.

**ValidateAgainstWorkspace_BracketFilterOnNonRecursiveKind_ReturnsDiagnostic**: A bracket-filter expression on a
`MembershipExact` or `NamespaceDirectChildren` target is reported as a diagnostic. Verified by
`ViewDefinitionModelTests.ValidateAgainstWorkspace_BracketFilterOnNonRecursiveKind_ReturnsDiagnostic`.

**DefinitionState_ReportsRenderAndExportReadiness**: The definition reports whether it has enough information to render
a preview or export a snippet. Verified by `ViewDefinitionModelTests.DefinitionState_ReportsRenderAndExportReadiness`.

**AddExposeTarget_SelectionOverload_AppendsAndDedupesByQualifiedNameAndRecursionKind**: The `ExposeTargetSelection`
overload appends a fully-specified selection (qualified name, recursion kind, and bracket-filter expression) in a
single call, is a no-op when the exact (qualified name, recursion kind) pair is already present (preserving the
originally-added selection's bracket filter), and allows the same qualified name to be added again under a
different recursion kind. Verified by
`ViewDefinitionModelTests.AddExposeTarget_SelectionOverload_AppendsAndDedupesByQualifiedNameAndRecursionKind`.

**AddExposeTarget_SelectionOverload_NullSelection_ThrowsArgumentNullException**: Passing a `null` selection to the
`ExposeTargetSelection` overload throws `ArgumentNullException` rather than silently ignoring it or throwing a
`NullReferenceException`. Verified by
`ViewDefinitionModelTests.AddExposeTarget_SelectionOverload_NullSelection_ThrowsArgumentNullException`.
