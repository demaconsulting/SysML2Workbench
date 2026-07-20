package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def AppShellSubsystem {
            doc /* Main window, navigation, tabbed views (Avalonia 12 desktop shell). */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystemTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/app-shell-subsystem.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/app-shell-subsystem.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/app-shell-subsystem.yaml */

            part mainWindowShell: MainWindowShell;
            part workspacePanel: WorkspacePanel;
            part aboutDialog: AboutDialog;
        }
    }
}
