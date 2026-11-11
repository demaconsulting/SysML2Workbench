package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def ViewDefinitionModel {
            doc /* Captures custom view type, expose targets, and filter expression. */

            metadata info : SoftwareInfo {
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/ViewBuilderSubsystem/ViewDefinitionModel.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystem/ViewDefinitionModelTests.cs");
                designFiles = ("docs/design/sysml2-workbench/view-builder-subsystem/view-definition-model.md");
                verificationFiles = ("docs/verification/sysml2-workbench/view-builder-subsystem/view-definition-model.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/view-builder-subsystem/view-definition-model.yaml");
            }
        }
    }
}
