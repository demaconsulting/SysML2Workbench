package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def DiagnosticsPanelSubsystem {
            doc /* UI list of workspace diagnostics. */

            metadata info : SoftwareInfo {
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/DiagnosticsPanelSubsystemTests.cs");
                designFiles = ("docs/design/sysml2-workbench/diagnostics-panel-subsystem.md");
                verificationFiles = ("docs/verification/sysml2-workbench/diagnostics-panel-subsystem.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/diagnostics-panel-subsystem.yaml");
            }

            part diagnosticsListView: DiagnosticsListView;
        }
    }
}
