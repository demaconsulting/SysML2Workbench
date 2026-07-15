## LayoutRenderingSubsystem

### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystemTests.cs` exercise
LayoutRenderingSubsystem's units (LayoutInvoker, SvgCanvasHost) together. The suite renders both predefined and GUI-
built custom views, then loads the SVG into the canvas host to verify displayed behavior end to end. The scenario list
below follows the authoritative mappings in `docs/reqstream/sysml2-workbench/layout-rendering-subsystem.yaml` and
describes the implemented tests in present tense.

### Test Environment

Tests run under the standard .NET test runner with real workspace models and in-memory SVG output that is loaded
directly into the canvas host. No external services are required.

### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystemTests.cs` that correspond
  to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/layout-rendering-subsystem.yaml` using the real paths and collaborators described
  above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

### Test Scenarios

**RenderPredefinedView_DisplaysSvgDiagram**: Selecting a predefined view renders it and displays the resulting SVG
diagram in the canvas host. Verified by `LayoutRenderingSubsystemTests.RenderPredefinedView_DisplaysSvgDiagram`.

**RenderCustomView_SupportsPanAndZoom**: A rendered custom view is displayed in a canvas host that supports pan and zoom
interaction over it. Verified by `LayoutRenderingSubsystemTests.RenderCustomView_SupportsPanAndZoom`.
