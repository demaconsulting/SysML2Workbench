using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using DemaConsulting.SysML2Workbench;

[assembly: AvaloniaTestApplication(typeof(OtsSoftwareTests.TestAppBuilder))]

namespace OtsSoftwareTests;

/// <summary>
///     Builds the headless Avalonia application used by <see cref="AvaloniaFactAttribute" />-decorated tests in
///     <see cref="AvaloniaTests" />, so the real <see cref="App" /> composition root and control tree can be
///     exercised without a visible window or platform windowing system.
/// </summary>
public static class TestAppBuilder
{
    /// <summary>
    ///     Configures the headless Avalonia application instance shared by this assembly's Avalonia-backed tests.
    /// </summary>
    /// <returns>An <see cref="AppBuilder" /> configured for headless rendering.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
