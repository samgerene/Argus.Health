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
```

## Solution Structure

| Project | Framework | Role |
|---|---|---|
| `ArgusTransfer` | net10.0 | Protocol, Routing, Serialization, Named-pipe client + server host |
| `ArgusTransfer.Tests` | net10.0 | Tests for ArgusTransfer |
| `Argus.Health.Common` | net10.0 | Shared models and domain JSON serialization |
| `Argus.Health.Client` | net10.0 | `HealthEndPointClient` typed client |
| `Argus.Health.Client.Tests` | net10.0 | Tests for `HealthEndPointClient` |
| `Argus.Health.Service` | net10.0 | Worker Service — main executable |
| `Argus.Health.Service.Tests` | net10.0 | Tests for domain service |
| `Argus.Health.Pulse` | net10.0 | Avalonia desktop dashboard for monitoring Argus Health endpoints |
| `Argus.Health.Pulse.Tests` | net10.0 | Tests for Argus.Health.Pulse |

## Architecture

### Runtime Flow

`Program.cs` bootstraps a `Host` with Serilog logging, registers a named `HttpClient` (`"ArgusHealth"`), registers `HealthEndPointRepository` as a singleton, and starts `HealthEndPointBackgroundService`.

`HealthEndPointBackgroundService` (extends `BackgroundService`) loads all endpoints from the repository on startup and spawns a per-endpoint monitoring loop. It subscribes to repository events (`EndpointAdded`, `EndpointUpdated`, `EndpointRemoved`) to dynamically start/stop monitors without restart. Each monitor loop is tracked in a `ConcurrentDictionary<Guid, (Task, CancellationTokenSource)>`.

Each monitor loop wraps HTTP calls with three stacked Polly policies (innermost to outermost): **circuit breaker** → **timeout** → **retry**.

### IPC Protocol

`ArgusPipeHostBackgroundService` (in `ArgusTransfer.Server`) listens on a configurable named pipe (default `"argus"`) for IPC requests. The protocol uses `ArgusRequest` / `ArgusResponse` messages with HTTP-like routes (`/healthendpoint`, `/healthendpoint/{identifier}`) and verbs (`ArgusVerb`: GET, POST, PUT, PATCH, HEAD, DELETE). Requests are routed via `ArgusRouter` to registered `IArgusModule` implementations. The pipe host is registered via `AddArgusPipeHost()` in `Program.cs`.

### Repository

`IHealthEndPointRepository` defines CRUD + three events. `HealthEndPointRepository` stores `HealthEndPoint` records in a SQLite database at `%ProgramData%\ArgusHealth\ArgusHealth.sqlite` (Windows) or the equivalent on Linux. The constructor has an `internal` overload accepting a custom folder path — used only in tests.

The database column for URLs is named `Urls` and stores semicolon-separated values in the raw SQL, but the model property is currently `Url` (singular `string`).

### Pulse Desktop Dashboard

**App bootstrap**: `Program.cs` configures Serilog, registers DI services (`ArgusClient`, `HealthEndPointClient`, `IEndpointSyncService`, `IHealthCheckService`), sets `App.Services` static property. `App.axaml.cs` creates `MainWindowViewModel` and wires tray icon events.

**UI framework**: Avalonia 11.3 with FluentTheme (dark), ReactiveUI 23.1 for MVVM, DynamicData for reactive collections.

**ViewModels**: `MainWindowViewModel` (shell/navigation/notifications/sync state), `DashboardViewModel` (real-time endpoint status grid via `SourceCache`), `EndpointListViewModel` (CRUD list via `SourceCache`), `EndpointEditorViewModel` (create/edit form with validation), `EndpointStatusViewModel` (single dashboard row with computed `IsHealthy`/`StatusDisplay`).

**Services**: `EndpointSyncService` polls every 10s, publishes `EndpointsObservable`/`ConnectionErrorObservable`/`IsRunningObservable`. `HealthCheckService` runs per-endpoint monitor loops with Polly policies, publishes `ResultsObservable`/`FailureObservable`.

**Client**: `HealthEndPointClient` — typed CRUD client over ArgusTransfer named-pipe IPC with Polly retry + timeout.

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

## Code Conventions

- **language**: C#, no using python or other
- **Explicit usings**: `ImplicitUsings` is disabled — all `using` directives must be written explicitly.
- **usings location**: using statements go inside namespace.
- **Nullable**: enabled in `Argus.Health.Service`, `Argus.Health.Common`, and `Argus.Health.Pulse`; disabled in the test project.
- **XML doc comments**: required on all types and members: public, private, protetected and internal . do not use inheritdoc
- **License header**: every `.cs` file starts with the Apache-2.0 copyright block.
- **FluentResults**: CRUD methods return `Task<Result>` (not exceptions) for expected failures; repository read methods throw `DataException` on failure.
- **this**: make use of the `this` keyword on properties and methods

## Testing Conventions

- Test class naming: `{ClassName}TestFixture`
- Test method naming: `Verify_that_{scenario_in_snake_case}`
- Test convention: use Nunit with `Assert.That` syntax
- Each test copies `TestData/ArgusHealth.sqlite` → `TestDataCopy/ArgusHealth.sqlite` in `[SetUp]` to ensure test isolation. The seeded database contains exactly 2 endpoints (GUIDs `cfb2e590-...` and `fe08550c-...`).
- The `internal` constructor of `HealthEndPointRepository` is used in tests to inject a custom database folder path.

## Reference Sources

When working on `Argus.Health.Pulse` and doing work related to Avalonia and/or ReactiveUI, consult the ReactiveUI source code located at `C:\Users\sgerene\Documents\15-Source-Code\ReactiveUI` and the Avalonia source code located at `C:\Users\sgerene\Documents\15-Source-Code\AvaloniaUI\Avalonia`. You are always allowed to read the contents of this folder and its subfolders to understand API usage, patterns, and idiomatic conventions.