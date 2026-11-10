package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def SysML2Workbench {
            doc /* Cross-platform desktop viewer and IDE shell for SysML v2 textual models. */

            metadata info : SoftwareInfo {
                description = "Cross-platform desktop viewer and IDE shell for SysML v2 textual models.";
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/SysML2WorkbenchTests.cs");
                designFiles = ("docs/design/sysml2-workbench.md");
                verificationFiles = ("docs/verification/sysml2-workbench.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench.yaml");
            }

            part workspaceSubsystem: WorkspaceSubsystem;
            part viewCatalogSubsystem: ViewCatalogSubsystem;
            part viewBuilderSubsystem: ViewBuilderSubsystem;
            part layoutRenderingSubsystem: LayoutRenderingSubsystem;
            part diagnosticsPanelSubsystem: DiagnosticsPanelSubsystem;
            part loggingSubsystem: LoggingSubsystem;
            part elementPickerSubsystem: ElementPickerSubsystem;
            part appShellSubsystem: AppShellSubsystem;
        }
    }
}
