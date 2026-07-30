using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using DemaConsulting.SysML2Workbench;

[assembly: AvaloniaTestApplication(typeof(DemaConsulting.SysML2Workbench.UiTests.TestAppBuilder))]

// Every [AvaloniaFact] in this assembly runs against ONE shared headless Avalonia application and its single
// UI thread/dispatcher. xUnit, however, runs distinct test classes as distinct collections in parallel by
// default, so two Avalonia tests can interleave on that one dispatcher: each test's
// Dispatcher.UIThread.RunJobs() then pumps the *other* test's queued UI work, and a test can observe control
// and Dock state settling at a point the other test never intended. That is a shared-state hazard of the
// headless platform, not of the code under test - it made
// MainWindowShellUiTests.MainWindowView_SourceTextDocumentTabSelected_StatusBarShowsFileSummary fail only when
// MainWindowShellUiTests and WorkspacePanelUiTests ran together (each class passed in isolation). Serializing
// the assembly is the supported remedy and costs nothing measurable: the whole suite runs in ~2 seconds.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
