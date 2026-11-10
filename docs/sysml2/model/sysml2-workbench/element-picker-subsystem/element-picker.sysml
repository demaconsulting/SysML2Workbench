package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def ElementPicker {
            doc /* Reusable Avalonia UserControl and ObservableObject view model that presents a filterable list of candidate elements: type-label chip row (OR), substring search text (AND), single selection. Independent of any workspace or shell type - callers hand it a candidate list. */

            metadata info : SoftwareInfo {
                description = "Reusable Avalonia UserControl and ObservableObject view model that presents a filterable list of candidate elements: type-label chip row (OR), substring search text (AND), single selection. Independent of any workspace or shell type - callers hand it a candidate list.";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/ElementPickerSubsystem/ElementPickerViewModel.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystem/ElementPickerViewModelTests.cs");
                designFiles = ("docs/design/sysml2-workbench/element-picker-subsystem/element-picker.md");
                verificationFiles = ("docs/verification/sysml2-workbench/element-picker-subsystem/element-picker.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/element-picker-subsystem/element-picker.yaml");
            }
        }
    }
}
