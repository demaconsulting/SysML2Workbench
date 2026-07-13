## ViewCatalogSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewCatalogSubsystemTests.cs` exercise ViewCatalogSubsystem's unit
(ViewCatalogPresenter) against a real loaded workspace. The suite loads a real workspace model, refreshes the catalog,
and verifies selection publication without a UI host. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/view-catalog-subsystem.yaml` and describes the implemented tests in present tense.

### Test Environment

Tests run under the standard .NET test runner with temporary workspace files that declare real SysML views. No UI host
or external services are required.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/ViewCatalogSubsystemTests.cs` that correspond to
  the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/view-catalog-subsystem.yaml` using the real paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

### Test Scenarios

**LoadedModel_ListsSupportedViews**: The loaded model's declared views are all listed as supported predefined views.
Verified by `ViewCatalogSubsystemTests.LoadedModel_ListsSupportedViews`.

**SelectView_PublishesActiveSelection**: Selecting a view publishes it as the active selection. Verified by
`ViewCatalogSubsystemTests.SelectView_PublishesActiveSelection`.
