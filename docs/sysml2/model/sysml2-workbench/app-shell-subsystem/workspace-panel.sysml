package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def WorkspacePanel {
            doc /* Dock tool panel presenting workspace sources as a tree, with Add File/Add Folder/Remove and drag-and-drop. */

            metadata info : SoftwareInfo {
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/AppShellSubsystem/WorkspacePanelToolViewModel.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/WorkspacePanelToolViewModelTests.cs");
                designFiles = ("docs/design/sysml2-workbench/app-shell-subsystem/workspace-panel.md");
                verificationFiles = ("docs/verification/sysml2-workbench/app-shell-subsystem/workspace-panel.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/app-shell-subsystem/workspace-panel.yaml");
            }
        }
    }
}
