package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def FileWatcher {
            doc /* Detects external file changes and triggers incremental reload. */

            metadata info : SoftwareInfo {
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/WorkspaceSubsystem/FileWatcher.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystem/FileWatcherTests.cs");
                designFiles = ("docs/design/sysml2-workbench/workspace-subsystem/file-watcher.md");
                verificationFiles = ("docs/verification/sysml2-workbench/workspace-subsystem/file-watcher.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/workspace-subsystem/file-watcher.yaml");
            }
        }
    }
}
