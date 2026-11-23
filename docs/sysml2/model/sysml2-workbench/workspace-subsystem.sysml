package SysML2Workbench_WorkspaceSubsystem {
    public import SysML2Workbench_WorkspaceModel::*;
    public import SysML2Workbench_WorkspaceSourceSet::*;
    public import SysML2Workbench_FileWatcher::*;
    public import SysML2Workbench_DiagnosticsAggregator::*;
    private import SoftwareItemMetadata::*;

    part def WorkspaceSubsystem {
        doc /* Folder-based workspace, live file watching, glob/import resolution. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystemTests.cs");
            designFiles = ("docs/design/sysml2-workbench/workspace-subsystem.md");
            verificationFiles = ("docs/verification/sysml2-workbench/workspace-subsystem.md");
            requirementFiles = ("docs/reqstream/sysml2-workbench/workspace-subsystem.yaml");
        }

        part workspaceModel: WorkspaceModel;
        part workspaceSourceSet: WorkspaceSourceSet;
        part fileWatcher: FileWatcher;
        part diagnosticsAggregator: DiagnosticsAggregator;
    }
}
