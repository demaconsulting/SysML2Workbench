package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def SysmlSnippetGenerator {
            doc /* Generates copy-pasteable SysML view syntax from GUI selections. */

            metadata info : SoftwareInfo {
                description = "Generates copy-pasteable SysML view syntax from GUI selections.";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/ViewBuilderSubsystem/SysmlSnippetGenerator.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystem/SysmlSnippetGeneratorTests.cs");
                designFiles = ("docs/design/sysml2-workbench/view-builder-subsystem/sysml-snippet-generator.md");
                verificationFiles = ("docs/verification/sysml2-workbench/view-builder-subsystem/sysml-snippet-generator.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/view-builder-subsystem/sysml-snippet-generator.yaml");
            }
        }
    }
}
