using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using DemaConsulting.SysML2Workbench;

[assembly: AvaloniaTestApplication(typeof(DemaConsulting.SysML2Workbench.UiTests.TestAppBuilder))]

namespace DemaConsulting.SysML2Workbench.UiTests;

/// <summary>
///     Builds the headless Avalonia application used by <see cref="AvaloniaFactAttribute" />-decorated tests in
///     this assembly, so real views and view models can be exercised without a visible window or platform
///     windowing system. This mirrors <c>test/OtsSoftwareTests/TestAppBuilder.cs</c>, but is this assembly's own
///     copy - each headless-Avalonia test assembly needs its own <see cref="AvaloniaTestApplicationAttribute" />
///     target, since the attribute is assembly-scoped.
/// </summary>
public static class TestAppBuilder
{
    /// <summary>
    ///     Configures the headless Avalonia application instance shared by this assembly's Avalonia-backed tests.
    /// </summary>
    /// <returns>An <see cref="AppBuilder" /> configured for headless rendering.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
