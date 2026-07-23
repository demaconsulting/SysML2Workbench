using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using SkiaSharp;

namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Captures full-window or element-cropped PNG screenshots from a live Appium session and saves them
///     under <c>artifacts/inspection/</c> for later manual/agent-driven visual review, since automatically
///     detecting styling defects (contrast, color, spacing) is an LLM-visual-inspection problem that does
///     not belong in a pass/fail CI gate.
/// </summary>
/// <remarks>
///     <c>AppiumDriver.GetScreenshot()</c> is part of the base WebDriver protocol, so this works
///     identically across every <see cref="AppFixture" /> backend (NovaWindows, Mac2, AT-SPI2) with no
///     backend-specific code. Cropping uses SkiaSharp (MIT-licensed, and already Avalonia's own rendering
///     backend, so no new licensing concern is introduced) rather than <c>System.Drawing.Common</c>, which
///     is Windows-only from .NET 6 onward and would not work on this repository's Linux/macOS Appium
///     branches.
/// </remarks>
public static class InspectionScreenshot
{
    /// <summary>
    ///     Resolves the <c>inspection</c> subfolder under wherever inspection screenshots should land: the
    ///     repo-root-relative <c>artifacts</c> directory that <c>build.ps1</c>'s wrapped
    ///     <c>dotnet test .../--results-directory artifacts/tests</c> invocation targets, or a same-repo
    ///     <c>artifacts</c> folder when run directly via a bare <c>dotnet test</c> from the repository root.
    /// </summary>
    /// <remarks>
    ///     <c>dotnet test</c> runs the test host with its current directory set to the test assembly's own
    ///     <c>bin/&lt;configuration&gt;/&lt;tfm&gt;</c> output folder, not wherever <c>dotnet test</c> itself
    ///     was invoked from - unlike <c>--results-directory</c>, which vstest resolves relative to the
    ///     invocation directory, not the test host's. A plain relative <c>"artifacts"</c> path here would
    ///     therefore silently land inside that output folder instead of the repo-root <c>artifacts/</c>
    ///     directory that <c>.github/workflows/build.yaml</c>'s <c>appium-windows-integration-tests</c> job
    ///     already uploads wholesale - so <c>build.ps1</c> sets <c>SYSML2WORKBENCH_ARTIFACTS_DIR</c> to an
    ///     absolute path (mirroring the existing <c>SYSML2WORKBENCH_APP_PATH</c> convention in
    ///     <see cref="AppFixture" />) before invoking the wrapped <c>dotnet test</c>, so this lands in the same
    ///     place regardless of the test host's own working directory.
    /// </remarks>
    private static readonly string OutputDirectory = Path.Combine(
        Environment.GetEnvironmentVariable("SYSML2WORKBENCH_ARTIFACTS_DIR") ?? "artifacts",
        "inspection");

    /// <summary>
    ///     Captures the full application window and saves it as a PNG.
    /// </summary>
    /// <param name="driver">The live Appium session to capture from.</param>
    /// <param name="fileName">
    ///     Descriptive file name (without extension), e.g. <c>"main-window_dark_win"</c>. Should identify the
    ///     captured area, active theme, and platform so a reviewer can tell images apart without opening each
    ///     one.
    /// </param>
    public static void CaptureWindow(AppiumDriver driver, string fileName)
    {
        Save(driver.GetScreenshot().AsByteArray, fileName);
    }

    /// <summary>
    ///     Captures just the region covered by <paramref name="element" />, cropped from a full-window
    ///     screenshot using its on-screen bounds, and saves it as a PNG.
    /// </summary>
    /// <param name="driver">The live Appium session to capture from.</param>
    /// <param name="element">The element whose bounding rectangle should be cropped out.</param>
    /// <param name="fileName">See <see cref="CaptureWindow" />'s <c>fileName</c> parameter.</param>
    public static void CaptureElement(AppiumDriver driver, IWebElement element, string fileName)
    {
        using var fullImage = SKBitmap.Decode(driver.GetScreenshot().AsByteArray);

        var location = element.Location;
        var size = element.Size;

        // Clamp to the captured image's bounds: some backends report coordinates slightly outside the
        // screenshot (for example a partially off-screen tooltip anchor), which would otherwise throw.
        var x = Math.Max(0, location.X);
        var y = Math.Max(0, location.Y);
        var width = Math.Max(0, Math.Min(size.Width, fullImage.Width - x));
        var height = Math.Max(0, Math.Min(size.Height, fullImage.Height - y));

        using var cropped = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(cropped))
        {
            var sourceRect = SKRect.Create(x, y, width, height);
            var destRect = SKRect.Create(0, 0, width, height);
            canvas.DrawBitmap(fullImage, sourceRect, destRect, SKSamplingOptions.Default);
        }

        using var data = cropped.Encode(SKEncodedImageFormat.Png, quality: 100);
        Directory.CreateDirectory(OutputDirectory);

        // File.Create (unlike File.OpenWrite) truncates an existing file: without this, re-running the
        // test after a prior capture wrote a larger PNG would leave that PNG's trailing bytes appended
        // past the new (shorter) image data, corrupting the file.
        using var file = File.Create(Path.Combine(OutputDirectory, $"{fileName}.png"));
        data.SaveTo(file);
    }

    /// <summary>
    ///     Writes raw PNG bytes to <see cref="OutputDirectory" /> under <paramref name="fileName" />.
    /// </summary>
    private static void Save(byte[] pngBytes, string fileName)
    {
        Directory.CreateDirectory(OutputDirectory);
        File.WriteAllBytes(Path.Combine(OutputDirectory, $"{fileName}.png"), pngBytes);
    }
}
