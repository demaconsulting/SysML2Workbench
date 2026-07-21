package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def ElementFilter {
            doc /* Reusable Avalonia UserControl and ObservableObject view model that presents a filter-only candidate list: type-label chip row (OR), substring search text (AND). No selection concept. Composed by ElementPicker for callers that also need a selection, or used standalone by callers with no target-element concept (for example the Query dialog's List Query Type). */

            comment sourceRef /* Source: src/DemaConsulting.SysML2Workbench/ElementPickerSubsystem/ElementFilterViewModel.cs */
            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/ElementFilterViewModelTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/element-picker-subsystem/element-filter.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/element-picker-subsystem/element-filter.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/element-picker-subsystem/element-filter.yaml */
        }
    }
}
