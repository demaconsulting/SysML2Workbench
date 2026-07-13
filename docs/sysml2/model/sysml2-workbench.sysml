package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def SysML2Workbench {
            doc /* Cross-platform desktop viewer and IDE shell for SysML v2 textual models. */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/SysML2WorkbenchTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench.yaml */

            part workspaceSubsystem: WorkspaceSubsystem;
            part viewCatalogSubsystem: ViewCatalogSubsystem;
            part viewBuilderSubsystem: ViewBuilderSubsystem;
            part layoutRenderingSubsystem: LayoutRenderingSubsystem;
            part diagnosticsPanelSubsystem: DiagnosticsPanelSubsystem;
            part loggingSubsystem: LoggingSubsystem;
            part appShellSubsystem: AppShellSubsystem;
        }
    }
}
