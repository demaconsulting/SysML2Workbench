package SysML2WorkbenchArchitecture {
    private import SoftwareItemMetadata::*;

    package SysML2Workbench {
        part def LayoutRenderingSubsystem {
            doc /* Turns a selected view into pixels via SysML2Tools layout and DemaConsulting.Rendering SVG output. */

            metadata info : SoftwareInfo {
                testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystemTests.cs");
                designFiles = ("docs/design/sysml2-workbench/layout-rendering-subsystem.md");
                verificationFiles = ("docs/verification/sysml2-workbench/layout-rendering-subsystem.md");
                requirementFiles = ("docs/reqstream/sysml2-workbench/layout-rendering-subsystem.yaml");
            }

            part layoutInvoker: LayoutInvoker;
            part svgCanvasHost: SvgCanvasHost;
        }
    }
}
