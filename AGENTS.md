# AGENTS.md

WARNING: temporary IGNORE this file. It describes the current state of the codebase, but it will be significantly refactored in the future.

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build the solution
dotnet build SmartBin.slnx

# Run the API locally (requires MongoDB and MQTT broker running)
dotnet run --project src/SmartBin.Api

# Build and run the full stack via Docker Compose
docker compose up --build

# Restore NuGet packages
dotnet restore SmartBin.slnx
```

There are no test projects in the solution yet.

`TreatWarningsAsErrors=true` is set across all projects — compiler warnings will fail the build.

## Architecture

Clean Architecture with four layers (each as a separate .NET 10 project):

```
SmartBin.Domain         ← no dependencies; entities, value objects, role definitions
SmartBin.Application    ← depends on Domain; service interfaces + generic repository abstraction
SmartBin.Infrastructure ← depends on Domain + Application; service implementations (JWT, BCrypt, business logic)
SmartBin.Api            ← depends on all; controllers, MQTT background service, Program.cs
```

### Domain layer (`SmartBin.Domain`)

- `Shared/Entity<TId>` — base class for all entities (identity equality by `Id`)
- `Shared/ValueObject` — base class for value objects (structural equality via `GetEqualityComponents()`)
- `Models/IEntity` — marker interface required by the generic repository
- `Models/UserRole` — abstract `record` hierarchy: `AdminRole > SalesManagerRole > GuestRole`. Each role is a singleton (`Instance`). Role hierarchy is enforced via `HasPermissionsOf()`. Parse from JWT claim string with `UserRole.Parse(string)`.

### Application layer (`SmartBin.Application`) [Mongo shouldn't be in the Application. Refactor is already planned]

- `GenericRepository/IRepository<T>` — generic MongoDB repository interface; entities must implement `IEntity`
- `GenericRepository/MongoRepository<T>` — concrete implementation; reads the collection name from `[MongoCollection("collectionName")]` attribute on the entity class (falls back to the type name)
- `GenericRepository/MongoSettings` / `IMongoSettings` — connection string + collection name config
- `Services/I*Service` — service interfaces for all domain services (User, Bin, Alert, ShiftLog, CleaningLog, Jwt, PasswordHasher)

### Infrastructure layer (`SmartBin.Infrastructure`)

Implements all `Application` service interfaces:
- `JwtService` — access token (15 min, HS256) + refresh token generation; refresh tokens stored in an in-memory `ConcurrentDictionary` (lost on restart)
- `BCryptPasswordHasher` — BCrypt.Net-Core wrapper
- `UserService`, `BinService`, `AlertService`, `CleaningLogService`, `ShiftLogService` — business logic backed by `IRepository<T>`

### API layer (`SmartBin.Api`)

- Controllers under `Controllers/` follow `api/[controller]` routing
- `Mqtt/MqttClientService` — `BackgroundService` that connects to the MQTT broker and handles incoming telemetry. MQTT topic format: `bins/{binId}/telemetry`. Deserializes `BinTelemetry` JSON and calls `IBinService.UpdateTelemetryAsync` + `UpdateTelemetryHistoryAsync`.
- Authorization uses named policies (`MinimumRole_Admin`, `MinimumRole_SalesManager`, `MinimumRole_Guest`) wired in `Program.cs`. Controllers decorate actions with `[AuthorizeRole(typeof(AdminRole))]` which resolves to the matching policy.
- `Extensions/AuthorizationExtensions.ValidateToken()` — validates the role claim in the JWT against the required role hierarchy.
- API docs available at `/docs` (Scalar UI) in Development.

## Configuration

The API reads config from two sources merged together:
1. `appsettings.json` / `appsettings.Development.json`
2. `.env` file in the working directory (loaded via DotNetEnv)

Copy `src/SmartBin.Api/.env.example` → `src/SmartBin.Api/.env` and fill in:

| Variable | Description |
|---|---|
| `MONGO_CONNECTION_STRING` | MongoDB connection string |
| `MQTT_HOST` / `MQTT_PORT` | Mosquitto broker address |
| `MQTT_CLIENT_ID` | MQTT client identifier |
| `MQTT_ALLOW_ANONYMOUS` | Skip credential auth if `true` |
| `MQTT_USERNAME` / `MQTT_PASSWORD` | Required when not anonymous |

JWT settings (`Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`) live in `appsettings.json`.

MongoDB collection names are configured in the `MongoSettings` section of `appsettings.json`.

## Infrastructure (Docker Compose)

`compose.yaml` brings up the full stack:

| Service | Port | Purpose |
|---|---|---|
| `smartbin.api` | 8080 | ASP.NET Core API |
| `smartbin.mongo` | 27017 | MongoDB 8.2 |
| `smartbin.mosquitto` | 1883 | Eclipse Mosquitto MQTT broker |
| `smartbin.otel-collector` | 4317/4318 | OpenTelemetry Collector |
| `smartbin.seq` | 5341 | Log storage (Seq) |
| `smartbin.grafana` | 3030 | Dashboards |
| `smartbin.prometheus` | 9090 | Metrics |

The OTel pipeline routes logs/traces to Seq and metrics to Prometheus. Copy `.env.example` → `.env` in the repo root and fill in `MONGODB_USERNAME`, `MONGODB_PASSWORD`, `GRAFANA_USERNAME`, `GRAFANA_PASSWORD`, `SEQ_API_KEY` before running compose.

The compose file references an **external** Docker network `smartbin_network` — create it once with `docker network create smartbin_network`.