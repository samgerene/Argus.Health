# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Argus Health is a .NET 10 Worker Service that monitors HTTP health endpoints by executing GET requests on configurable intervals and reporting their status. It runs as a Windows Service or systemd daemon on Linux.

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

- **language**: C#, no using python
- **Explicit usings**: `ImplicitUsings` is disabled — all `using` directives must be written explicitly.
- **Nullable**: enabled in `Argus.Health.Service` and `Argus.Health.Common`; disabled in the test project.
- **XML doc comments**: required on all public types and members.
- **License header**: every `.cs` file starts with the Apache-2.0 copyright block.
- **FluentResults**: CRUD methods return `Task<Result>` (not exceptions) for expected failures; repository read methods throw `DataException` on failure.

## Testing Conventions

- Test class naming: `{ClassName}TestFixture`
- Test method naming: `Verify_that_{scenario_in_snake_case}`
- Test convention: use Nunit with `Assert.That` syntax
- Each test copies `TestData/ArgusHealth.sqlite` → `TestDataCopy/ArgusHealth.sqlite` in `[SetUp]` to ensure test isolation. The seeded database contains exactly 2 endpoints (GUIDs `cfb2e590-...` and `fe08550c-...`).
- The `internal` constructor of `HealthEndPointRepository` is used in tests to inject a custom database folder path.
