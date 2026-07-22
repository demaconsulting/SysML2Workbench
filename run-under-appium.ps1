# run-under-appium.ps1
#
# PURPOSE:
#   Wraps an arbitrary command (typically `dotnet test` against
#   DemaConsulting.SysML2Workbench.IntegrationTests) with whatever local
#   automation server AppFixture.cs expects to already be listening at
#   http://127.0.0.1:4723 before the wrapped command's process starts:
#     - Windows/macOS: starts a local Appium server (NovaWindows/Mac2 driver
#       is selected inside AppFixture.cs, not here), runs the wrapped
#       command, then stops the server - regardless of whether the wrapped
#       command succeeded, failed, or threw.
#     - Linux: has no directly-startable server binary to manage. KDE's
#       selenium-webdriver-at-spi only supports its own `-run` wrapper,
#       which itself wraps the whole command (boots a nested Wayland
#       session, starts its Flask/AT-SPI2 server, runs the wrapped command
#       as its child, tears everything down together). This script simply
#       delegates to that wrapper instead of managing anything itself.
#
#   AppFixture.cs never starts/stops a server itself on any OS - it always
#   just connects to http://127.0.0.1:4723, assuming this script (or a
#   developer running the equivalent steps by hand) already made one
#   available.
#
# USAGE:
#   ./run-under-appium.ps1 -- <command> [args...]
#   e.g.: ./run-under-appium.ps1 -- dotnet test test/.../DemaConsulting.SysML2Workbench.IntegrationTests.csproj
#
#   The exit code of the wrapped command is propagated as this script's
#   exit code.

param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Command
)

if (-not $Command -or $Command.Count -eq 0) {
    Write-Error "Usage: run-under-appium.ps1 -- <command> [args...]"
    exit 1
}

$commandExe = $Command[0]
$commandArgs = if ($Command.Count -gt 1) { $Command[1..($Command.Count - 1)] } else { @() }

if ($IsLinux) {
    $atSpiRun = Get-Command selenium-webdriver-at-spi-run -ErrorAction SilentlyContinue
    if (-not $atSpiRun) {
        Write-Error "selenium-webdriver-at-spi-run was not found on PATH. Build and install it from https://invent.kde.org/sdk/selenium-webdriver-at-spi (see docs/design/ots/appium.md), then re-run."
        exit 1
    }

    # selenium-webdriver-at-spi-run boots its own nested Wayland session and
    # AT-SPI2/WebDriver server, runs the wrapped command as its child, and
    # tears everything down together - so it owns the entire lifecycle here;
    # this script does not poll/start/stop anything itself on Linux.
    Write-Host "Running under selenium-webdriver-at-spi-run: $commandExe $($commandArgs -join ' ')"
    & selenium-webdriver-at-spi-run $commandExe @commandArgs
    exit $LASTEXITCODE
}

# Windows/macOS: this script owns the Appium server's lifecycle itself via
# AppiumServiceBuilder-equivalent plain node/appium spawning, since
# AppFixture.cs never starts one.
$appiumProcess = $null
$exitCode = 1
try {
    if ($IsWindows) {
        # appium is installed as appium.cmd on Windows, which Start-Process
        # cannot exec directly (no shebang support); going through cmd.exe
        # would break PID tracking for the Stop-Process call below. Resolve
        # and launch the real JS entry point via node.exe instead, bypassing
        # the .cmd wrapper entirely.
        $appiumCmd = Get-Command appium.cmd -ErrorAction Stop
        $appiumEntry = Join-Path (Split-Path $appiumCmd.Source -Parent) "node_modules\appium\index.js"
        if (-not (Test-Path $appiumEntry)) {
            throw "Could not locate the Appium entry point at '$appiumEntry'."
        }

        Write-Host "Starting Appium server..."
        $appiumProcess = Start-Process -FilePath "node" -ArgumentList $appiumEntry, "--base-path", "/", "--allow-insecure", "*:chromedriver_autodownload" -WindowStyle Hidden -PassThru
    } else {
        # macOS: appium is a real shebang script on PATH, so it can be
        # exec'd directly - no .cmd/index.js resolution dance needed.
        Write-Host "Starting Appium server..."
        $appiumProcess = Start-Process -FilePath "appium" -ArgumentList "--base-path", "/", "--allow-insecure", "*:chromedriver_autodownload" -PassThru
    }

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
        exit 1
    }

    Write-Host "Running: $commandExe $($commandArgs -join ' ')"
    & $commandExe @commandArgs
    $exitCode = $LASTEXITCODE
} finally {
    if ($appiumProcess -and -not $appiumProcess.HasExited) {
        Write-Host "Stopping Appium server..."
        Stop-Process -Id $appiumProcess.Id -Force -ErrorAction SilentlyContinue
    }
}

exit $exitCode
