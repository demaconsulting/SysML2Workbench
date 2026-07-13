### ViewCatalogPresenter

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewCatalogSubsystem/ViewCatalogPresenterTests.cs` exercise
`ViewCatalogPresenter` directly. The suite uses real loaded workspace declarations and asserts catalog contents,
selection updates, stale-selection clearing, and invalid-selection handling. The scenario list below follows the
authoritative mappings in `docs/reqstream/sysml2-workbench/view-catalog-subsystem/view-catalog-presenter.yaml` and
describes the implemented tests in present tense.

#### Test Environment

Tests run under the standard .NET test runner with real workspace declarations loaded from temporary SysML files. No UI
host or external services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewCatalogSubsystem/ViewCatalogPresenterTests.cs`
  that correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/view-catalog-subsystem/view-catalog-presenter.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**LoadedModel_ListsSupportedViewDefinitions**: The catalog lists every predefined view declared in the loaded model.
Verified by `ViewCatalogPresenterTests.LoadedModel_ListsSupportedViewDefinitions`.

**LoadedModel_ShowsViewKindAndDisplayName**: Each catalog entry carries the view's kind and display name. Verified by
`ViewCatalogPresenterTests.LoadedModel_ShowsViewKindAndDisplayName`.

**SelectView_PublishesCurrentSelection**: Selecting a predefined view publishes it as the current selection. Verified by
`ViewCatalogPresenterTests.SelectView_PublishesCurrentSelection`.
