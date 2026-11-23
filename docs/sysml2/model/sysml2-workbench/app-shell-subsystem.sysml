package SysML2Workbench_AppShellSubsystem {
    public import SysML2Workbench_MainWindowShell::*;
    public import SysML2Workbench_WorkspacePanel::*;
    public import SysML2Workbench_AboutDialog::*;
    public import SysML2Workbench_ViewBuilderDialog::*;
    public import SysML2Workbench_QueryDialog::*;
    private import SoftwareItemMetadata::*;

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
