using System.Reflection;

namespace DemaConsulting.SysML2Workbench.AppShellSubsystem;

/// <summary>
///     One open-source dependency entry shown in the About dialog's dependency list.
/// </summary>
/// <param name="Name">Package or project name.</param>
/// <param name="License">License type shown alongside <paramref name="Name" />.</param>
public sealed record DependencyInfo(string Name, string License);

/// <summary>
///     View model backing the About dialog (<see cref="AboutDialogView" />). Exposes the application's identity,
///     the running assembly's build-stamped version, copyright text, and a read-only list of the application's
///     key OSS dependencies. Holds no Avalonia dependency, so it can be constructed and asserted against directly
///     in unit tests without a UI thread, matching the Avalonia-free view model convention used elsewhere in this
///     subsystem (for example <see cref="WorkspacePanelToolViewModel" />).
/// </summary>
public sealed class AboutDialogViewModel
{
    private const string UnknownVersion = "Unknown";

    /// <summary>
    ///     License name shared by every entry in <see cref="Dependencies" />; every key OSS dependency this
    ///     application bundles happens to use the same license, so this avoids repeating the literal string.
    /// </summary>
    private const string MitLicense = "MIT License";

    /// <summary>
    ///     The application's display name.
    /// </summary>
    public static string ApplicationName => "SysML2Workbench";

    /// <summary>
    ///     Short description of what the application does.
    /// </summary>
    public static string Tagline => "Cross-platform desktop viewer and IDE for SysML v2 models";

    /// <summary>
    ///     Application copyright text, matching the format used by the repository's own <c>LICENSE</c> file.
    /// </summary>
    public static string Copyright => "Copyright (c) 2026 DEMA Consulting";

    /// <summary>
    ///     The running assembly's build-stamped version, read at runtime so it always reflects the actual build
    ///     rather than a hard-coded string that could drift from CI's <c>--property:Version=...</c> stamp. Prefers
    ///     <see cref="AssemblyInformationalVersionAttribute" /> (the full version string, including any
    ///     pre-release/build-metadata suffix); falls back to the assembly's normalized four-part
    ///     <see cref="AssemblyName.Version" /> when that attribute is absent (for example an un-stamped local
    ///     build), and finally to <see cref="UnknownVersion" /> if neither is available.
    /// </summary>
    public string VersionText { get; }

    /// <summary>
    ///     The application's key OSS dependencies, each with its name and license type, shown so users and
    ///     reviewers have visible attribution of the libraries the application is built on.
    /// </summary>
    public IReadOnlyList<DependencyInfo> Dependencies { get; } =
    [
        new DependencyInfo("Avalonia", MitLicense),
        new DependencyInfo("Dock.Avalonia", MitLicense),
        new DependencyInfo("Material.Icons.Avalonia", MitLicense),
        new DependencyInfo("CommunityToolkit.Mvvm", MitLicense),
        new DependencyInfo("DemaConsulting.SysML2Tools", MitLicense),
        new DependencyInfo("DemaConsulting.Rendering", MitLicense),
    ];

    /// <summary>
    ///     Creates the About dialog view model, resolving <see cref="VersionText" /> from the executing assembly.
    /// </summary>
    public AboutDialogViewModel()
    {
        VersionText = ResolveVersionText();
    }

    /// <summary>
    ///     Resolves the running assembly's build-stamped version text.
    /// </summary>
    /// <returns>
    ///     The assembly's <see cref="AssemblyInformationalVersionAttribute" /> value if present and non-empty;
    ///     otherwise its normalized <see cref="AssemblyName.Version" />; otherwise <see cref="UnknownVersion" />.
    /// </returns>
    private static string ResolveVersionText()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return assembly.GetName().Version?.ToString() ?? UnknownVersion;
    }
}
