package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def ViewCatalogSubsystem {
            doc /* Renders the six predefined view kinds (General, Interconnection, State Transition, Action Flow, Sequence, Grid). */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/ViewCatalogSubsystemTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/view-catalog-subsystem.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/view-catalog-subsystem.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/view-catalog-subsystem.yaml */

            part viewCatalogPresenter: ViewCatalogPresenter;
        }
    }
}
