package SysML2WorkbenchArchitecture {
    package Ots {
        part def SysML2Tools {
            doc /* Collective SysML2Tools NuGet dependencies for parsing, standard library loading, and layout generation. */

            comment designRef /* Design: docs/design/ots/sysml2-tools.md */
            comment verificationRef /* Verification: docs/verification/ots/sysml2-tools.md */
            comment reqRef /* Requirements: docs/reqstream/ots/sysml2-tools.yaml */
        }
        part def Rendering {
            doc /* DemaConsulting.Rendering NuGet dependency for SVG generation and rendering primitives. */

            comment designRef /* Design: docs/design/ots/rendering.md */
            comment verificationRef /* Verification: docs/verification/ots/rendering.md */
            comment reqRef /* Requirements: docs/reqstream/ots/rendering.yaml */
        }
        part def Avalonia {
            doc /* Avalonia UI framework dependency for the desktop application shell and controls. */

            comment designRef /* Design: docs/design/ots/avalonia.md */
            comment verificationRef /* Verification: docs/verification/ots/avalonia.md */
            comment reqRef /* Requirements: docs/reqstream/ots/avalonia.yaml */
        }
        part def XUnit {
            doc /* xUnit test framework used for repository verification code. */

            comment designRef /* Design: docs/design/ots/xunit.md */
            comment verificationRef /* Verification: docs/verification/ots/xunit.md */
            comment reqRef /* Requirements: docs/reqstream/ots/xunit.yaml */
        }
        part def Dock {
            doc /* AvaloniaUI/Dock NuGet dependency for the resizable, floatable, closable dockable panel layout hosting the desktop application shell. */

            comment designRef /* Design: docs/design/ots/dock.md */
            comment verificationRef /* Verification: docs/verification/ots/dock.md */
            comment reqRef /* Requirements: docs/reqstream/ots/dock.yaml */
        }
        part def CommunityToolkitMvvm {
            doc /* CommunityToolkit.Mvvm NuGet dependency providing observable-property source generation for the Dock panel view models. */

            comment designRef /* Design: docs/design/ots/communitytoolkit-mvvm.md */
            comment verificationRef /* Verification: docs/verification/ots/communitytoolkit-mvvm.md */
            comment reqRef /* Requirements: docs/reqstream/ots/communitytoolkit-mvvm.yaml */
        }
        part def AvaloniaEdit {
            doc /* Avalonia.AvaloniaEdit NuGet dependency for the read-only, syntax-highlighted text editor control backing the source-text viewer document tabs. */

            comment designRef /* Design: docs/design/ots/avaloniaedit.md */
            comment verificationRef /* Verification: docs/verification/ots/avaloniaedit.md */
            comment reqRef /* Requirements: docs/reqstream/ots/avaloniaedit.yaml */
        }
    }
}
