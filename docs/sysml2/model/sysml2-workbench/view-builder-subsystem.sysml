package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def ViewBuilderSubsystem {
            doc /* GUI for constructing custom views (view type, multi-target expose selection, filter expression). */

            metadata info : SoftwareInfo {
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystemTests.cs");
                designFiles = ("docs/design/sysml2-workbench/view-builder-subsystem.md");
                verificationFiles = ("docs/verification/sysml2-workbench/view-builder-subsystem.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/view-builder-subsystem.yaml");
            }

            part viewDefinitionModel: ViewDefinitionModel;
            part sysmlSnippetGenerator: SysmlSnippetGenerator;
        }
    }
}
