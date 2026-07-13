## ViewCatalogSubsystem

![ViewCatalogSubsystem Structure](ViewCatalogSubsystemView.svg)

### Overview

ViewCatalogSubsystem exposes the predefined SysML view usages already present in
the loaded workspace. Its responsibility is limited to discovering, listing,
filtering, and selecting those views; it does not own custom-view authoring,
layout generation, or SVG presentation. The subsystem contains one unit,
ViewCatalogPresenter, which adapts the semantic workspace into a UI-friendly
catalog.

### Interfaces

**View Catalog API**: In-process interface used to enumerate and select
predefined views.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Returns descriptors for General, Interconnection, State
  Transition, Action Flow, Sequence, and Grid views found in the current
  workspace and tracks the current selection.
- *Constraints*: The catalog must be regenerated when the workspace changes and
  must remain deterministic even when the model defines multiple views of the
  same kind.

**Workspace Semantic Model**: The loaded workspace content used to discover view
definitions.

- *Type*: In-process .NET API.
- *Role*: Consumer.
- *Contract*: Consumes the semantic workspace snapshot and derives catalog
  entries from the view usages defined in the model.
- *Constraints*: Discovery must treat diagnostics as non-fatal so the catalog
  can still show any valid views that remain available.

### Design

1. ViewCatalogPresenter queries WorkspaceModel for the current semantic
   workspace snapshot.
2. The presenter extracts predefined view usages, normalizes them into view
   descriptors, and groups them by supported view kind for the UI.
3. AppShellSubsystem binds the resulting list into the view-selection surface.
4. When the user selects a catalog entry, the presenter returns the stable
   descriptor consumed by LayoutRenderingSubsystem.
5. When WorkspaceSubsystem publishes a refresh, the presenter rebuilds the
   catalog and preserves selection only when the prior descriptor is still
   valid.
