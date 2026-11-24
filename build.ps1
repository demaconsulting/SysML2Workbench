# build.ps1
#
# PURPOSE:
#   Builds the solution in Release configuration and runs the headless unit tests, either all of them
#   (-All, the full local build and test run) or only the test projects a change affects
#   (-Changed / -Project). The Appium/AT-SPI integration tier drives the real desktop, so it is an opt-in
#   switch (-IntegrationTest) that -All does not include.
#
# USAGE:
#   pwsh ./build.ps1 -Help                     Print the usage text and exit 0
#   pwsh ./build.ps1 -All                      Full local build and all headless tests (no integration tier)
#   pwsh ./build.ps1 -Changed [-Base <ref>]    Only the test projects affected by the changes since <ref>
#                                              (default origin/main, else main) plus uncommitted changes
#   pwsh ./build.ps1 -Project UiTests          Only the test projects affected by the named projects (full or short
#                                              names: UiTests finds DemaConsulting.SysML2Workbench.UiTests, Core is the
#                                              core library, aliases from $ProjectAliases also work; -Help lists this
#                                              repository's names)
#   pwsh ./build.ps1 -IntegrationTest          Opt in to the Appium/AT-SPI integration tier: publish the Desktop app
#                                              and drive it through a real desktop session (alone, or added to any
#                                              scope above, e.g. -All -IntegrationTest for everything)
#   Add -DryRun to any scope to print the selection without building anything.
#   With no arguments the usage text is printed and the script exits 1 (a scope must be chosen).
#
#   A changed project selects every project that references it, directly or transitively, from the
#   ProjectReference graph, and files linked into another project (any item or file property that
#   points outside the project folder) select that project too. Changes that affect the whole
#   build (solution, project, props/targets, global.json, .editorconfig, nuget.config, tool manifest,
#   this script, scripts/ProjectScripts.psm1) fall back to the full build; repository files tests read
#   at run time select their test projects through $RuntimeReads; other changes the build does not
#   read select nothing. This works unchanged for a solution with one project pair or many.
#
#   The test platform is detected from global.json: "test": { "runner": "Microsoft.Testing.Platform" } runs
#   'dotnet test --project <path> --report-trx', anything else runs VSTest 'dotnet test <path> --logger trx';
#   results go to artifacts/tests either way. $TestArgsVSTest / $TestArgsTestingPlatform add the repository's
#   Category!=Integration exclusion on whichever platform runs (this repository has no global.json, so VSTest).
#
# EXTENSION POINTS:
#   Search for "[PROJECT-SPECIFIC]" comments to find the designated locations
#   for adding project-specific build or test operations.
#
# CUSTOMIZATION:
#   This script belongs to the project: extend it as the project grows. Add steps at the
#   [PROJECT-SPECIFIC] points and add optional parameters that focus a run. The reusable scoping
#   logic lives in scripts/ProjectScripts.psm1, which is delivered by the Agents release zip, is
#   shared across repositories, and must never be edited here. Slow or environment-dependent test
#   tiers (GUI automation, integration) may be opt-in switches.
#   With no arguments the script prints the usage text and exits 1; -All must still build and run
#   the standard test suite (agents rely on that), and never remove or weaken a test step to make
#   it pass.

param(
    # Full local build and all headless tests.
    [switch]$All,

    # Build and test only the projects affected by the changed files.
    [switch]$Changed,

    # Git reference to compare against for -Changed (default: origin/main, else main).
    [string]$Base,

    # Build and test only the projects affected by these projects (full or short names, e.g. Core, UiTests).
    [string[]]$Project,

    # [PROJECT-SPECIFIC] Opt in to the Appium/AT-SPI integration tier (publishes the Desktop app and drives it
    # through a real desktop session). Alone it is a scope; with -All, -Changed, or -Project it is added to that run.
    [switch]$IntegrationTest,

    # Print the selection and exit without building.
    [switch]$DryRun,

    # Print the usage text and exit 0.
    [switch]$Help
)

Import-Module (Join-Path $PSScriptRoot 'scripts/ProjectScripts.psm1') -Force

# [PROJECT-SPECIFIC] Repository files that tests read at run time (through the repository root, not through a
# project item), mapped to the test project that reads them. Keep in step with the tests. The headless tests
# build their own temporary workspaces and read no repository files. Example:
#   '^docs/user_guide/tool\.md$' = 'SystemName.Tests'
$RuntimeReads = [ordered]@{}

# [PROJECT-SPECIFIC] Short -Project names that no resolution rule can derive (exact name, unique dot-segment suffix,
# Core), as data: alias = project name. Empty by default. Example:
#   $ProjectAliases = @{ Ots = 'OtsSoftwareTests'; App = 'DemaConsulting.Foo' }
$ProjectAliases = @{}

# [PROJECT-SPECIFIC] Folders scanned for projects. List every root once here when projects live outside src/, test/,
# and tools/ (for example demo, samples, benchmarks); leave out folders of fixture projects that are test inputs.
$ProjectRoots = @('src', 'test', 'tools')

# Test platform: global.json selects Microsoft.Testing.Platform with "test": { "runner": "Microsoft.Testing.Platform" };
# otherwise VSTest is used. The platform decides the 'dotnet test' arguments (see Invoke-DotnetTest).
$UseTestingPlatform = $false
if (Test-Path global.json) {
    try { $UseTestingPlatform = (Get-Content -Raw global.json | ConvertFrom-Json).test.runner -eq 'Microsoft.Testing.Platform' }
    catch { Write-Host "Warning: global.json could not be read; assuming VSTest." }
}

# [PROJECT-SPECIFIC] Extra 'dotnet test' arguments appended to every headless test run, one list per test platform
# (only the list for the detected platform is used). They keep the Appium tests (which need a running Appium server
# and the published app) out of every headless run; -IntegrationTest runs that tier. See docs/design/introduction.md
# for the full test strategy. Empty by default.
$TestArgsVSTest = @('--filter', 'Category!=Integration')
$TestArgsTestingPlatform = @('--filter-not-trait', 'Category=Integration')   # xUnit v3 on Microsoft.Testing.Platform

function Invoke-DotnetTest {
    # Runs the tests of one project, or of the whole solution when no path is given, against the existing build
    param([string]$Path)

    # @(...) keeps a one-element list an array, so the splat passes the path whole and not character by character
    if ($UseTestingPlatform) {
        $target = @(if ($Path) { '--project'; $Path })
        dotnet test @target --no-build --configuration Release --report-trx --results-directory artifacts/tests @TestArgsTestingPlatform
    } else {
        $target = @(if ($Path) { $Path })
        dotnet test @target --no-build --configuration Release --logger trx --results-directory artifacts/tests @TestArgsVSTest
    }
}

# ==============================================================================
# USAGE AND SCOPE VALIDATION
# ==============================================================================

function Show-Usage {
    Write-ScriptUsage -Script 'build.ps1' -IterateWith '-Changed (or -Project)' `
        -Purpose 'build the solution (Release) and run the headless unit tests.' `
        -Options ([ordered]@{
            '-All'             = 'Full local build and all headless tests; cannot be combined with another scope.'
            '-Changed'         = 'Only the test projects affected by files changed since the base, plus uncommitted files.'
            '-Base <ref>'      = 'Base reference for -Changed (default origin/main, else main).'
            '-Project <names>' = 'Only the test projects affected by these projects (full or short names, e.g. UiTests).'
            '-IntegrationTest' = 'Opt in: publish Desktop and run the Appium/AT-SPI tests (takes over the desktop; not in -All).'
            '-DryRun'          = 'Print what would be built, tested, and skipped, then exit 0 without running anything.'
            '-Help'            = 'Print this text and exit 0.'
        }) `
        -ChecksLabel 'Projects and aliases (-Project names; short names such as Core or UiTests also work)' `
        -Checks @(try { Get-ProjectNameList (Get-ProjectGraph -Root $ProjectRoots) $ProjectAliases } catch { }) `
        -Examples @('-Changed', '-Project Core', '-Changed -DryRun', '-All', '-All -IntegrationTest')
}

if ($Help) { Show-Usage; exit 0 }

# -IntegrationTest is a scope of its own only when no other scope is given; otherwise it is added to that scope
$unitScope = $All -or $Changed -or [bool]$Project
$scopeProblems = Test-ScopeArguments -Base $Base -Scopes ([ordered]@{
        All             = $All.IsPresent
        Changed         = $Changed.IsPresent
        Project         = [bool]$Project
        IntegrationTest = ($IntegrationTest.IsPresent -and -not $unitScope)
    })
if ($scopeProblems.Count -gt 0) {
    $scopeProblems | ForEach-Object { Write-Host "Error: $_" }
    Write-Host ""
    Show-Usage
    exit 1
}

$buildError = $false

# ==============================================================================
# SCOPED BUILD (-Changed / -Project)
# Selects the affected test projects; falls back to the full build when needed.
# ==============================================================================

$scoped = $false          # a scoped selection was made (built and tested, or only printed by -DryRun)
$solutionBuilt = $false   # the whole solution was restored and built in Release this run
if ($unitScope -and -not $All) {
    $graph = Get-ProjectGraph -Root $ProjectRoots
    $selected = [System.Collections.Generic.HashSet[string]]::new()
    $fullReason = $null

    $names = @($Project | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $known = (Get-ProjectNameList $graph $ProjectAliases) -join ', '
    if ($Project -and $names.Count -eq 0) {
        Write-Host "Error: no project named in '$($Project -join ' ')'. Known projects: $known"
        exit 1
    }
    foreach ($single in $names) {
        try { $resolved = Resolve-ProjectName $graph $single $ProjectAliases }
        catch { Write-Host "Error: $($_.Exception.Message)"; exit 1 }
        if (-not $resolved) {
            Write-Host "Unknown project '$single'. Known projects: $known"
            exit 1
        }
        [void]$selected.Add($resolved)
    }

    if ($Changed) {
        $changedFiles = Get-ChangedFiles -Base $Base
        if ($null -eq $changedFiles) {
            $fullReason = 'the changed files could not be determined'
        } else {
            Write-Host "Changed files: $($changedFiles.Count)"
            $fullReason = Add-ChangedProjects -Graph $graph -Files $changedFiles -Selected $selected -RuntimeReads $RuntimeReads
        }
    }

    if ($fullReason) {
        Write-Host "Full build required: $fullReason."
    } else {
        Expand-ReferencingProjects -Graph $graph -Selected $selected

        # Test projects are built and tested; other leaf projects (tools/) are built only
        $tests = @($graph.Values | Where-Object { $selected.Contains($_.Name) -and $_.Directory -like 'test/*' })
        $tools = @($graph.Values | Where-Object { $selected.Contains($_.Name) -and $_.Directory -like 'tools/*' })
        # [PROJECT-SPECIFIC] The Desktop platform head has no test project referencing it, so build it on its own
        $tools += @($graph.Values | Where-Object { $selected.Contains($_.Name) -and $_.Name -eq 'DemaConsulting.SysML2Workbench.Desktop' })
        $skipped = @($graph.Values | Where-Object { $_.Directory -like 'test/*' -and -not $selected.Contains($_.Name) })
        $scoped = $true
        if ($tests.Count -eq 0 -and $tools.Count -eq 0) {
            Write-Host "Nothing to build: no changed file is read by the build."
        } else {
            Write-Host "Affected test projects (built and tested):"
            $tests | ForEach-Object { Write-Host "  $($_.Name)" }
            if ($tools.Count -gt 0) {
                Write-Host "Affected projects without tests (build only):"
                $tools | ForEach-Object { Write-Host "  $($_.Name)" }
            }
            if ($DryRun) {
                Write-Host "Skipped test projects ($($skipped.Count)): $(($skipped | ForEach-Object { $_.Name }) -join ', ')"
            } else {
                foreach ($entry in $tests + $tools) {
                    Write-Host "Building $($entry.Name)..."
                    dotnet build $entry.Path --configuration Release
                    if ($LASTEXITCODE -ne 0) { $buildError = $true }
                }

                if (-not $buildError) {
                    foreach ($entry in $tests) {
                        Write-Host "Running tests: $($entry.Name)..."
                        Invoke-DotnetTest $entry.Path
                        if ($LASTEXITCODE -ne 0) { $buildError = $true }
                    }
                } else {
                    Write-Host "Skipping tests: the build failed."
                }
            }
        }
    }
}

# The full build and test run: -All, or a scoped run that needs everything
$fullBuild = $unitScope -and -not $scoped

if ($DryRun) {
    if ($fullBuild) { Write-Host "Dry run: would run the full build and all headless tests." }
    if ($IntegrationTest) {
        Write-Host "Dry run: would publish the Desktop app and run the Appium/AT-SPI integration tests (-IntegrationTest)."
    }
    Write-Host "Dry run: nothing was built or tested."
    exit 0
}

# ==============================================================================
# FULL BUILD (-All, or a scoped run that needs everything)
# ==============================================================================

if ($fullBuild) {
    # Each step runs only if the steps before it succeeded, so tests never run against stale binaries
    $failedStep = $null   # the step that failed first, so the skip message names the real cause
    Write-Host "Restoring dependencies..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) { $buildError = $true; $failedStep = 'restore' }

    if (-not $buildError) {
        Write-Host "Building..."
        dotnet build --no-restore --configuration Release
        if ($LASTEXITCODE -ne 0) { $buildError = $true; $failedStep = 'build' } else { $solutionBuilt = $true }
    }

    if (-not $buildError) {
        Write-Host "Running tests..."
        # The Appium tests are excluded (Category!=Integration, see Invoke-DotnetTest) because this step does not
        # start an Appium server; that tier runs via -IntegrationTest below (locally) or CI's dedicated
        # appium-windows-integration-tests job.
        Invoke-DotnetTest
        if ($LASTEXITCODE -ne 0) { $buildError = $true }
    } elseif ($failedStep -eq 'restore') {
        Write-Host "Skipping build and tests: restore failed."
    } else {
        Write-Host "Skipping tests: the build failed."
    }

    # [PROJECT-SPECIFIC] Add additional build steps here (e.g., packaging, publishing).
    # Guard steps that depend on the build with: if (-not $buildError) { ... }
}

# ==============================================================================
# [PROJECT-SPECIFIC] INTEGRATION TIER (-IntegrationTest, opt-in)
# Publishes the Desktop app and runs the Appium/AT-SPI IntegrationTests through run-under-appium.ps1. These tests
# drive a real desktop session, so they are never part of -All, -Changed, or -Project on their own.
# ==============================================================================

if ($IntegrationTest) {
    if ($buildError) {
        Write-Host "Skipping integration tests: the build failed."
    } else {
        if (-not $solutionBuilt) {
            Write-Host "Restoring dependencies..."
            dotnet restore
            if ($LASTEXITCODE -ne 0) { $buildError = $true }

            if (-not $buildError) {
                Write-Host "Building..."
                dotnet build --no-restore --configuration Release
                if ($LASTEXITCODE -ne 0) { $buildError = $true } else { $solutionBuilt = $true }
            }
        }

        if (-not $buildError) {
            if ($IsWindows -or $IsMacOS) {
                Write-Host "Installing Appium..."
                # Pinned to Appium 3.x until appium-novawindows-driver supports Appium 4 (its latest release,
                # 1.5.0, requires appium ^3); see docs/design/ots/appium.md, "Server version pin".
                npm install -g appium@3
                if ($LASTEXITCODE -ne 0) { $buildError = $true }

                $driverName = $IsWindows ? "appium-novawindows-driver" : "mac2"
                $driverArgs = $IsWindows ? @("--source=npm", $driverName) : @($driverName)

                Write-Host "Installing the $driverName driver..."
                $driverInstallOutput = appium driver install @driverArgs 2>&1 | Out-String
                Write-Host $driverInstallOutput
                if ($LASTEXITCODE -ne 0 -and $driverInstallOutput -notmatch "(?i)already installed") {
                    $buildError = $true
                }
            } else {
                Write-Host "Linux: not installing Appium/a driver - selenium-webdriver-at-spi must already be built and installed (see docs/design/ots/appium.md)."
            }

            if (-not $buildError) {
                $rid = $IsWindows ? "win-x64" : ($IsMacOS ? ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64 ? "osx-arm64" : "osx-x64") : "linux-x64")
                $exeName = $IsWindows ? "DemaConsulting.SysML2Workbench.Desktop.exe" : "DemaConsulting.SysML2Workbench.Desktop"

                Write-Host "Publishing self-contained Desktop build ($rid)..."
                dotnet publish src/DemaConsulting.SysML2Workbench.Desktop/DemaConsulting.SysML2Workbench.Desktop.csproj --configuration Release --runtime $rid --self-contained true --property:PublishSingleFile=true --output publish/$rid
                if ($LASTEXITCODE -ne 0) { $buildError = $true }

                if (-not $buildError) {
                    Write-Host "Running Appium IntegrationTests..."
                    $env:SYSML2WORKBENCH_APP_PATH = Join-Path (Get-Location) "publish/$rid/$exeName"
                    $env:SYSML2WORKBENCH_ARTIFACTS_DIR = Join-Path (Get-Location) "artifacts"
                    ./run-under-appium.ps1 -- dotnet test test/DemaConsulting.SysML2Workbench.IntegrationTests/DemaConsulting.SysML2Workbench.IntegrationTests.csproj --configuration Release --logger "trx;LogFilePrefix=appium-integration" --results-directory artifacts/tests
                    if ($LASTEXITCODE -ne 0) { $buildError = $true }
                    Remove-Item Env:\SYSML2WORKBENCH_APP_PATH -ErrorAction SilentlyContinue
                    Remove-Item Env:\SYSML2WORKBENCH_ARTIFACTS_DIR -ErrorAction SilentlyContinue
                }
            }
        }
    }
}

if (-not $buildError) {
    Write-Host "Build and tests completed successfully!"
}

exit ($buildError ? 1 : 0)
