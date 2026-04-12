![Argus Health](argus-health.png)

## Introduction

Argus Health is a Background service that executes GET requests on configurable http endpoints to verify the health of an application, typically a REST API.

## Code Quality

> More info coming soon

## Building the Installer

The MSI installer packages both the Argus Health Service and Argus Health Pulse into a single installer. It requires PowerShell and the .NET SDK.

```powershell
pwsh .\build-installer.ps1
```

The script publishes both projects as self-contained win-x64 applications and builds the WiX v5 installer. The output MSI is located at:

```
Argus.Health.Installer\bin\Release\Argus.Health.Installer.msi
```

## Releasing a New Version

Use the version bump script to update all project files in one step:

```powershell
pwsh .\bump-version.ps1 -Version 0.2.0
```

This updates the `<Version>` element in both `.csproj` files and the `Version` attribute in `Package.wxs`. Then:

```powershell
git diff                                              # review changes
git add -A && git commit -m "[Release] v0.2.0"        # commit
git tag v0.2.0                                        # tag
pwsh .\build-installer.ps1                            # build the MSI
```

## Build Status

GitHub actions are used to build and test the Argus Health application

Branch | Build Status
------- | :------------
Master | ![Build Status](https://github.com/samgerene/Argus.Health/actions/workflows/CodeQuality.yml/badge.svg?branch=master)
Development | ![Build Status](https://github.com/samgerene/Argus.Health/actions/workflows/CodeQuality.yml/badge.svg?branch=development)
