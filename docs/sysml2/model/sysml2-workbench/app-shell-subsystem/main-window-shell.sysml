package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def MainWindowShell {
            doc /* Hosts the main window, navigation, and tabbed view shell. */

            metadata info : SoftwareInfo {
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/AppShellSubsystem/MainWindowShell.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/MainWindowShellTests.cs");
                designFiles = ("docs/design/sysml2-workbench/app-shell-subsystem/main-window-shell.md");
                verificationFiles = ("docs/verification/sysml2-workbench/app-shell-subsystem/main-window-shell.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/app-shell-subsystem/main-window-shell.yaml");
            }
        }
    }
}
