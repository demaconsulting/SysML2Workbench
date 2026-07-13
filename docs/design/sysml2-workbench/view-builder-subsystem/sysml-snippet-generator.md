### SysmlSnippetGenerator

![ViewBuilderSubsystem Structure](ViewBuilderSubsystemView.svg)

#### Purpose

SysmlSnippetGenerator turns the current custom-view definition into readable,
copy-pasteable SysML source so users can persist a GUI-authored view in a
normal model file without introducing a second storage format.

#### Data Model

**Indentation**: `string` — whitespace prefix used when emitting nested SysML
clauses.

**LineEnding**: `string` — line separator applied consistently to generated
text.

**ReservedWordEscapes**: `IReadOnlyDictionary<string, string>` — identifier
rewrite rules used when a generated name would otherwise collide with syntax.

**SnippetOptions**: `SnippetGenerationOptions` — formatting choices such as
whether to emit an explicit view name.

#### Key Methods

**GenerateSnippet**: Produces SysML text for a custom view definition.

- *Parameters*: `ViewDefinitionModel definition` — normalized custom-view
  state.
- *Returns*: `string` — complete SysML snippet ready for copy or save.
- *Preconditions*: `definition` has passed validation and contains at least one
  expose target.
- *Postconditions*: The returned text encodes the selected view kind, target
  set, and optional filter in standard SysML syntax.

**FormatExposeClause**: Emits one `expose` statement.

- *Parameters*: `QualifiedName target` — selected package or element.
- *Returns*: `string` — formatted clause for the target.
- *Preconditions*: `target` is resolvable and normalized.
- *Postconditions*: The clause preserves the chosen target identity without
  inventing a proprietary alias format.

**SanitizeIdentifier**: Makes an optional exported view name safe for SysML
text.

- *Parameters*: `string rawName` — candidate name from the UI.
- *Returns*: `string` — safe identifier or quoted representation.
- *Preconditions*: `rawName` is not empty when supplied.
- *Postconditions*: The result is suitable for embedding in generated SysML.

#### Error Handling

SysmlSnippetGenerator treats missing targets, unsupported view kinds, or
invalid identifiers as caller errors and returns them as validation failures
before emission. Formatting defects detected during generation are propagated
because emitting malformed SysML would violate the unit's purpose. Whitespace
or naming normalization is handled locally when it can be corrected without
changing semantic intent.

#### Dependencies

- **ViewDefinitionModel** — supplies the normalized custom-view state to export.
- **SysML2Tools** — defines the SysML concepts and naming rules the generator
  mirrors.
- **Avalonia** — may provide clipboard integration at the caller layer, though
  the unit itself only produces text.
- **MainWindowShell** — hosts the user interaction that requests snippet
  generation.

#### Callers

- **MainWindowShell**
