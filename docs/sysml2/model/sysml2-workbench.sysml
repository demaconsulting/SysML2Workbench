package SysML2Workbench {
    public import SysML2Workbench_WorkspaceSubsystem::*;
    public import SysML2Workbench_ViewCatalogSubsystem::*;
    public import SysML2Workbench_ViewBuilderSubsystem::*;
    public import SysML2Workbench_LayoutRenderingSubsystem::*;
    public import SysML2Workbench_DiagnosticsPanelSubsystem::*;
    public import SysML2Workbench_LoggingSubsystem::*;
    public import SysML2Workbench_ElementPickerSubsystem::*;
    public import SysML2Workbench_AppShellSubsystem::*;
    private import OtsDependencies::*;
    private import SoftwareItemMetadata::*;

    part def SysML2WorkbenchSystem {
        doc /* Cross-platform desktop viewer and IDE shell for SysML v2 textual models. */

        metadata info : SoftwareInfo {
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
