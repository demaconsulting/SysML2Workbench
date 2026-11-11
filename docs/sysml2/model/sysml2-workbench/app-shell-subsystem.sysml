package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def AppShellSubsystem {
            doc /* Main window, navigation, tabbed views (Avalonia 12 desktop shell). */

            metadata info : SoftwareInfo {
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystemTests.cs");
                designFiles = ("docs/design/sysml2-workbench/app-shell-subsystem.md");
                verificationFiles = ("docs/verification/sysml2-workbench/app-shell-subsystem.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/app-shell-subsystem.yaml");
            }

            part mainWindowShell: MainWindowShell;
            part workspacePanel: WorkspacePanel;
            part aboutDialog: AboutDialog;
            part viewBuilderDialog: ViewBuilderDialog;
            part queryDialog: QueryDialog;
        }
    }
}
