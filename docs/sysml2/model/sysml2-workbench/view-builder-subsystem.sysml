package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def ViewBuilderSubsystem {
            doc /* GUI for constructing custom views (view type, multi-target expose selection, filter expression). */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/ViewBuilderSubsystemTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/view-builder-subsystem.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/view-builder-subsystem.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/view-builder-subsystem.yaml */

            part viewDefinitionModel: ViewDefinitionModel;
            part sysmlSnippetGenerator: SysmlSnippetGenerator;
        }
    }
}
