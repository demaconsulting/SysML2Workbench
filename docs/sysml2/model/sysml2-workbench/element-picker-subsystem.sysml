package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def ElementPickerSubsystem {
            doc /* Shared, dialog-agnostic "element picker" control - a chip-row of type-label filters (OR semantics), a case-insensitive substring search, and a single-select list of candidate qualified names - hosted by more than one dialog (Custom View Builder, Query dialog) without duplicating the filter logic. */

            metadata info : SoftwareInfo {
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/ElementPickerSubsystemTests.cs");
                designFiles = ("docs/design/sysml2-workbench/element-picker-subsystem.md");
                verificationFiles = ("docs/verification/sysml2-workbench/element-picker-subsystem.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/element-picker-subsystem.yaml");
            }

            part elementFilter: ElementFilter;
            part elementPicker: ElementPicker;
        }
    }
}
