### SvgCanvasHost

#### Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystem/SvgCanvasHostTests.cs` exercise
`SvgCanvasHost` directly. The suite loads in-memory SVG content and verifies default load state, pan and zoom behavior,
and no-content guards. The scenario list below follows the authoritative mappings in
`docs/reqstream/sysml2-workbench/layout-rendering-subsystem/svg-canvas-host.yaml` and describes the implemented tests in
present tense.

#### Test Environment

Tests run under the standard .NET test runner with in-memory SVG markup loaded directly into the host. No external
services are required.

#### Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/LayoutRenderingSubsystem/SvgCanvasHostTests.cs`
  that correspond to the scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench/layout-rendering-subsystem/svg-canvas-host.yaml` using the real paths and
  collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

#### Test Scenarios

**LoadSvgDocument_DisplaysDiagramCanvas**: Loading an SVG document displays the diagram canvas with default zoom and pan
state. Verified by `SvgCanvasHostTests.LoadSvgDocument_DisplaysDiagramCanvas`.

**UserInteraction_PansAndZoomsDiagram**: User interaction can both pan and zoom a loaded diagram, and that out-of-range
zoom requests are clamped rather than rejected. Verified by `SvgCanvasHostTests.UserInteraction_PansAndZoomsDiagram`.

**Clear_ContentLoaded_DiscardsSvgAndResetsViewport**: Clearing a loaded diagram discards its SVG content and resets
zoom and pan to their defaults. Verified by `SvgCanvasHostTests.Clear_ContentLoaded_DiscardsSvgAndResetsViewport`.
