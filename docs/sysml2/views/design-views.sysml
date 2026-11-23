package SysML2WorkbenchDesignViews {
        private import StandardViewDefinitions::*;
        private import SysML2Workbench::*;

        view SoftwareStructureView : InterconnectionView {
            expose SysML2Workbench::SysML2WorkbenchSystem;
        }

        view SysML2WorkbenchView {
            expose SysML2Workbench::SysML2WorkbenchSystem;
        }

        view WorkspaceSubsystemView {
            expose WorkspaceSubsystem;
        }

        view ViewCatalogSubsystemView {
            expose ViewCatalogSubsystem;
        }

        view ViewBuilderSubsystemView {
            expose ViewBuilderSubsystem;
        }

        view LayoutRenderingSubsystemView {
            expose LayoutRenderingSubsystem;
        }

        view DiagnosticsPanelSubsystemView {
            expose DiagnosticsPanelSubsystem;
        }

        view LoggingSubsystemView {
            expose LoggingSubsystem;
        }

        view AppShellSubsystemView {
            expose AppShellSubsystem;
        }

        view ElementPickerSubsystemView {
            expose ElementPickerSubsystem;
        }
}
