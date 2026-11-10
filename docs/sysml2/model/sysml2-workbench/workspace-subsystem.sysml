package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def WorkspaceSubsystem {
            doc /* Folder-based workspace, live file watching, glob/import resolution. */

            metadata info : SoftwareInfo {
                description = "Folder-based workspace, live file watching, glob/import resolution.";
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
}
