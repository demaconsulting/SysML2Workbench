### LayoutInvoker

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystem/LayoutInvokerTests.cs` exercise
`LayoutInvoker` directly. The suite renders predefined and custom views against a real loaded workspace and checks SVG
output plus workspace immutability. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/layout-rendering-subsystem/layout-invoker.yaml` and describes the implemented tests in
present tense.

#### Test Environment

Tests run under the standard .NET test runner with temporary workspace files, real loaded model data, and in-memory SVG
output. No external services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystem/LayoutInvokerTests.cs`
  that correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/layout-rendering-subsystem/layout-invoker.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**RenderPredefinedView_DisplaysSvgDiagram**: Selecting a predefined view produces SVG diagram markup. Verified by
`LayoutInvokerTests.RenderPredefinedView_DisplaysSvgDiagram`.

**RenderCustomView_SupportsPanAndZoom**: A GUI-built custom view with multiple expose targets renders to SVG suitable
for display with pan and zoom (i.e. loadable into SvgCanvasHost). Verified by
`LayoutInvokerTests.RenderCustomView_SupportsPanAndZoom`.

**RenderCustomView_ScopesOutputToSelectedTargetsOnly**: Regression coverage for the SysML2Tools 0.1.0-beta.8
`ResolvedExposeMembers` requirement - selecting only one of two workspace elements produces SVG that does not contain
the unselected element, proving the ephemeral preview node is correctly scoped rather than rendering the entire
workspace. Verified by `LayoutInvokerTests.RenderCustomView_ScopesOutputToSelectedTargetsOnly`.

**RenderCustomView_SameQualifiedNameTwoRecursionKinds_RendersWithoutError**: A custom view exposing the same
qualified name twice under two different recursion kinds renders without error, covering the valid SysML v2 pattern
of exposing the same package both exactly and via its direct children. Verified by
`LayoutInvokerTests.RenderCustomView_SameQualifiedNameTwoRecursionKinds_RendersWithoutError`.
