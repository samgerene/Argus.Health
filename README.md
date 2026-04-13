![Argus Health](argus-health.png)

## Introduction

Argus Health is a combination of a 
  - Health.Service: a background service that executes GET requests on configurable HTTP endpoints to verify the health of an application, typically a REST API.
  - Argus.Pulse: a desktop app that visualizes the health of EndPoints and that provides funnctionality to create, update and delete endpoints.

## Code Quality

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=coverage)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=samgerene_Argus.Health&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=samgerene_Argus.Health)

## Build Status

GitHub actions are used to build and test the Argus Health application

Branch | Build Status
------- | :------------
Master | ![Build Status](https://github.com/samgerene/Argus.Health/actions/workflows/CodeQuality.yml/badge.svg?branch=master)
Development | ![Build Status](https://github.com/samgerene/Argus.Health/actions/workflows/CodeQuality.yml/badge.svg?branch=development)

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
