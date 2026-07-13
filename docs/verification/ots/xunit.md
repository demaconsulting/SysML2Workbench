## XUnit

### Verification Approach

OTS integration tests in `test/OtsSoftwareTests/XUnitTests.cs` qualify the repository test harness by confirming that xUnit v3 discovers, executes, and reports pass/fail results for parameterized cases, and that the repository's build script wires xUnit's results into a form ReqStream can use as traceable evidence.

### Test Scenarios

**RunVerificationSuite_ReportsPassingResults**: A `[Theory]` with multiple `[InlineData]` cases exercises real production behavior (`SvgCanvasHost`'s zoom clamping) so xUnit v3 must discover, execute, and report a result for each parameterized case, demonstrating the mechanics the repository relies on to run its whole verification suite.

**GenerateResults_ProvideReqStreamEvidence**: The test confirms xUnit v3's per-test context (`Xunit.TestContext.Current`) is available during execution, and reads `build.ps1` to confirm it configures `dotnet test --logger trx --results-directory artifacts/tests` - the concrete mechanism that turns xUnit results into evidence files ReqStream can link from requirement records.
