# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Argus health combines a worker service and a desktop app as end-user fronted.

  - Argus.Health.Service is a .NET 10 Worker Service that monitors HTTP health endpoints by executing GET requests on configurable intervals and reporting their status. It runs as a Windows Service or systemd daemon on Linux.
  - Argus.Health.Pulse is an Avalonia based GUI using ReactiveUI.

## Commands

```bash
# Build
dotnet build Argus.Health.sln

# Run all tests
dotnet test Argus.Health.sln

# Run tests with coverage (as CI does)
dotnet test Argus.Health.sln --no-restore --no-build --verbosity normal /p:CollectCoverage=true /p:CoverletOutput="../CoverageResults/" /p:MergeWith="../CoverageResults/coverage.json" /p:CoverletOutputFormat="opencover,json"

# Run a single test by name filter
dotnet test Argus.Health.Service.Tests --filter "FullyQualifiedName~Verify_that_HealthEndPoint_can_be_read"

# Run the service locally
dotnet run --project Argus.Health.Service

# Run the Pulse desktop app
dotnet run --project Argus.Health.Pulse

# Build the Windows MSI installer (publishes Service + Pulse self-contained and builds the WiX project)
pwsh .\build-installer.ps1
```

## Solution Structure

| Project | Framework | Role |
|---|---|---|
| `Argus.Health.Common` | net10.0 | Shared models and domain JSON serialization |
| `Argus.Health.Service` | net10.0 | Worker Service — main executable |
| `Argus.Health.Service.Tests` | net10.0 | Tests for the service |
| `Argus.Health.Pulse` | net10.0-windows | Avalonia desktop dashboard for monitoring Argus Health endpoints |
| `Argus.Health.Pulse.Tests` | net10.0-windows | Tests for Argus.Health.Pulse |
| `Argus.Health.Installer` | WiX 5 | MSI installer packaging the Service and Pulse |

## Architecture

### Runtime Flow

`Program.cs` bootstraps a .NET generic `Host`:

1. **Serilog** is configured with a Console sink (from `appsettings.json`) and a rolling File sink added programmatically. The log file path is built at startup from `Program.ApplicationDataFolder + "\logs\argus-health-service-.log"` (rolling daily), so logs land next to the SQLite database regardless of the OS.
2. **OS-aware lifecycle registration**: `AddWindowsService(o => o.ServiceName = "Argus Health Service")` on Windows, `AddSystemd()` on Linux. Unsupported OSes exit with code 1.
3. **DI registration**: `ArgusHealthOptions` bound from the `ArgusHealth` config section, a named `HttpClient("ArgusHealth")`, `AddArgusModules()`, `AddArgusPipeHost()` (named-pipe IPC host), `IHealthEndPointRepository` + `IHealthEndPointCheckResultRepository` as singletons, and `HealthEndPointBackgroundService` as a hosted service. `HostOptions.ServicesStartConcurrently = true`.
4. **Database initialization**: after `builder.Build()`, `repository.InitializeDatabase()` runs synchronously before `app.RunAsync()`. It creates `ApplicationDataFolder` and the SQLite file/tables if missing.
5. **Host runs** until SCM / systemd / `Ctrl+C` stops it. When the host stops because a `BackgroundService` failed, `Program.ExitWhenBackgroundServiceFailed` (registered on `ApplicationStopping`) ends the process with exit code 1: the host would otherwise report a clean stop, and the recovery actions the MSI configures (`util:ServiceConfig` in `Package.wxs`: restart after 60 seconds) would not run. A failure during startup sets `Environment.ExitCode = 1`.

`HealthEndPointBackgroundService` (extends `BackgroundService`) loads all endpoints from the repository on startup and spawns a per-endpoint monitoring loop. It subscribes to repository events (`EndpointAdded`, `EndpointUpdated`, `EndpointRemoved`) to dynamically start/stop monitors without restart. Each monitor loop is tracked in a `ConcurrentDictionary<Guid, (Task, CancellationTokenSource)>`.

Each monitor loop wraps HTTP calls with three stacked Polly policies (innermost to outermost): **circuit breaker** → **timeout** → **retry**.

### Background Service File Paths

All persistent state is rooted under `Program.ApplicationDataFolder` (`Environment.SpecialFolder.LocalApplicationData` + `"ArgusHealthService"`). This ensures the service uses absolute, OS-appropriate, writable paths regardless of the process working directory (which is `C:\Windows\System32` when SCM starts a Windows Service).

| Artifact | Relative path under `ApplicationDataFolder` | Created by |
|---|---|---|
| SQLite database | `ArgusHealth.sqlite` | `HealthEndPointRepository.InitializeDatabase()` |
| Rolling log files | `logs\argus-health-service-YYYYMMDD.log` | `Program.Main` (Serilog File sink) |

Resolved paths per platform:

- **Windows (`NT AUTHORITY\LocalService`, the account the MSI installs the service with):** `C:\Windows\ServiceProfiles\LocalService\AppData\Local\ArgusHealthService\`. Version 1.0.0 ran as `LocalSystem` with its data in `C:\Windows\System32\config\systemprofile\AppData\Local\ArgusHealthService\`; the installer does not move it (see the README).
- **Linux (root/systemd):** `/root/.local/share/ArgusHealthService/` (or `$XDG_DATA_HOME/ArgusHealthService/` for a non-root service user)

Both the database folder and the logs folder are created via `Directory.CreateDirectory` on startup before any I/O is attempted. The Serilog File sink is added programmatically in `Program.cs` (not via `appsettings.json`) so the resolved absolute path can be injected.

### IPC Protocol

The pipe host (from the `ArgusTransfer` NuGet package — see `Argus.Health.Service.csproj`) listens on a configurable named pipe. The default name is `"ArgusHealth"` (see `ArgusHealthOptions.PipeName` and the `ArgusHealth:PipeName` config key in `appsettings.json`). The protocol uses `ArgusRequest` / `ArgusResponse` messages with HTTP-like routes (`/healthendpoint`, `/healthendpoint/{identifier}`) and verbs (`ArgusVerb`: GET, POST, PUT, PATCH, HEAD, DELETE). Requests are routed via `ArgusRouter` to registered `IArgusModule` implementations. The pipe host is registered via `AddArgusPipeHost()` in `Program.cs`, with response compression enabled; `AddArgusExceptionHandler()` turns an unhandled handler exception into a 500 problem details response.

Error responses of `HealthEndPointModule` are `ArgusProblemDetails` (`application/problem+json`) with a `detail` that explains the failure. Status codes: 400 for an empty or unreadable body, 422 for invalid values (empty name, url that is not absolute, frequency or timeout not greater than zero, negative retry count), 404 when the endpoint does not exist (`NotFoundError` from the repository on update and delete), 409 when the name or identifier is already in use (`ConflictError`), 500 for any other failure. `HealthEndPointClient` appends that detail to the message of the `InvalidOperationException` it throws (`Server returned {StatusCode}: {detail}`).

### Repository

`IHealthEndPointRepository` defines CRUD + three events. `HealthEndPointRepository` stores `HealthEndPoint` records in a SQLite database named `ArgusHealth.sqlite` inside `Program.ApplicationDataFolder`, which resolves to `Environment.SpecialFolder.LocalApplicationData` + `"ArgusHealthService"`. When the service runs as `NT AUTHORITY\LocalService` (the account of the WiX MSI install), that's `C:\Windows\ServiceProfiles\LocalService\AppData\Local\ArgusHealthService\ArgusHealth.sqlite`. On Linux it's the XDG equivalent (`~/.local/share/ArgusHealthService/`). The constructor has an `internal` overload accepting a custom folder path — used only in tests.

Rolling Serilog log files live alongside the database in the same folder tree: `{ApplicationDataFolder}\logs\argus-health-service-YYYYMMDD.log`. The `logs` folder is created on startup by `Program.Main` before the Serilog logger is built.

`IHealthEndPointCheckResultRepository` (backed by `HealthEndPointCheckResultRepository`) is a separate singleton repository for persisted check results. It shares the same database file.

The database column for URLs is named `Urls` and stores semicolon-separated values in the raw SQL, but the model property is currently `Url` (singular `string`).

### Pulse Desktop Dashboard

**App bootstrap**: `Program.cs` configures Serilog, registers DI services (`ArgusClient`, `HealthEndPointClient`, `IEndpointSyncService`, `IHealthCheckService`), sets `App.Services` static property. `App.axaml.cs` creates `MainWindowViewModel` and wires tray icon events.

**UI framework**: Avalonia 12.1 with FluentTheme (dark), ReactiveUI.Avalonia 12.0 with ReactiveUI 23.2 for MVVM, DynamicData for reactive collections.

**Package versions** are managed centrally in `Directory.Packages.props` at the root of the repository (NuGet Central Package Management). A project file references a package without a version (`<PackageReference Include="..." />`); a new package needs a `<PackageVersion>` entry in that file, in the group it belongs to. Packages in one group are updated together.

**Pinned packages** — do not update these as part of a routine dependency update (the same note is next to their versions in `Directory.Packages.props`):

- `ReactiveUI` 23.2.x, `ReactiveUI.Avalonia` 12.0.x and `ReactiveUI.SourceGenerators` 2.6.x. ReactiveUI 24 and later no longer build on System.Reactive and DynamicData: `Unit` becomes `RxVoid`, `WhenAnyValue` returns `PropertyValues` instead of tuples, `AvaloniaScheduler` is gone and `DisposeWith` and `Bind` have other signatures. Moving Pulse to it is a migration of all view models and views, not a version bump; it is tracked in issue #27. `ReactiveUI.Avalonia` 12.1.0 and later require ReactiveUI 24 or later. ReactiveUI also ships a System.Reactive-compatible distribution (`ReactiveUI.Reactive`, `ReactiveUI.Avalonia.Reactive`) that keeps `Unit`, `IScheduler` and `Subject<T>`; it has not been tried in Pulse.
- `WixToolset.Sdk` and `WixToolset.Util.wixext` 5.0.2, which must have the same version. The SDK version is in the `Sdk` attribute of `Argus.Health.Installer.wixproj`, the extension version in `Directory.Packages.props`. WiX 6 and later refuse to build until the Open Source Maintenance Fee EULA is accepted (error WIX7015), which is a licensing decision for the owner of the repository.

**ViewModels**: `MainWindowViewModel` (shell/navigation/notifications/sync state), `DashboardViewModel` (real-time endpoint status grid via `SourceCache`), `EndpointListViewModel` (CRUD list via `SourceCache`), `EndpointEditorViewModel` (create/edit form with validation), `EndpointStatusViewModel` (single dashboard row with computed `IsHealthy`/`StatusDisplay`).

**Services**: `EndpointSyncService` polls every 10s, publishes `EndpointsObservable`/`ConnectionErrorObservable`/`IsRunningObservable`. `HealthCheckService` runs per-endpoint monitor loops with Polly policies, publishes `ResultsObservable`/`FailureObservable`.

**Client**: `HealthEndPointClient` — typed CRUD client over ArgusTransfer named-pipe IPC. Timeout and retries are those of its `ArgusClient`, built by `HealthEndPointClient.CreateArgusClient`: a 10 s budget per call including retries, and an `ArgusRetryPolicy` (3 retries, exponential from 500 ms) that retries transient pipe failures but never a timeout, and never a POST that may already have reached the service. `DeleteAsync` treats a 404 as deleted, so a retried delete does not fail. Pulse no longer references Polly.

**Notifications**: `NotificationItem` model, fed from `FailureObservable`, backed by `SourceList`, auto-expires after 5s.

**Connection resilience**: Three-state tracking (connecting → connected/degraded → error). After 2 consecutive failures, sync stops automatically.

## Key Model

`HealthEndPoint` (in `Argus.Health.Common.Model`):
- `Identifier` — Guid, auto-assigned on create if empty
- `Name` — unique human-readable name
- `Url` — the endpoint URL to probe
- `Frequency` — polling interval in seconds (default 30)
- `Timeout` — HTTP timeout in seconds (default 5)
- `RetryCount` — Polly retry count (default 3)

## Git Conventions

- **No co-author trailer**: Do not add a `Co-Authored-By` line for Claude in commit messages.
- **Commit summary on completion**: After implementing a feature or fix, return a concise description of the changes suitable for use as a git commit message. Use the format `[Type] short summary` where Type is one of: Add, Update, Fix, Refactor, Remove. Keep it under two sentences.

## Code Conventions

- **language**: C#, no using python or other
- **Explicit usings**: `ImplicitUsings` is disabled — all `using` directives must be written explicitly.
- **usings location**: using statements go inside namespace.
- **Nullable**: enabled in `Argus.Health.Service`, `Argus.Health.Common`, and `Argus.Health.Pulse`; disabled in the test project.
- **XML doc comments**: required on all types and members: public, private, protetected and internal . do not use inheritdoc
- **License header**: every `.cs` file starts with the Apache-2.0 copyright block.
- **FluentResults**: CRUD methods return `Task<Result>` (not exceptions) for expected failures; repository read methods throw `DataException` on failure. A failure the caller can act on carries a typed error (`NotFoundError`, `ConflictError`) that `HealthEndPointModule` maps to a status code.
- **this**: make use of the `this` keyword on properties and methods

## Testing Conventions

- Test class naming: `{ClassName}TestFixture`
- Test method naming: `Verify_that_{scenario_in_snake_case}`
- Test convention: use Nunit with `Assert.That` syntax
- NUnit 5: `Assert.ThrowsAsync`, `Assert.DoesNotThrowAsync` and `Assert.CatchAsync` return a task. Always `await` them in an `async Task` test; a call that is not awaited asserts nothing. `Assert.That(async () => ..., Throws...)` is still evaluated without an `await`.
- Each test copies `TestData/ArgusHealth.sqlite` → `TestDataCopy/ArgusHealth.sqlite` in `[SetUp]` to ensure test isolation. The seeded database contains exactly 2 endpoints (GUIDs `cfb2e590-...` and `fe08550c-...`).
- The `internal` constructor of `HealthEndPointRepository` is used in tests to inject a custom database folder path.

## Build & Verification Workflow

After making code changes, follow this verification sequence before considering work complete:

1. **Build and inspect warnings**: Run `dotnet build Argus.Health.sln`. Inspect the output for all warnings (CSxxxx, CAxxxx, IDExxxx). Fix all warnings before proceeding.
2. **Run tests**: Run `dotnet test Argus.Health.sln`. All tests must pass.
3. **Format check**: Run `dotnet format Argus.Health.sln --verify-no-changes`. Fix any formatting violations reported.
4. **Final strict build**: Run `dotnet build Argus.Health.sln -warnaserror` as a final pass. The build must succeed with zero warnings and zero errors.

## Reference Sources

When working on `Argus.Health.Pulse` and doing work related to Avalonia and/or ReactiveUI, consult the ReactiveUI source code located at `C:\Users\sgerene\Documents\15-Source-Code\ReactiveUI` and the Avalonia source code located at `C:\Users\sgerene\Documents\15-Source-Code\AvaloniaUI\Avalonia`. You are always allowed to read the contents of this folder and its subfolders to understand API usage, patterns, and idiomatic conventions.