package SysML2Workbench_LayoutInvoker {
    private import SoftwareItemMetadata::*;

    part def LayoutInvoker {
        doc /* Invokes SysML2Tools layout strategies for the selected view. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.SysML2Workbench/LayoutRenderingSubsystem/LayoutInvoker.cs");
            testFiles = ("test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystem/LayoutInvokerTests.cs");
            designFiles = ("docs/design/sysml2-workbench/layout-rendering-subsystem/layout-invoker.md");
            verificationFiles = ("docs/verification/sysml2-workbench/layout-rendering-subsystem/layout-invoker.md");
            requirementFiles = ("docs/reqstream/sysml2-workbench/layout-rendering-subsystem/layout-invoker.yaml");
        }
    }
}
