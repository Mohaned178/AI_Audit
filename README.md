# AI Usage Guard

AI Usage Guard is a backend-first SaaS for monitoring how employees use AI tools inside a workspace. It captures usage events, detects risky activity such as sensitive prompts, file uploads, and unapproved tool usage, and gives workspace owners and admins the reporting they need to control cost, exposure, and policy compliance.

## Short Summary

The product is designed for organizations that want to adopt AI tools without losing governance. It combines event ingestion, risk evaluation, audit logging, billing awareness, notifications, and workspace-scoped reporting in a single ASP.NET Core backend.

## Problem Statement

AI adoption creates operational risk faster than most companies can manage it manually.

- Employees may paste confidential data into prompts.
- Users may upload files that should never leave company boundaries.
- Teams may call AI tools that were never approved by security or procurement.
- AI usage can grow quietly, making spend and accountability difficult to track.

AI Usage Guard addresses those problems with a central backend that records activity, evaluates policy, and exposes the right data to admins and owners.

## Main Features

- Workspace-based AI usage ingestion with idempotency support.
- Risk detection for sensitive prompts, file uploads, unapproved tools, and cost thresholds.
- Workspace risk policies that control how events are evaluated.
- Role-based access for owners, admins, and members.
- Membership management for workspace access control.
- Audit logs for authentication, administrative actions, and security events.
- Billing cycle tracking and estimated cost reporting.
- Dashboards and reports for usage by user, usage by tool, alerts, and cost summaries.
- Notification preferences and background delivery workflows.
- Workspace-scoped APIs with cross-tenant isolation enforced at the backend.

## Target Users

- Workspace owners who need oversight, governance, and accountability.
- Workspace admins who review risk findings and operational reports.
- Security and compliance teams that need an audit trail and policy enforcement.
- Platform or engineering teams integrating AI governance into internal tools.

## Core Workflow

1. A workspace is created through registration or an admin onboarding flow.
2. Users authenticate with cookie-based identity and are placed into a workspace context.
3. AI usage events are ingested from approved clients or internal integrations.
4. The backend stores the event, evaluates risk rules, and records findings when needed.
5. Usage rolls into billing cycles, dashboards, alert summaries, and cost reports.
6. Background workers generate digests, retry deliveries, and reconcile late usage.
7. Owners and admins inspect events, findings, audit logs, and reports through the API.

## Architecture Overview

The solution follows a layered ASP.NET Core architecture:

- `AIUsageGuard.Api` hosts HTTP controllers, authentication, authorization, antiforgery, health checks, request logging, and the middleware pipeline.
- `AIUsageGuard.Application` contains use-case services, commands, queries, and business-oriented models.
- `AIUsageGuard.Domain` defines the core business entities and enumerations for workspaces, users, memberships, AI usage, risk, billing, auditing, and notifications.
- `AIUsageGuard.Infrastructure` implements EF Core persistence, ASP.NET Core Identity, notification delivery, background workers, tenancy helpers, and other runtime integrations.
- `tests/` contains unit and integration tests for services, policy logic, persistence, and API behavior.

The controllers stay thin. The application layer owns the workflow. Infrastructure is responsible for data access, background execution, and platform concerns.

## Tech Stack

- C# 14
- .NET 10
- ASP.NET Core 10 Web API
- ASP.NET Core Identity
- Entity Framework Core 10
- PostgreSQL for application and identity persistence
- Npgsql Entity Framework provider
- SQLite for local/test scenarios where configured
- xUnit for automated testing

## Project Structure

```text
src/
  AIUsageGuard.Api/
    Controllers/
    Contracts/
    Policies/
    Security/
    Program.cs
    appsettings.json
    appsettings.Development.json
  AIUsageGuard.Application/
    AIUsageEvents/
    Auditing/
    BackgroundJobs/
    Billing/
    Identity/
    Memberships/
    Notifications/
    Reporting/
    RiskDetection/
    Security/
    Workspaces/
  AIUsageGuard.Domain/
    AIUsageEvents/
    Auditing/
    Memberships/
    RiskDetection/
    Users/
    Workspaces/
  AIUsageGuard.Infrastructure/
    Auditing/
    BackgroundProcessing/
    Billing/
    Identity/
    Notifications/
    Persistence/
    Tenancy/
tests/
  AIUsageGuard.UnitTests/
  AIUsageGuard.IntegrationTests/
specs/
docs/
```

Generated artifacts such as `bin/`, `obj/`, and `.artifacts/` are not part of the source tree.

## Setup and Run Instructions

### Prerequisites

- .NET 10 SDK
- PostgreSQL instance for local development

### Restore dependencies

```bash
dotnet restore AIUsageGuard.slnx
```

### Build the solution

```bash
dotnet build AIUsageGuard.slnx
```

### Run the API

```bash
dotnet run --project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
```

The API uses the ports defined in the ASP.NET Core launch settings for development runs.

### Run with Docker Compose

The repository includes a local-development Docker setup for the API and PostgreSQL only. It is intended for demos, manual testing, and portfolio walkthroughs, not for production deployment.

See [docs/docker-local-development.md](docs/docker-local-development.md) for the complete Docker workflow, including host-side EF Core migration commands against the containerized PostgreSQL instance.

```bash
docker compose up --build -d
```

This starts:

- `postgres` on host port `5433`
- `api` on host port `5172`

Inside Docker, the API uses the PostgreSQL compose service name:

```text
Host=postgres;Port=5432;Database=ai_usage_guard_dev;Username=postgres;Password=postgres
```

From the host machine, Swagger is available at:

```text
http://localhost:5172/swagger
```

Health checks are available at:

```text
http://localhost:5172/health
```

To stop the stack:

```bash
docker compose down
```

To stop the stack and remove the PostgreSQL volume:

```bash
docker compose down -v
```

## Environment Variables

The application relies on standard ASP.NET Core configuration. The most relevant settings are:

- `ConnectionStrings__DefaultConnection` - primary database connection string.
- `Database__Provider` - set to `Postgres` or `Sqlite`.
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

Default values are defined in `src/AIUsageGuard.Api/appsettings.json` and `src/AIUsageGuard.Api/appsettings.Development.json`.

For Docker Compose local development, the key overrides are:

- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://+:8080`
- `Database__Provider=Postgres`
- `ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=ai_usage_guard_dev;Username=postgres;Password=postgres`

## Database and Migrations

Entity Framework Core migrations live in:

```text
src/AIUsageGuard.Infrastructure/Persistence/Migrations/
```

They cover the full platform schema, including identity, workspaces, memberships, usage events, risk findings, billing cycles, notifications, audit records, and supporting indexes.

Typical commands:

```bash
dotnet ef migrations add <MigrationName> --project src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj --startup-project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
dotnet ef database update --project src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj --startup-project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
```

### Apply Migrations Against the Docker PostgreSQL Database

The API container does not apply EF Core migrations automatically. For local development, start the Docker PostgreSQL service first, then run EF Core from the host machine against port `5433`.

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

Use the correct host based on where the process runs:

- inside Docker: `Host=postgres;Port=5432`
- from the host machine: `Host=localhost;Port=5433`

## API Overview

The API is organized around workspace-scoped routes and backend administration flows.

Authentication:

- `POST /auth/register` - create a workspace and initial owner session.
- `POST /auth/login` - sign in an existing user.
- `POST /auth/logout` - end the current session.

Workspace context:

- `GET /workspaces/{workspaceId}/context` - resolve the active workspace context.

AI usage:

- `POST /workspaces/{workspaceId}/events` - ingest a new AI usage event.
- `GET /workspaces/{workspaceId}/events` - list workspace events.

Risk detection:

- `GET /workspaces/{workspaceId}/risk-policy` - read the current risk policy.
- `PUT /workspaces/{workspaceId}/risk-policy` - update the risk policy.
- `GET /workspaces/{workspaceId}/risk-findings` - list findings.
- `GET /workspaces/{workspaceId}/risk-findings/{findingId}` - fetch finding details.

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

Development-only helper:

- `GET /dev/antiforgery-token` - return a fresh antiforgery request token and set the cookie required for protected write requests.

Security controls are enforced with cookie authentication, workspace policies, authorization handlers, antiforgery protection on unsafe requests, and workspace context tracking to prevent cross-tenant access.

## Example Use Case / Demo Scenario

A finance company wants employees to use AI assistants without exposing client data or losing spend control.

1. A security lead creates the workspace and becomes the initial owner.
2. The team adds members and approves a narrow set of AI tools.
3. Employees start submitting prompts and file uploads through an internal integration.
4. The backend flags a prompt that contains sensitive data and stores a finding for review.
5. A usage report shows which users and tools are driving cost.
6. An urgent notification is created for the security team, while the weekly digest keeps leadership informed.
7. Audit logs preserve the administrative history for compliance review.

This is the kind of flow the system is meant to support: operational visibility without turning the product into a frontend-heavy dashboard app.

## Testing

The repository includes both unit and integration test coverage.

```bash
dotnet test tests/AIUsageGuard.UnitTests/AIUsageGuard.UnitTests.csproj
dotnet test tests/AIUsageGuard.IntegrationTests/AIUsageGuard.IntegrationTests.csproj
```

Unit tests focus on service behavior, policy evaluation, and business rules. Integration tests cover controllers, authentication, authorization, persistence, and workspace isolation.

For manual testing in Development, call `GET /dev/antiforgery-token` first, then send the returned request token in the `X-CSRF-TOKEN` header when calling protected write endpoints such as `POST /workspaces/{workspaceId}/events`.

The same antiforgery flow applies when the API runs in Docker Compose because the container stays in the `Development` environment for local testing.

## Future Improvements

- Add richer ingestion adapters for browser extensions and third-party AI platforms.
- Expand risk scoring with configurable weights, thresholds, and rule groups.
- Add rate limiting and usage quotas at the API edge.
- Improve reporting exports for compliance and finance teams.
- Add more delivery channels for alerts and digests.
- Improve observability for background jobs and failed deliveries.

## Conclusion

AI Usage Guard is a practical backend foundation for AI governance in the enterprise. It focuses on the hard parts that matter to security, compliance, and operations: authenticated workspace access, event ingestion, risk detection, cost visibility, auditability, and tenant isolation. The result is a credible SaaS backend for teams that need to control AI usage without blocking adoption.
