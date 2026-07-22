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
    Write-Host "  -IntegrationTest  Publish the Desktop app and run the Windows Appium/NovaWindows integration tests (Windows only)"
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
    if (-not $IsWindows) {
        Write-Error "IntegrationTests' Appium/NovaWindows session is Windows-only today; skipping -IntegrationTest on this OS. See docs/design/ots/appium.md."
        $buildError = $true
    } elseif (-not (Invoke-SolutionBuild)) {
        $buildError = $true
    } else {
        Write-Host "Installing Appium and the NovaWindows driver..."
        npm install -g appium
        if ($LASTEXITCODE -ne 0) { $buildError = $true }

        $driverInstallOutput = appium driver install --source=npm appium-novawindows-driver 2>&1 | Out-String
        Write-Host $driverInstallOutput
        if ($LASTEXITCODE -ne 0 -and $driverInstallOutput -notmatch "(?i)already installed") {
            $buildError = $true
        }

        Write-Host "Publishing self-contained Desktop build (win-x64)..."
        dotnet publish src/DemaConsulting.SysML2Workbench.Desktop/DemaConsulting.SysML2Workbench.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --property:PublishSingleFile=true --output publish/win-x64
        if ($LASTEXITCODE -ne 0) { $buildError = $true }

        # Resolve the Appium JS entry point and launch it via node.exe directly,
        # bypassing the appium.cmd/cmd.exe wrapper. This avoids the bare-binary-name
        # "%1 is not a valid Win32 application" crash (Start-Process cannot execute a
        # .cmd file without going through cmd.exe) and ensures -PassThru returns the
        # real Appium server process (not a wrapper), so Stop-Process below reliably
        # terminates the actual server with no orphaned processes.
        $appiumProcess = $null
        try {
            $appiumCmd = Get-Command appium.cmd -ErrorAction Stop
            $appiumEntry = Join-Path (Split-Path $appiumCmd.Source -Parent) "node_modules\appium\index.js"
            if (-not (Test-Path $appiumEntry)) {
                throw "Could not locate the Appium entry point at '$appiumEntry'."
            }

            Write-Host "Starting Appium server..."
            $appiumProcess = Start-Process -FilePath "node" -ArgumentList $appiumEntry, "--base-path", "/", "--allow-insecure", "*:chromedriver_autodownload" -WindowStyle Hidden -PassThru
        } catch {
            Write-Error "Failed to start the Appium server: $_"
            $buildError = $true
        }

        if ($appiumProcess) {
            try {
                $ready = $false
                for ($i = 0; $i -lt 30; $i++) {
                    try {
                        $response = Invoke-WebRequest -Uri "http://127.0.0.1:4723/status" -UseBasicParsing -TimeoutSec 2
                        if ($response.StatusCode -eq 200) { $ready = $true; break }
                    } catch {
                        Start-Sleep -Seconds 2
                    }
                }

                if (-not $ready) {
                    Write-Error "Appium server did not become ready within the expected time."
                    $buildError = $true
                } else {
                    Write-Host "Running Appium IntegrationTests..."
                    $env:SYSML2WORKBENCH_APP_PATH = Join-Path (Get-Location) "publish/win-x64/DemaConsulting.SysML2Workbench.Desktop.exe"
                    dotnet test test/DemaConsulting.SysML2Workbench.IntegrationTests/DemaConsulting.SysML2Workbench.IntegrationTests.csproj --configuration Release --logger "trx;LogFilePrefix=appium-windows" --results-directory artifacts/tests
                    if ($LASTEXITCODE -ne 0) { $buildError = $true }
                    Remove-Item Env:\SYSML2WORKBENCH_APP_PATH -ErrorAction SilentlyContinue
                }
            } finally {
                Write-Host "Stopping Appium server..."
                if (-not $appiumProcess.HasExited) {
                    Stop-Process -Id $appiumProcess.Id -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }
}

exit ($buildError ? 1 : 0)
