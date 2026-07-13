package SysML2WorkbenchArchitecture {
    package SysML2Workbench {
        part def LayoutRenderingSubsystem {
            doc /* Turns a selected view into pixels via SysML2Tools layout and DemaConsulting.Rendering SVG output. */

            comment testRef /* Test: test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystemTests.cs */
            comment designRef /* Design: docs/design/sysml2-workbench/layout-rendering-subsystem.md */
            comment verificationRef /* Verification: docs/verification/sysml2-workbench/layout-rendering-subsystem.md */
            comment reqRef /* Requirements: docs/reqstream/sysml2-workbench/layout-rendering-subsystem.yaml */

            part layoutInvoker: LayoutInvoker;
            part svgCanvasHost: SvgCanvasHost;
        }
    }
}
