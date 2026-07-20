using System.Linq;
using System.Reflection;
using DemaConsulting.SysML2Workbench.AppShellSubsystem;

namespace DemaConsulting.SysML2Workbench.Tests.AppShellSubsystem;

/// <summary>
///     Unit tests for <see cref="AboutDialogViewModel" />.
/// </summary>
public sealed class AboutDialogViewModelTests
{
    /// <summary>
    ///     Validates that a freshly constructed view model exposes the fixed application name and tagline.
    /// </summary>
    [Fact]
    public void Construction_ExposesApplicationNameAndTagline()
    {
        // Arrange / Act
        var viewModel = new AboutDialogViewModel();

        // Assert
        Assert.Equal("SysML2Workbench", viewModel.ApplicationName);
        Assert.Equal("Cross-platform desktop viewer and IDE for SysML v2 models", viewModel.Tagline);
    }

    /// <summary>
    ///     Validates that <see cref="AboutDialogViewModel.VersionText" /> is resolved from the running assembly
    ///     (either its <see cref="System.Reflection.AssemblyInformationalVersionAttribute" /> or, failing that,
    ///     its <see cref="System.Reflection.AssemblyName.Version" />) rather than being blank or a hard-coded
    ///     literal disconnected from the actual build.
    /// </summary>
    [Fact]
    public void Construction_ExposesVersionFromAssembly()
    {
        // Arrange / Act
        var viewModel = new AboutDialogViewModel();

        // Assert: non-empty, and matches what the executing assembly itself reports.
        Assert.False(string.IsNullOrWhiteSpace(viewModel.VersionText));

        var assembly = typeof(AboutDialogViewModel).Assembly;
        var expected = assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .Cast<AssemblyInformationalVersionAttribute>()
            .Select(attribute => attribute.InformationalVersion)
            .FirstOrDefault(version => !string.IsNullOrWhiteSpace(version))
            ?? assembly.GetName().Version?.ToString()
            ?? "Unknown";

        Assert.Equal(expected, viewModel.VersionText);
    }

    /// <summary>
    ///     Validates that a freshly constructed view model exposes the application's copyright text, matching the
    ///     format used by the repository's own <c>LICENSE</c> file.
    /// </summary>
    [Fact]
    public void Construction_ExposesCopyrightText()
    {
        // Arrange / Act
        var viewModel = new AboutDialogViewModel();

        // Assert
        Assert.Equal("Copyright (c) 2026 DEMA Consulting", viewModel.Copyright);
    }

    /// <summary>
    ///     Validates that the dependency list contains the application's key OSS dependencies, each paired with
    ///     its license type.
    /// </summary>
    [Fact]
    public void Construction_ExposesDependencyList_ContainsExpectedEntries()
    {
        // Arrange / Act
        var viewModel = new AboutDialogViewModel();

        // Assert
        Assert.Contains(viewModel.Dependencies, dependency => dependency is { Name: "Avalonia", License: "MIT License" });
        Assert.Contains(viewModel.Dependencies, dependency => dependency is { Name: "Dock.Avalonia", License: "MIT License" });
        Assert.Contains(viewModel.Dependencies, dependency => dependency is { Name: "Material.Icons.Avalonia", License: "MIT License" });
        Assert.Contains(viewModel.Dependencies, dependency => dependency is { Name: "CommunityToolkit.Mvvm", License: "MIT License" });
        Assert.Contains(viewModel.Dependencies, dependency => dependency is { Name: "DemaConsulting.SysML2Tools", License: "MIT License" });
        Assert.Contains(viewModel.Dependencies, dependency => dependency is { Name: "DemaConsulting.Rendering", License: "MIT License" });
    }
}
