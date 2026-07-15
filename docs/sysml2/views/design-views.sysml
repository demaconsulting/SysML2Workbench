package SysML2WorkbenchArchitecture {
        view SoftwareStructureView {
            expose SysML2Workbench;
        }

        view SysML2WorkbenchView {
            expose SysML2Workbench::SysML2Workbench;
        }

        view WorkspaceSubsystemView {
            expose SysML2Workbench::WorkspaceSubsystem;
        }

        view ViewCatalogSubsystemView {
            expose SysML2Workbench::ViewCatalogSubsystem;
        }

        view ViewBuilderSubsystemView {
            expose SysML2Workbench::ViewBuilderSubsystem;
        }

        view LayoutRenderingSubsystemView {
            expose SysML2Workbench::LayoutRenderingSubsystem;
        }

        view DiagnosticsPanelSubsystemView {
            expose SysML2Workbench::DiagnosticsPanelSubsystem;
        }

        view LoggingSubsystemView {
            expose SysML2Workbench::LoggingSubsystem;
        }

        view AppShellSubsystemView {
            expose SysML2Workbench::AppShellSubsystem;
        }
}
