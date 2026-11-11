package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def ViewBuilderDialog {
            doc /* Modal dialog for composing a custom SysML view (view kind, expose targets, filter expression) with a live SVG preview, committed as a new diagram tab or cancelled with zero side effects. */

            metadata info : SoftwareInfo {
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/AppShellSubsystem/ViewBuilderDialogViewModel.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/ViewBuilderDialogViewModelTests.cs");
                designFiles = ("docs/design/sysml2-workbench/app-shell-subsystem/view-builder-dialog.md");
                verificationFiles = ("docs/verification/sysml2-workbench/app-shell-subsystem/view-builder-dialog.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/app-shell-subsystem/view-builder-dialog.yaml");
            }
        }
    }
}
