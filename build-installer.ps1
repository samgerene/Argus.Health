# Builds the Argus Health MSI installer.
# Publishes both Service and Pulse as self-contained win-x64,
# then builds the WiX v5 project.

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot

Write-Host "Publishing Argus.Health.Service..." -ForegroundColor Cyan
dotnet publish "$root\Argus.Health.Service\Argus.Health.Service.csproj" `
    -c Release -r win-x64 --self-contained true `
    -o "$root\Argus.Health.Service\bin\publish"
if ($LASTEXITCODE -ne 0) { throw "Service publish failed" }

Write-Host "Publishing Argus.Health.Pulse..." -ForegroundColor Cyan
dotnet publish "$root\Argus.Health.Pulse\Argus.Health.Pulse.csproj" `
    -c Release -r win-x64 --self-contained true `
    -o "$root\Argus.Health.Pulse\bin\publish"
if ($LASTEXITCODE -ne 0) { throw "Pulse publish failed" }

Write-Host "Preparing Service EXE for WiX component..." -ForegroundColor Cyan
$serviceExeDir = "$root\Argus.Health.Installer\ServiceExe"
New-Item -ItemType Directory -Force -Path $serviceExeDir | Out-Null
Move-Item -Force `
    "$root\Argus.Health.Service\bin\publish\Argus.Health.Service.exe" `
    "$serviceExeDir\Argus.Health.Service.exe"

Write-Host "Building MSI installer..." -ForegroundColor Cyan
dotnet build "$root\Argus.Health.Installer\Argus.Health.Installer.wixproj" -c Release
if ($LASTEXITCODE -ne 0) { throw "Installer build failed" }

$msi = "$root\Argus.Health.Installer\bin\Release\Argus.Health.Installer.msi"
Write-Host "Done: $msi" -ForegroundColor Green
