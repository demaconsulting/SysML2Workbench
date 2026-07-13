package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def DiagnosticsPanelSubsystem {
            doc /* UI list of workspace diagnostics. */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/DiagnosticsPanelSubsystemTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/diagnostics-panel-subsystem.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/diagnostics-panel-subsystem.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/diagnostics-panel-subsystem.yaml */

            part diagnosticsListView: DiagnosticsListView;
        }
    }
}
