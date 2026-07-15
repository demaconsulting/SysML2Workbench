### ViewCatalogPresenter

![ViewCatalogSubsystem Structure](ViewCatalogSubsystemView.svg)

#### Purpose

ViewCatalogPresenter converts the workspace's predefined SysML view usages into
a deterministic, UI-ready catalog and tracks which predefined view is currently
selected for rendering.

#### Data Model

**AvailableViews**: `IReadOnlyList<ViewDescriptor>` — flattened catalog of
predefined views visible in the current workspace.

**SelectedViewId**: `string?` — identifier of the currently selected predefined
view, or `null` when nothing is selected.

**KindsInDisplayOrder**: `IReadOnlyList<ViewKind>` — stable ordering for the
six supported predefined view kinds.

**WorkspaceRevision**: `string` — opaque token used to detect when the
underlying workspace snapshot has changed and the catalog must be rebuilt.

#### Key Methods

**RefreshCatalog**: Rebuilds the catalog from the latest workspace snapshot.

- *Parameters*: `SemanticWorkspace workspace` — loaded model content.
- *Returns*: `IReadOnlyList<ViewDescriptor>` — updated view descriptors.
- *Preconditions*: `workspace` represents a coherent snapshot from
  WorkspaceModel.
- *Postconditions*: `AvailableViews` reflects the discovered predefined views
  and stale selections are cleared when no longer valid.

**SelectView**: Marks one predefined view as active.

- *Parameters*: `string viewId` — identifier of the chosen descriptor.
- *Returns*: `ViewDescriptor` — descriptor to forward to rendering.
- *Preconditions*: `viewId` exists in `AvailableViews`.
- *Postconditions*: `SelectedViewId` matches the chosen descriptor.

**GetSelectedView**: Returns the active predefined view descriptor.

- *Parameters*: `None` — uses current presenter state.
- *Returns*: `ViewDescriptor?` — active descriptor or `null` when nothing is
  selected.
- *Preconditions*: None.
- *Postconditions*: The returned descriptor matches the current catalog
  revision.

#### Error Handling

ViewCatalogPresenter handles invalid or disappearing selections locally by
clearing the selected identifier and forcing the UI to choose again. Semantic
workspace discovery failures are propagated from WorkspaceModel because the
presenter cannot create a correct catalog without a coherent snapshot. Duplicate
view identifiers are treated as a local normalization error and must be
resolved before exposing the catalog.

#### Dependencies

- **WorkspaceModel** — provides the semantic workspace snapshot used for
  discovery.
- **LayoutInvoker** — consumes the selected descriptor to generate a layout.
- **SysML2Tools** — supplies the view usage concepts and semantic model types.
- **AppShellSubsystem** — hosts the UI surface that binds the catalog output.

#### Callers

- **MainWindowShell**
- **LayoutInvoker**
