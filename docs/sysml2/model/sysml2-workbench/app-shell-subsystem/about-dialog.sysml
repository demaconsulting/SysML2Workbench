package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def AboutDialog {
            doc /* Modal About dialog presenting the application's name, tagline, build-stamped version, copyright, and key OSS dependency list. */

            metadata info : SoftwareInfo {
                description = "Modal About dialog presenting the application's name, tagline, build-stamped version, copyright, and key OSS dependency list.";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/AppShellSubsystem/AboutDialogViewModel.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/AboutDialogViewModelTests.cs");
                designFiles = ("docs/design/sysml2-workbench/app-shell-subsystem/about-dialog.md");
                verificationFiles = ("docs/verification/sysml2-workbench/app-shell-subsystem/about-dialog.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/app-shell-subsystem/about-dialog.yaml");
            }
        }
    }
}
