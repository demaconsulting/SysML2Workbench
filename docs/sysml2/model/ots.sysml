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
    }
}
