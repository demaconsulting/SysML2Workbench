package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def DiagnosticsAggregator {
            doc /* Collects workspace diagnostics into a unified view. */

            metadata info : SoftwareInfo {
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/WorkspaceSubsystem/DiagnosticsAggregator.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/DiagnosticsAggregatorTests.cs");
                designFiles = ("docs/design/sysml2-workbench/workspace-subsystem/diagnostics-aggregator.md");
                verificationFiles = ("docs/verification/sysml2-workbench/workspace-subsystem/diagnostics-aggregator.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/workspace-subsystem/diagnostics-aggregator.yaml");
            }
        }
    }
}
