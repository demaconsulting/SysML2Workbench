namespace OtsSoftwareTests;

/// <summary>
///     Verifies the OTS xUnit requirements in docs/reqstream/ots/xunit.yaml: that xUnit v3 is genuinely the
///     framework executing the repository's automated verification suite, and that its results are produced in
///     a form usable as traceable ReqStream evidence.
/// </summary>
public sealed class XUnitTests
{
    /// <summary>
    ///     Validates that xUnit v3 discovers, executes, and reports results for parameterized verification
    ///     cases exercised against real production behavior (<see cref="DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem.SvgCanvasHost" />'s
    ///     zoom clamping), demonstrating the mechanics the repository relies on to run its whole verification
    ///     suite.
    /// </summary>
    [Theory]
    [InlineData(0.01, 0.1)]
    [InlineData(1.0, 1.0)]
    [InlineData(50.0, 8.0)]
    public void RunVerificationSuite_ReportsPassingResults(double requested, double expectedClamped)
    {
        // Arrange
        var canvas = new DemaConsulting.SysML2Workbench.LayoutRenderingSubsystem.SvgCanvasHost();
        canvas.LoadSvg("<svg></svg>");

        // Act
        canvas.SetZoom(requested);

        // Assert: xUnit v3 executed and reported a pass/fail result for each parameterized case
        Assert.Equal(expectedClamped, canvas.ZoomLevel);
    }

    /// <summary>
    ///     Validates that xUnit v3 identifies the executing test by its method name through its per-test context
    ///     (<see cref="Xunit.TestContext" />), which is the name xUnit writes into the TRX results that ReqStream
    ///     matches against requirement test links. This test's own pass result in those TRX results is the
    ///     traceable evidence; no repository script text is inspected.
    /// </summary>
    [Fact]
    public void GenerateResults_ProvideReqStreamEvidence()
    {
        // Act
        var context = Xunit.TestContext.Current;
        context.TestOutputHelper?.WriteLine($"{nameof(GenerateResults_ProvideReqStreamEvidence)} executed under xUnit v3.");

        // Assert: xUnit reports this test under the exact method name requirement records link to
        Assert.NotNull(context.TestMethod);
        Assert.Equal(nameof(GenerateResults_ProvideReqStreamEvidence), context.TestMethod.MethodName);
        Assert.Equal(Xunit.TestPipelineStage.TestExecution, context.PipelineStage);
    }
}
