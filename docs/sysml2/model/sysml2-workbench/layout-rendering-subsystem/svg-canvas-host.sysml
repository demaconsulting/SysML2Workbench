package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def SvgCanvasHost {
            doc /* Hosts rendered SVG output in the Avalonia diagram canvas. */

            metadata info : SoftwareInfo {
                description = "Hosts rendered SVG output in the Avalonia diagram canvas.";
                sourceFiles = ("src/DemaConsulting.SysML2Workbench/LayoutRenderingSubsystem/SvgCanvasHost.cs");
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystem/SvgCanvasHostTests.cs");
                designFiles = ("docs/design/sysml2-workbench/layout-rendering-subsystem/svg-canvas-host.md");
                verificationFiles = ("docs/verification/sysml2-workbench/layout-rendering-subsystem/svg-canvas-host.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/layout-rendering-subsystem/svg-canvas-host.yaml");
            }
        }
    }
}
