package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def QueryDialog {
            doc /* Modal dialog opened from the main window's Query menu that lets the user browse or query workspace elements: a Browse tab that filters the workspace client-side, and an Element Query tab that dispatches ten element-scoped QueryVerb operations through DemaConsulting.SysML2Tools.Query.QueryEngine.Execute. Results render through QueryResultRenderer and copy to the clipboard as Markdown or JSON. */

            comment sourceRef /* Source: src/DemaConsulting.SysML2Workbench/AppShellSubsystem/QueryDialogViewModel.cs */
            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/AppShellSubsystem/QueryDialogViewModelTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/app-shell-subsystem/query-dialog.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/app-shell-subsystem/query-dialog.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/app-shell-subsystem/query-dialog.yaml */
        }
    }
}
