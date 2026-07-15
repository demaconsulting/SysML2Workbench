# SysML2Workbench

## Verification Approach

Tests in `test/DemaConsulting.SysML2Workbench.Tests/SysML2WorkbenchTests.cs` exercise the whole SysML2Workbench Phase 0
workflow end to end through MainWindowShell. The suite uses real subsystem units plus temporary workspace and log
directories to drive integrated workspace, rendering, diagnostics, and logging behavior. The scenario list below follows
the authoritative mappings in `docs/reqstream/sysml2-workbench.yaml` and describes the implemented tests in present
tense.

## Test Environment

Tests run under the repository .NET test runner against temporary workspace and log directories on the local file
system. The suite uses local file fixtures only and requires no network or external services.

## Acceptance Criteria

- All implemented tests in `test/DemaConsulting.SysML2Workbench.Tests/SysML2WorkbenchTests.cs` that correspond to the
  scenarios below pass with zero failures.
- The assertions exercised by these scenarios continue to verify the behavior traced from
  `docs/reqstream/sysml2-workbench.yaml` using the real paths and collaborators described above.
- Any regression in the covered normal, boundary, or error flows produces a failing xUnit assertion rather than a
  speculative or placeholder verification statement.

## Test Scenarios

**OpenWorkspace_LoadsAndRefreshesWorkspace**: Opening a workspace loads it and, after an external change, refreshes it.
Verified by `SysML2WorkbenchTests.OpenWorkspace_LoadsAndRefreshesWorkspace`.

**SelectPredefinedView_RendersDiagram**: Selecting a predefined view renders its diagram end to end. Verified by
`SysML2WorkbenchTests.SelectPredefinedView_RendersDiagram`.

**BuildCustomView_PreviewsAndExportsSnippet**: The full GUI custom-view builder workflow: building a definition,
previewing it, and exporting it as a SysML snippet. Verified by
`SysML2WorkbenchTests.BuildCustomView_PreviewsAndExportsSnippet`.

**OpenWorkspace_ShowsWorkspaceDiagnostics**: Opening a workspace with parser/reference-resolution problems shows
workspace diagnostics. Verified by `SysML2WorkbenchTests.OpenWorkspace_ShowsWorkspaceDiagnostics`.

**StartSession_OpensShellAndWritesOperationalLogs**: Starting a session opens the shell and writes operational log
entries locally. Verified by `SysML2WorkbenchTests.StartSession_OpensShellAndWritesOperationalLogs`.
