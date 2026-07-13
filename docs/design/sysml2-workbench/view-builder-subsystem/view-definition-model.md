### ViewDefinitionModel

![ViewBuilderSubsystem Structure](ViewBuilderSubsystemView.svg)

#### Purpose

ViewDefinitionModel captures the complete session-only definition of a custom
view so the same normalized state can be used both for live preview rendering
and for SysML snippet generation.

#### Data Model

**ViewKind**: `ViewKind` — selected custom-view rendering style, constrained to
the supported SysML view kinds exposed by the UI.

**ExposeTargets**: `IReadOnlyList<QualifiedName>` — ordered set of packages or
elements selected for `expose` clauses.

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

**SetExposeTargets**: Replaces the current `expose` target set.

- *Parameters*: `IReadOnlyList<QualifiedName> targets` — selected packages or
  elements.
- *Returns*: `void` — state is updated in place.
- *Preconditions*: Every target resolves in the current workspace snapshot.
- *Postconditions*: `ExposeTargets` preserves the requested order with
  duplicates removed.

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
to remove exact duplicates during normalization.

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
