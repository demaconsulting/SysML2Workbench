package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def DiagnosticsListView {
            doc /* Displays workspace diagnostics in the UI list panel. */

            metadata info : SoftwareInfo {
                description = "Displays workspace diagnostics in the UI list panel.";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/DiagnosticsPanelSubsystem/DiagnosticsListView.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/DiagnosticsPanelSubsystem/DiagnosticsListViewTests.cs");
                designFiles = ("docs/design/sysml2-workbench/diagnostics-panel-subsystem/diagnostics-list-view.md");
                verificationFiles = ("docs/verification/sysml2-workbench/diagnostics-panel-subsystem/diagnostics-list-view.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/diagnostics-panel-subsystem/diagnostics-list-view.yaml");
            }
        }
    }
}
