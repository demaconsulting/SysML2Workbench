package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def ElementPickerSubsystem {
            doc /* Shared, dialog-agnostic "element picker" control - a chip-row of type-label filters (OR semantics), a case-insensitive substring search, and a single-select list of candidate qualified names - hosted by more than one dialog (Custom View Builder, Query dialog) without duplicating the filter logic. */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystemTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/element-picker-subsystem.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/element-picker-subsystem.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/element-picker-subsystem.yaml */

            part elementFilter: ElementFilter;
            part elementPicker: ElementPicker;
        }
    }
}
