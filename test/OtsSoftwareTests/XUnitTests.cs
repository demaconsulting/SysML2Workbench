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
    ///     Validates that xUnit v3's per-test context is available during execution (the mechanism
    ///     <see cref="Xunit.TestContext" />-aware test code relies on for structured output) and that the
    ///     repository's build script wires xUnit's TRX logger to a results directory, which is the concrete
    ///     mechanism that turns xUnit results into evidence files ReqStream can link from requirement records.
    /// </summary>
    [Fact]
    public void GenerateResults_ProvideReqStreamEvidence()
    {
        // Assert: the xUnit v3 test context is present and usable for structured, traceable test output
        Assert.NotNull(Xunit.TestContext.Current);
        Xunit.TestContext.Current.TestOutputHelper?.WriteLine("GenerateResults_ProvideReqStreamEvidence executed under xUnit v3.");

        // Assert: the repository's build script actually configures xUnit's TRX logger and results directory,
        // which is what makes xUnit results consumable as ReqStream-linked evidence artifacts
        var buildScriptPath = FindRepositoryFile("build.ps1");
        var buildScriptContent = File.ReadAllText(buildScriptPath);
        Assert.Contains("--logger trx", buildScriptContent);
        Assert.Contains("--results-directory", buildScriptContent);
    }

    /// <summary>
    ///     Walks upward from the test assembly's output directory to find a named file at the repository root.
    /// </summary>
    /// <param name="fileName">Repository-root-relative file name to locate.</param>
    /// <returns>Absolute path to the located file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file cannot be located within a reasonable number of parent directories.</exception>
    private static string FindRepositoryFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{fileName}' above {AppContext.BaseDirectory}.");
    }
}
