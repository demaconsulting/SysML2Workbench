# build.ps1
#
# TODO: Update this script to build and test your solution.
# Replace SysML2Workbench with your actual solution/project name.

$buildError = $false

Write-Host "Restoring dependencies..."
dotnet restore
if ($LASTEXITCODE -ne 0) { $buildError = $true }

Write-Host "Building..."
dotnet build --no-restore --configuration Release
if ($LASTEXITCODE -ne 0) { $buildError = $true }

Write-Host "Running tests..."
# IntegrationTests' Appium-driven tests are excluded (Category!=Integration) because this script does
# not start an Appium server; that tier only runs in CI's dedicated appium-windows-integration-tests
# job (.github/workflows/build.yaml), which does. See docs/design/introduction.md for the full
# three-(plus-OTS)-tier test strategy.
dotnet test --no-build --configuration Release --logger trx --results-directory artifacts/tests --filter "Category!=Integration"
if ($LASTEXITCODE -ne 0) { $buildError = $true }

# [PROJECT-SPECIFIC] Add additional build steps here (e.g., packaging, publishing).

exit ($buildError ? 1 : 0)
