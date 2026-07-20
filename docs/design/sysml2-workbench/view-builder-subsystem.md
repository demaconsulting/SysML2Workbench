## ViewBuilderSubsystem

![ViewBuilderSubsystem Structure](ViewBuilderSubsystemView.svg)

### Overview

ViewBuilderSubsystem captures session-only custom-view intent and turns it into
standard SysML text that users can promote into permanent model files. Its
boundary is limited to constructing the definition of a custom view; it does
not persist a proprietary format and it does not perform layout directly. The
subsystem contains ViewDefinitionModel and SysmlSnippetGenerator.

### Interfaces

**Custom View Definition API**: In-process interface for setting the desired
view kind, expose targets, and filter expression.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Accepts user selections, validates them against the current
  workspace, and exposes a normalized custom-view definition to rendering and
  export consumers.
- *Constraints*: The definition is session-only and must stay aligned with
  SysML view semantics, including multi-target expose support.

**Snippet Export API**: In-process operation for producing copy-pasteable SysML
text.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Converts the current custom-view definition into valid
  `view ... expose ...` syntax for the user to copy into a model file.
- *Constraints*: Output must remain human-editable and must not introduce a
  separate tool-specific persistence format.

**Workspace Element Lookup**: Semantic model access used to validate expose
targets.

- *Type*: In-process .NET API.
- *Role*: Consumer.
- *Contract*: Consumes candidate elements and packages from the loaded
  workspace so the builder can restrict target choices to resolvable items.
- *Constraints*: Invalid or stale targets must be reported as validation errors
  rather than silently removed.

### Design

1. ViewDefinitionModel stores the currently selected view kind, target set, and
   filter expression for the active custom view.
2. AppShellSubsystem binds user edits into that model and requests validation
   against the loaded workspace, now through the modal `ViewBuilderDialog`
   rather than a docked panel.
3. LayoutRenderingSubsystem consumes the normalized definition when the user
   previews a custom view.
4. SysmlSnippetGenerator converts the same definition into copy-pasteable SysML
   text so the user can persist the design in a normal model file.
5. Because the subsystem owns only authoring intent, it stays independent of
   diagram rendering controls and file persistence concerns.
