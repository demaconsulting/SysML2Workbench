package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def WorkspaceSubsystem {
            doc /* Folder-based workspace, live file watching, glob/import resolution. */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/WorkspaceSubsystemTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/workspace-subsystem.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/workspace-subsystem.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/workspace-subsystem.yaml */

            part workspaceModel: WorkspaceModel;
            part fileWatcher: FileWatcher;
            part diagnosticsAggregator: DiagnosticsAggregator;
        }
    }
}
