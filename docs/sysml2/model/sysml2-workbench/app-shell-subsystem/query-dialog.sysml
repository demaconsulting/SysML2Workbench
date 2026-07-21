package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def QueryDialog {
            doc /* Modal dialog opened from the main window's Query menu that lets the user browse or query workspace elements through a single, always-visible adaptive form (no tabs, no "Run" button): a Query Type combo box offers a merged "List" client-side filter plus ten element-scoped QueryVerb operations dispatched through DemaConsulting.SysML2Tools.Query.QueryEngine.Execute, driven by one always-visible element picker. Every relevant change recomputes the result immediately. Results render through QueryResultRenderer and copy to the clipboard as Markdown or JSON via a right-click context menu on the results panel. */

            comment sourceRef /* Source: src/DemaConsulting.SysML2Workbench/AppShellSubsystem/QueryDialogViewModel.cs */
            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/QueryDialogViewModelTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/app-shell-subsystem/query-dialog.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/app-shell-subsystem/query-dialog.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/app-shell-subsystem/query-dialog.yaml */
        }
    }
}
