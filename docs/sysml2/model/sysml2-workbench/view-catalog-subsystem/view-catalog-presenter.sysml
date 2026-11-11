package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def ViewCatalogPresenter {
            doc /* Lists and selects predefined views defined in the model. */

            metadata info : SoftwareInfo {
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/ViewCatalogSubsystem/ViewCatalogPresenter.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/ViewCatalogSubsystem/ViewCatalogPresenterTests.cs");
                designFiles = ("docs/design/sysml2-workbench/view-catalog-subsystem/view-catalog-presenter.md");
                verificationFiles = ("docs/verification/sysml2-workbench/view-catalog-subsystem/view-catalog-presenter.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/view-catalog-subsystem/view-catalog-presenter.yaml");
            }
        }
    }
}
