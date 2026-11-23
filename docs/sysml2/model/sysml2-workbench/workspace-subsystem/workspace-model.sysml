package SysML2Workbench_WorkspaceModel {
    private import SoftwareItemMetadata::*;

    part def WorkspaceModel {
        doc /* In-memory folder/file tree and per-file parse state. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.SysML2Workbench/WorkspaceSubsystem/WorkspaceModel.cs");
            testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/WorkspaceModelTests.cs");
            designFiles = ("docs/design/sysml2-workbench/workspace-subsystem/workspace-model.md");
            verificationFiles = ("docs/verification/sysml2-workbench/workspace-subsystem/workspace-model.md");
            requirementFiles = ("docs/reqstream/sysml2-workbench/workspace-subsystem/workspace-model.yaml");
        }
    }
}
