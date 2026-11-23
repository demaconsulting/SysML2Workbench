package SysML2Workbench_ViewCatalogSubsystem {
    public import SysML2Workbench_ViewCatalogPresenter::*;
    private import SoftwareItemMetadata::*;

    part def ViewCatalogSubsystem {
        doc /* Renders the six predefined view kinds (General, Interconnection, State Transition, Action Flow, Sequence, Grid). */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/ViewCatalogSubsystemTests.cs");
            designFiles = ("docs/design/sysml2-workbench/view-catalog-subsystem.md");
            verificationFiles = ("docs/verification/sysml2-workbench/view-catalog-subsystem.md");
            requirementFiles = ("docs/reqstream/sysml2-workbench/view-catalog-subsystem.yaml");
        }

        part viewCatalogPresenter: ViewCatalogPresenter;
    }
}
