package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package Ots {
        part def SysML2Tools {
            doc /* Collective SysML2Tools NuGet dependencies for parsing, standard library loading, and layout generation. */

            metadata info : SoftwareInfo {
                description = "Collective SysML2Tools NuGet dependencies for parsing, standard library loading, and layout generation.";
                designFiles = ("docs/design/ots/sysml2-tools.md");
                verificationFiles = ("docs/verification/ots/sysml2-tools.md");
                requirementFiles = ("docs/reqstream/ots/sysml2-tools.yaml");
            }
        }
        part def Rendering {
            doc /* DemaConsulting.Rendering NuGet dependency for SVG generation and rendering primitives. */

            metadata info : SoftwareInfo {
                description = "DemaConsulting.Rendering NuGet dependency for SVG generation and rendering primitives.";
                designFiles = ("docs/design/ots/rendering.md");
                verificationFiles = ("docs/verification/ots/rendering.md");
                requirementFiles = ("docs/reqstream/ots/rendering.yaml");
            }
        }
        part def Avalonia {
            doc /* Avalonia UI framework dependency for the desktop application shell and controls. */

            metadata info : SoftwareInfo {
                description = "Avalonia UI framework dependency for the desktop application shell and controls.";
                designFiles = ("docs/design/ots/avalonia.md");
                verificationFiles = ("docs/verification/ots/avalonia.md");
                requirementFiles = ("docs/reqstream/ots/avalonia.yaml");
            }
        }
        part def XUnit {
            doc /* xUnit test framework used for repository verification code. */

            metadata info : SoftwareInfo {
                description = "xUnit test framework used for repository verification code.";
                designFiles = ("docs/design/ots/xunit.md");
                verificationFiles = ("docs/verification/ots/xunit.md");
                requirementFiles = ("docs/reqstream/ots/xunit.yaml");
            }
        }
        part def Dock {
            doc /* AvaloniaUI/Dock NuGet dependency for the resizable, floatable, closable dockable panel layout hosting the desktop application shell. */

            metadata info : SoftwareInfo {
                description = "AvaloniaUI/Dock NuGet dependency for the resizable, floatable, closable dockable panel layout hosting the desktop application shell.";
                designFiles = ("docs/design/ots/dock.md");
                verificationFiles = ("docs/verification/ots/dock.md");
                requirementFiles = ("docs/reqstream/ots/dock.yaml");
            }
        }
        part def CommunityToolkitMvvm {
            doc /* CommunityToolkit.Mvvm NuGet dependency providing observable-property source generation for the Dock panel view models. */

            metadata info : SoftwareInfo {
                description = "CommunityToolkit.Mvvm NuGet dependency providing observable-property source generation for the Dock panel view models.";
                designFiles = ("docs/design/ots/communitytoolkit-mvvm.md");
                verificationFiles = ("docs/verification/ots/communitytoolkit-mvvm.md");
                requirementFiles = ("docs/reqstream/ots/communitytoolkit-mvvm.yaml");
            }
        }
        part def AvaloniaEdit {
            doc /* Avalonia.AvaloniaEdit NuGet dependency for the read-only, syntax-highlighted text editor control backing the source-text viewer document tabs. */

            metadata info : SoftwareInfo {
                description = "Avalonia.AvaloniaEdit NuGet dependency for the read-only, syntax-highlighted text editor control backing the source-text viewer document tabs.";
                designFiles = ("docs/design/ots/avaloniaedit.md");
                verificationFiles = ("docs/verification/ots/avaloniaedit.md");
                requirementFiles = ("docs/reqstream/ots/avaloniaedit.yaml");
            }
        }
        part def Appium {
            doc /* Appium.WebDriver NuGet dependency, together with Appium's NovaWindows driver, used to drive the compiled Desktop application end-to-end through its real accessibility tree from test/DemaConsulting.SysML2Workbench.IntegrationTests. */

            metadata info : SoftwareInfo {
                description = "Appium.WebDriver NuGet dependency, together with Appium's NovaWindows driver, used to drive the compiled Desktop application end-to-end through its real accessibility tree from test/DemaConsulting.SysML2Workbench.IntegrationTests.";
                designFiles = ("docs/design/ots/appium.md");
                verificationFiles = ("docs/verification/ots/appium.md");
                requirementFiles = ("docs/reqstream/ots/appium.yaml");
            }
        }
    }
}
