# Toolchain Setup

SysML2Workbench is a cross-platform Avalonia 12 desktop application built on .NET

and the DemaConsulting.SysML2Tools and DemaConsulting.Rendering NuGet packages.

The exact pinned SDK version is not finalized yet, so use the repository's declared

version when a `global.json` is added. Until then, use a current stable .NET SDK

that is compatible with Avalonia 12 and this solution.

## Baseline Tools

- Install a current stable .NET SDK suitable for building modern Avalonia desktop applications.

- Install Git so the repository can be cloned, updated, and built from a local workspace.

- Install PowerShell 7 if it is not already available, because repository automation uses `pwsh`.

## Recommended IDEs

- Visual Studio 2022 with the .NET desktop development workload

- JetBrains Rider with .NET support enabled

- Visual Studio Code with the C# Dev Kit extension set

## Package and Restore Expectations

No special .NET workloads are currently expected beyond the standard SDK-based

desktop toolchain. Restoring the solution should retrieve the Avalonia,

DemaConsulting.SysML2Tools, and DemaConsulting.Rendering packages from the

configured NuGet feeds.

## Basic Validation

After installing the toolchain, validate the environment with these commands:

```pwsh

dotnet --info

pwsh ./build.ps1

```

If `build.ps1` completes successfully, the local machine has the minimum toolchain

needed for the current repository state.
