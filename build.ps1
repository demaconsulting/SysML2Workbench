# build.ps1
#
# PURPOSE:
#   Builds the solution and/or runs its test tiers on demand, selected via
#   switch parameters so contributors and agents can run only the tier(s)
#   they need instead of always paying for a full restore+build+test+
#   integration-test cycle.
#
# USAGE:
#   ./build.ps1 [-Build] [-Test] [-IntegrationTest] [-All]
#   Run with no switches to print usage and exit non-zero.

param(
    [switch]$Build,
    [switch]$Test,
    [switch]$IntegrationTest,
    [switch]$All
)

function Show-Usage {
    Write-Host "Usage: build.ps1 [-Build] [-Test] [-IntegrationTest] [-All]"
    Write-Host "  -Build            Restore and build the solution (Release configuration)"
    Write-Host "  -Test             Run the unit/headless test suite (excludes Appium integration tests)"
    Write-Host "  -IntegrationTest  Publish the Desktop app and run the Appium/AT-SPI integration tests (Windows/macOS/Linux)"
    Write-Host "  -All              Equivalent to -Build -Test -IntegrationTest"
    Write-Host "Multiple switches may be combined, e.g.: ./build.ps1 -Build -Test"
}

if ($All) { $Build = $true; $Test = $true; $IntegrationTest = $true }

if (-not ($Build -or $Test -or $IntegrationTest)) {
    Show-Usage
    exit 1
}

$buildError = $false
$solutionBuilt = $false   # tracks whether restore+build already ran this invocation

function Invoke-SolutionBuild {
    # Shared by -Build, -Test, and -IntegrationTest so each switch is usable
    # standalone from a clean checkout without requiring -Build in the same
    # invocation; only runs once per script invocation.
    if ($script:solutionBuilt) { return $true }

    Write-Host "Restoring dependencies..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) { return $false }

    Write-Host "Building..."
    dotnet build --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { return $false }

    $script:solutionBuilt = $true
    return $true
}

if ($Build) {
    if (-not (Invoke-SolutionBuild)) { $buildError = $true }

    # [PROJECT-SPECIFIC] Add additional build steps here (e.g., packaging, publishing).
}

if ($Test) {
    if (-not (Invoke-SolutionBuild)) {
        $buildError = $true
    } else {
        Write-Host "Running tests..."
        # IntegrationTests' Appium-driven tests are excluded (Category!=Integration) because this
        # step does not start an Appium server; that tier runs via -IntegrationTest below (locally)
        # or CI's dedicated appium-windows-integration-tests job. See docs/design/introduction.md
        # for the full three-(plus-OTS)-tier test strategy.
        dotnet test --no-build --configuration Release --logger trx --results-directory artifacts/tests --filter "Category!=Integration"
        if ($LASTEXITCODE -ne 0) { $buildError = $true }
    }
}

if ($IntegrationTest) {
    if (-not (Invoke-SolutionBuild)) {
        $buildError = $true
    } else {
        if ($IsWindows -or $IsMacOS) {
            Write-Host "Installing Appium..."
            npm install -g appium
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
                ./run-under-appium.ps1 -- dotnet test test/DemaConsulting.SysML2Workbench.IntegrationTests/DemaConsulting.SysML2Workbench.IntegrationTests.csproj --configuration Release --logger "trx;LogFilePrefix=appium-integration" --results-directory artifacts/tests
                if ($LASTEXITCODE -ne 0) { $buildError = $true }
                Remove-Item Env:\SYSML2WORKBENCH_APP_PATH -ErrorAction SilentlyContinue
            }
        }
    }
}

exit ($buildError ? 1 : 0)
