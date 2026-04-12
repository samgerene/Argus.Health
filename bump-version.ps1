param(
    [Parameter(Mandatory)]
    [string]$Version
)

# Validate semver format
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Version must be in format X.Y.Z (e.g. 1.0.0)"
    exit 1
}

$root = $PSScriptRoot

# Files to update
$files = @(
    "$root\Argus.Health.Service\Argus.Health.Service.csproj",
    "$root\Argus.Health.Pulse\Argus.Health.Pulse.csproj",
    "$root\Argus.Health.Installer\Package.wxs"
)

foreach ($file in $files) {
    if (-not (Test-Path $file)) {
        Write-Warning "File not found: $file"
        continue
    }

    $content = Get-Content $file -Raw

    if ($file -like '*.csproj') {
        $content = $content -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
    }
    elseif ($file -like '*.wxs') {
        $content = $content -replace 'Version="[^"]+"', "Version=`"$Version`""
    }

    Set-Content $file $content -NoNewline
    Write-Host "Updated $file -> $Version" -ForegroundColor Green
}

Write-Host "`nDone. Next steps:" -ForegroundColor Cyan
Write-Host "  git diff"
Write-Host "  git add -A && git commit -m '[Release] v$Version'"
Write-Host "  git tag v$Version"
Write-Host "  pwsh .\build-installer.ps1"
