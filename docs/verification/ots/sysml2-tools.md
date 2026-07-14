## SysML2Tools

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/SysML2ToolsTests.cs` verify the real parsing, semantic-resolution, standard-library, and rendering APIs consumed by SysML2Workbench (`GlobFileCollector`, `StdlibProvider`, `WorkspaceLoader`, `DiagramRenderer`), exercised against representative workspace fixtures, because vendor behavior is the primary evidence target for this dependency.

### Test Scenarios

**LoadWorkspaceModel_ParsesAndResolvesImports**: A two-file workspace, where one file imports a definition declared in the other, is discovered and loaded through the same `GlobFileCollector`/`StdlibProvider`/`WorkspaceLoader` pipeline the workbench's `WorkspaceModel` uses. The dependency proves it discovers both files, parses without diagnostics, and resolves the cross-file import.

**RenderView_GeneratesLayoutGraph**: A loaded workspace containing a named view usage is submitted to `DiagramRenderer.RenderWorkspace`, and the dependency returns non-empty rendered diagram output for that view. The test name predates the empirical discovery that SysML2Tools 0.1.0-beta.7 has no public `LayoutGraph` type or layout-strategy registry (see the planning report's Assumption #1); the name is kept for ReqStream traceability, and the scenario verifies the real single-call `RenderWorkspace` contract instead. This dependency's public surface has since been confirmed unchanged through SysML2Tools 0.1.0-beta.8.
