### ViewDefinitionModel

![ViewBuilderSubsystem Structure](ViewBuilderSubsystemView.svg)

#### Purpose

ViewDefinitionModel captures the complete session-only definition of a custom
view so the same normalized state can be used both for live preview rendering
and for SysML snippet generation.

#### Data Model

**ViewKind**: `ViewKind` — selected custom-view rendering style, constrained to
the supported SysML view kinds exposed by the UI.

**ExposeTargets**: `IReadOnlyList<ExposeTargetSelection>` — ordered set of
packages or elements selected for `expose` clauses, each carrying its own
recursion kind and optional bracket-filter expression.

**ExposeTargetSelection**: `record(QualifiedName: QualifiedName, RecursionKind:
ExposeRecursionKind, BracketFilterExpression: string?)` — one selected expose
target. `RecursionKind` selects which SysML v2 `expose` textual form is
emitted (default `MembershipRecursive`); `BracketFilterExpression` is only
meaningful on the two recursive kinds.

**FilterExpression**: `string?` — optional filter text applied to narrow the
rendered content within the selected targets.

**DisplayName**: `string?` — optional user-facing view name used when exporting
a named snippet.

#### Key Methods

**SetViewKind**: Changes the target rendering style for the custom view.

- *Parameters*: `ViewKind viewKind` — selected view kind.
- *Returns*: `void` — state is updated in place.
- *Preconditions*: `viewKind` is one of the supported custom-view kinds.
- *Postconditions*: The model reflects the new kind and invalid kind-specific
  cached state is cleared.

**AddExposeTarget**: Adds a qualified name to the `expose` target set.

- *Parameters*: `QualifiedName qualifiedName` — package or element to expose.
- *Returns*: `void` — state is updated in place.
- *Preconditions*: `qualifiedName` is not null or whitespace.
- *Postconditions*: The target is added with `RecursionKind` defaulted to
  `MembershipRecursive`, unless a selection already exists for the exact
  (`qualifiedName`, `MembershipRecursive`) pair, in which case the existing
  selection (including its bracket filter) is preserved rather than reset. If
  `qualifiedName` is already selected under a *different* recursion kind, a
  second, independent selection is added for the same qualified name — this
  is valid SysML v2 (for example both `expose PublishingSubsystem;` and
  `expose PublishingSubsystem::*;` for the same package). Since there are
  only four possible recursion kinds, at most four selections can ever exist
  for one qualified name; once all four are taken, this remains a safe no-op.

**RemoveExposeTarget**: Removes a qualified name/recursion kind pair from the
`expose` target set.

- *Parameters*: `QualifiedName qualifiedName` — previously-added target;
  `ExposeRecursionKind recursionKind` — recursion kind of the selection to
  remove.
- *Returns*: `void` — state is updated in place.
- *Postconditions*: The selection matching both `qualifiedName` and
  `recursionKind` is removed, if any; a no-op otherwise. Any other selection
  sharing the same `qualifiedName` under a different recursion kind is left
  untouched.

**SetExposeRecursionKind**: Changes a previously-added target's recursion kind.

- *Parameters*: `QualifiedName qualifiedName`, `ExposeRecursionKind
  currentRecursionKind` — recursion kind of the selection to change,
  `ExposeRecursionKind newRecursionKind` — new recursion kind.
- *Returns*: `void` — state is updated in place.
- *Postconditions*: The selection matching (`qualifiedName`,
  `currentRecursionKind`) has its `RecursionKind` updated to
  `newRecursionKind`; a no-op if no selection matches that pair. Also a no-op
  if a *different* selection already exists for (`qualifiedName`,
  `newRecursionKind`): applying the change would otherwise collide with that
  sibling selection, so it is rejected rather than creating a duplicate pair
  or silently clobbering the sibling.

**SetExposeBracketFilter**: Sets or clears a previously-added target's optional
bracket-filter expression.

- *Parameters*: `QualifiedName qualifiedName`, `ExposeRecursionKind
  recursionKind` — recursion kind of the selection to update, `string?
  expression`.
- *Returns*: `void` — state is updated in place.
- *Postconditions*: The selection matching (`qualifiedName`, `recursionKind`)
  has its `BracketFilterExpression` updated (or cleared, if null/whitespace);
  a no-op if no selection matches that pair. Setting a bracket filter on a
  `MembershipExact`/`NamespaceDirectChildren` target is accepted here but
  reported as an error diagnostic by `ValidateAgainstWorkspace`.

**ValidateAgainstWorkspace**: Confirms the definition is renderable.

- *Parameters*: `SemanticWorkspace workspace` — current loaded workspace.
- *Returns*: `IReadOnlyList<SysmlDiagnostic>` — validation findings for the
  builder UI.
- *Preconditions*: A workspace has been loaded.
- *Postconditions*: Missing targets, invalid filters, or unsupported
  combinations are reported without mutating the user's prior selections.

#### Error Handling

ViewDefinitionModel handles incomplete authoring states locally by treating
missing view kinds, empty target sets, or invalid filter expressions as
validation failures rather than exceptions. Resolution failures caused by a
stale workspace snapshot are surfaced back to the caller so the UI can prompt
for a refresh. The model never silently changes a user's chosen targets except
to remove exact (qualified name, recursion kind) duplicates during
normalization. A bracket-filter expression
set on a `MembershipExact` or `NamespaceDirectChildren` target is reported as
a validation error, since bracket filters are only valid SysML v2 syntax on
the two recursive expose forms.

#### Dependencies

- **WorkspaceModel** — validates that target elements still resolve in the
  current workspace.
- **SysML2Tools** — provides the semantic types and view semantics used by
  validation.
- **SysmlSnippetGenerator** — consumes the normalized definition when exporting
  text.
- **LayoutInvoker** — consumes the same definition for live preview rendering.

#### Callers

- **MainWindowShell**
- **SysmlSnippetGenerator**
- **LayoutInvoker**
