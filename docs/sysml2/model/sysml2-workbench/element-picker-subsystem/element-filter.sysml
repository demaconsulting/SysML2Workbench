package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def ElementFilter {
            doc /* Reusable Avalonia UserControl and ObservableObject view model that presents a filter-only candidate list: type-label chip row (OR), substring search text (AND). No selection concept. Composed by ElementPicker for callers that also need a selection, or used standalone by callers with no target-element concept (for example the Query dialog's List Query Type). */

            metadata info : SoftwareInfo {
                description = "Reusable Avalonia UserControl and ObservableObject view model that presents a filter-only candidate list: type-label chip row (OR), substring search text (AND). No selection concept. Composed by ElementPicker for callers that also need a selection, or used standalone by callers with no target-element concept (for example the Query dialog's List Query Type).";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/ElementPickerSubsystem/ElementFilterViewModel.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/ElementFilterViewModelTests.cs");
                designFiles = ("docs/design/sysml2-workbench/element-picker-subsystem/element-filter.md");
                verificationFiles = ("docs/verification/sysml2-workbench/element-picker-subsystem/element-filter.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/element-picker-subsystem/element-filter.yaml");
            }
        }
    }
}
