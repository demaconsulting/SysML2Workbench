package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def WorkspaceSourceSet {
            doc /* Ordered file/folder source list; resolves to a merged, deduplicated file set with per-source attribution. */

            metadata info : SoftwareInfo {
                description = "Ordered file/folder source list; resolves to a merged, deduplicated file set with per-source attribution.";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/WorkspaceSubsystem/WorkspaceSourceSet.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/WorkspaceSourceSetTests.cs");
                designFiles = ("docs/design/sysml2-workbench/workspace-subsystem/workspace-source-set.md");
                verificationFiles = ("docs/verification/sysml2-workbench/workspace-subsystem/workspace-source-set.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/workspace-subsystem/workspace-source-set.yaml");
            }
        }
    }
}
