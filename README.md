# AI Usage Guard

AI Usage Guard is a backend-first SaaS platform for monitoring how employees use AI tools inside a workspace. It records AI usage events, evaluates them against workspace policy, highlights risky behavior, and exposes the reporting and audit data needed for governance, cost control, and compliance.

## What It Does

- Ingests workspace-scoped AI usage events with idempotency support.
- Detects risky activity such as sensitive prompts, file uploads, unapproved tools, and cost threshold breaches.
- Enforces workspace membership and role-based access for owners, admins, and members.
- Tracks billing state, usage cycles, dashboard summaries, and cost reports.
- Stores audit logs for security-sensitive and administrative activity.
- Runs background workflows for urgent alerts, digest generation, notification retries, and billing reconciliation.

## Current Scope

The repository currently includes the backend foundation plus these implemented feature areas:

- `002-core-saas-foundation`
- `003-ai-usage-ingestion`
- `004-risk-detection-engine`
- `005-reporting-dashboard-apis`
- `006-background-jobs-notifications`
- `007-usage-limits-billing`
- `008-audit-logs-hardening`

Specs and supporting design artifacts live under [`specs/`](specs).

## Architecture

The solution follows a layered ASP.NET Core design:

- `AIUsageGuard.Api` hosts controllers, auth, authorization, antiforgery, Swagger, health checks, and the HTTP pipeline.
- `AIUsageGuard.Application` contains use-case services, query/command handlers, options, and application models.
- `AIUsageGuard.Domain` defines core business entities and enums.
- `AIUsageGuard.Infrastructure` implements EF Core persistence, Identity storage, tenancy helpers, notification delivery, and hosted background workers.
- `tests/` contains unit and integration coverage across API, persistence, policy, reporting, billing, and security flows.

## Tech Stack

- C# 14
- .NET 10
- ASP.NET Core 10 Web API
- ASP.NET Core Identity
- Entity Framework Core 10
- PostgreSQL with Npgsql
- SQLite support for selected local or test scenarios
- xUnit
- Docker Compose for local API + PostgreSQL development

## Repository Layout

```text
src/
  AIUsageGuard.Api/
  AIUsageGuard.Application/
  AIUsageGuard.Domain/
  AIUsageGuard.Infrastructure/
tests/
  AIUsageGuard.UnitTests/
  AIUsageGuard.IntegrationTests/
docs/
specs/
.specify/
```

Generated output such as `bin/`, `obj/`, `.vs/`, and `.artifacts/` is not part of the source layout.

## Quick Start

### Prerequisites

- .NET 10 SDK
- PostgreSQL for local host-based development, or Docker Desktop for the containerized flow

### Restore

```bash
dotnet restore AIUsageGuard.slnx
```

### Build

```bash
dotnet build AIUsageGuard.slnx
```

### Run the API

```bash
dotnet run --project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
```

Development launch settings expose the API on:

- `http://localhost:5172`
- `https://localhost:7015`

Swagger is available in Development at:

- `http://localhost:5172/swagger`
- `https://localhost:7015/swagger`

Health checks:

- `http://localhost:5172/health`
- `https://localhost:7015/health`

## Docker Local Development

The repository includes a two-service local Docker setup:

- `postgres` on host port `5433`
- `api` on host port `5172`

Start the stack:

```bash
docker compose up --build -d
```

Useful URLs:

- Swagger: `http://localhost:5172/swagger`
- Health: `http://localhost:5172/health`

Stop the stack:

```bash
docker compose down
```

Remove the PostgreSQL volume too:

```bash
docker compose down -v
```

Full Docker notes are in [docs/docker-local-development.md](docs/docker-local-development.md).

## Configuration

The application uses standard ASP.NET Core configuration. The most important settings are:

- `ConnectionStrings__DefaultConnection`
- `Database__Provider`
- `Notifications__Processing__WorkerInterval`
- `Notifications__Processing__PendingDeliveryBatchSize`
- `Notifications__Processing__UrgentAlertBatchSize`
- `Notifications__Processing__MaxDeliveryAttempts`
- `Notifications__Processing__InitialRetryDelay`
- `Notifications__DigestScheduling__WeeklyDigestStartsOn`
- `Notifications__DigestScheduling__CompletedPeriodDelay`
- `Notifications__DigestScheduling__SummaryTopCount`
- `Billing__Reconciliation__WorkerInterval`
- `Billing__Reconciliation__LateActivityWindow`
- `Billing__Reconciliation__MaxWorkspaceBatchSize`
- `Security__SignInHardening__MaxFailedAttempts`
- `Security__SignInHardening__FailureWindow`
- `Security__SignInHardening__LockoutDuration`
- `Security__ProtectedRequestIntegrity__Enabled`
- `Security__ProtectedRequestIntegrity__RequireForUnsafeMethods`
- `Security__ProtectedRequestIntegrity__HeaderName`

Defaults live in:

- `src/AIUsageGuard.Api/appsettings.json`
- `src/AIUsageGuard.Api/appsettings.Development.json`

### Connection Strings

Use the correct PostgreSQL host for the environment:

- Inside Docker: `Host=postgres;Port=5432;Database=ai_usage_guard_dev;Username=postgres;Password=postgres`
- From the host machine: `Host=localhost;Port=5433;Database=ai_usage_guard_dev;Username=postgres;Password=postgres`

## Database and Migrations

EF Core migrations live in:

```text
src/AIUsageGuard.Infrastructure/Persistence/Migrations/
```

Create a migration:

```bash
dotnet ef migrations add <MigrationName> --project src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj --startup-project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
```

Apply migrations:

```bash
dotnet ef database update --project src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj --startup-project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
```

To apply migrations against the Docker PostgreSQL instance, first start Compose and then point EF Core at host port `5433`.

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=ai_usage_guard_dev;Username=postgres;Password=postgres"
dotnet ef database update --project src\AIUsageGuard.Infrastructure\AIUsageGuard.Infrastructure.csproj --startup-project src\AIUsageGuard.Api\AIUsageGuard.Api.csproj
Remove-Item Env:ConnectionStrings__DefaultConnection
```

Bash:

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=ai_usage_guard_dev;Username=postgres;Password=postgres" \
dotnet ef database update --project src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj --startup-project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
```

## API Surface

Authentication:

- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/logout`

Workspace context:

- `GET /workspaces/{workspaceId}/context`

AI usage:

- `POST /workspaces/{workspaceId}/events`
- `GET /workspaces/{workspaceId}/events`

Risk detection:

- `GET /workspaces/{workspaceId}/risk-policy`
- `PUT /workspaces/{workspaceId}/risk-policy`
- `GET /workspaces/{workspaceId}/risk-findings`
- `GET /workspaces/{workspaceId}/risk-findings/{findingId}`

Billing and reporting:

- `GET /workspaces/{workspaceId}/billing/plan-status`
- `GET /workspaces/{workspaceId}/billing/cycles`
- `GET /workspaces/{workspaceId}/billing/cycles/{cycleId}`
- `GET /workspaces/{workspaceId}/dashboard`
- `GET /workspaces/{workspaceId}/reports/usage-by-user`
- `GET /workspaces/{workspaceId}/reports/usage-by-tool`
- `GET /workspaces/{workspaceId}/reports/alerts-summary`
- `GET /workspaces/{workspaceId}/reports/cost-summary`

Governance and operations:

- `GET /workspaces/{workspaceId}/audit-logs`
- `GET /workspaces/{workspaceId}/audit-logs/{auditLogId}`
- `GET /workspaces/{workspaceId}/notifications`
- `GET /workspaces/{workspaceId}/notifications/{notificationId}`
- `GET /workspaces/{workspaceId}/notification-preferences`
- `PUT /workspaces/{workspaceId}/notification-preferences`
- `GET /workspaces/{workspaceId}/memberships`
- `POST /workspaces/{workspaceId}/memberships`
- `PATCH /workspaces/{workspaceId}/memberships/{membershipId}`

Development helper:

- `GET /dev/antiforgery-token`

## Security Model

- Cookie authentication is used for application sessions.
- Workspace policies enforce member, admin, and owner access levels.
- Antiforgery protection is enabled for protected write requests.
- Workspace context tracking prevents cross-tenant access.
- Sign-in hardening options support failed-attempt windows and lockout behavior.
- Correlation IDs are echoed through the API response headers for request tracing.

For manual testing in Development, call `GET /dev/antiforgery-token` first and then send the returned token in the `X-CSRF-TOKEN` header for protected write requests.

## Testing

Run unit tests:

```bash
dotnet test tests/AIUsageGuard.UnitTests/AIUsageGuard.UnitTests.csproj
```

Run integration tests:

```bash
dotnet test tests/AIUsageGuard.IntegrationTests/AIUsageGuard.IntegrationTests.csproj
```

Test coverage currently includes:

- authentication and workspace onboarding
- event ingestion and deduplication
- workspace isolation and authorization
- risk evaluation and policy management
- billing cycles and plan-status flows
- reporting and dashboard APIs
- notifications and digest workflows
- audit log access and security hardening

See [docs/api-test-cases.md](docs/api-test-cases.md) for API-oriented manual test coverage.

## Roadmap

Likely next steps for the platform:

- richer ingestion adapters for browser and third-party AI tools
- more configurable risk scoring and rule composition
- stronger API edge controls such as rate limiting and quotas
- improved reporting export formats for finance and compliance
- additional notification channels
- deeper operational telemetry for background processing

## License

No license file is currently included in the repository.
