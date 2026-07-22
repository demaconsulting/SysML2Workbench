using System.Runtime.CompilerServices;

namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     An xUnit <see cref="FactAttribute" /> that behaves exactly like <see cref="FactAttribute" /> on Windows,
///     but is reported as skipped everywhere else. Applied to every real <see cref="AppFixture" />-driven test in
///     this assembly so that a developer or future CI runner executing on macOS/Linux (where no Appium
///     driver/session is provisioned - see <see cref="AppFixture" />'s remarks) sees an explicit, self-explaining
///     skip reason instead of a connection failure or an unhandled <see cref="PlatformNotSupportedException" />
///     thrown out of a shared collection fixture.
/// </summary>
public sealed class SkippableOnNonWindowsFactAttribute : FactAttribute
{
    /// <summary>
    ///     Creates the attribute, setting <see cref="FactAttribute.Skip" /> whenever the current platform is not
    ///     Windows. The caller-info parameters satisfy xUnit v3's analyzer requirement (xUnit3003) that every
    ///     <see cref="FactAttribute" /> subclass forward source information for accurate test-explorer reporting.
    /// </summary>
    /// <param name="sourceFilePath">Automatically supplied source file of the attribute usage.</param>
    /// <param name="sourceLineNumber">Automatically supplied source line of the attribute usage.</param>
    public SkippableOnNonWindowsFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Manual-only until macOS/Linux Appium CI is provisioned; only the Windows/NovaWindows path "
                + "is validated in CI today (see AppFixture's remarks).";
        }
    }
}
