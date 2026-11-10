package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def WorkspaceModel {
            doc /* In-memory folder/file tree and per-file parse state. */

            metadata info : SoftwareInfo {
                description = "In-memory folder/file tree and per-file parse state.";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/WorkspaceSubsystem/WorkspaceModel.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/WorkspaceModelTests.cs");
                designFiles = ("docs/design/sysml2-workbench/workspace-subsystem/workspace-model.md");
                verificationFiles = ("docs/verification/sysml2-workbench/workspace-subsystem/workspace-model.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/workspace-subsystem/workspace-model.yaml");
            }
        }
    }
}
