# Implementation Plan: Phase 5 Background Jobs and Notifications

**Branch**: `006-background-jobs-notifications` | **Date**: 2026-04-22 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-background-jobs-notifications/spec.md`

## Summary

Add a durable background-processing and notification slice to the existing
modular-monolith SaaS API so workspace owners and admins can receive urgent
governance alerts and scheduled digest summaries without depending on manual
dashboard checks. The design introduces persisted notification preferences, job
run tracking, notification records, and delivery outcomes; uses hosted
background workers to scan due work, generate workspace-scoped notifications,
and retry failed deliveries; exposes admin APIs for notification preferences
and history review; and keeps complex workflow automation and multi-channel
enterprise orchestration out of scope.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS  
**Primary Dependencies**: ASP.NET Core 10 Web API controllers, ASP.NET Core hosted services, Microsoft.Extensions.Options, ASP.NET Core Identity, Entity Framework Core 10, Npgsql Entity Framework provider, Microsoft.Extensions.Logging, existing reporting and auditing abstractions  
**Storage**: PostgreSQL for existing application, AI usage event, risk finding, audit, and new notification/job state persistence; SQLite in-memory for integration test execution  
**Testing**: xUnit for unit tests, `WebApplicationFactory` integration tests, and OpenAPI contract verification for notification preference and history endpoints  
**Target Platform**: Linux-hosted ASP.NET Core application with local Windows development support  
**Project Type**: Backend-first multi-tenant SaaS API service with background processing  
**Performance Goals**: 95% of urgent alert scenarios reach at least one eligible recipient or record a final non-success outcome within 5 minutes of the triggering condition; 95% of scheduled digest runs complete within 15 minutes of the due window; retry scans pick up due failed deliveries within 10 minutes in acceptance-test scenarios  
**Constraints**: Preserve strict workspace isolation, keep background processing idempotent and durable across restarts, avoid duplicate urgent notifications, minimize copied AI data in outbound messages, keep supported delivery channels narrow in this phase, and avoid unplanned frontend dependencies  
**Scale/Scope**: Workspace-scoped notification preferences, urgent governance alerts, scheduled digest summaries, persisted delivery outcomes, and retry handling for startup and small-team workspaces across hundreds of active workspaces and low-to-moderate notification volume  
**Tenant Boundary**: Every notification preference, job run, notification, delivery outcome, and history query is scoped to one workspace; background workers may scan globally for due work but must resolve recipients, payloads, and persistence strictly within the owning workspace context; no job may leak another workspace's actors, findings, or summaries  
**AuthZ Model**: Existing ASP.NET Core Identity cookie-backed authentication remains the only interactive identity model in Phase 5; workspace owners and admins may manage notification preferences and review notification history; regular members may not access notification administration; background jobs execute as system-initiated workflows with auditable service actions rather than end-user permissions  
**Sensitive Data Handling**: Notifications may include workspace identifiers, actor display labels, tool labels, severity, threshold status, reporting periods, and estimated cost summaries only when needed to explain the message; raw prompts, raw file contents, copied evidence payloads, and unnecessary sensitive metadata must not be included in notification content or history views; recipient addresses are resolved from existing workspace members and used only for workspace-scoped delivery  
**Audit & Observability**: Record audit outcomes for notification preference reads and updates, background job runs, notification generation, skipped notifications, delivery attempts, retries, exhausted retries, and denied notification access; enrich structured logs with job run ID, workspace ID, notification type, notification ID, delivery outcome, attempt number, and covered period; add counters for due job scans, notifications created, notifications delivered, retries scheduled, skipped deliveries, and failed deliveries  
**API / Contract Strategy**: Add workspace-scoped JSON endpoints under `/workspaces/{workspaceId}/notification-preferences` and `/workspaces/{workspaceId}/notifications`; keep job triggering internal to hosted services; use stable response shapes for preferences, notification lists, notification detail, and ProblemDetails failures for invalid preference payloads or missing notification records

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Backend-first value is delivered without depending on unplanned frontend work.
- [x] API or event contracts, validation rules, and failure modes are defined.
- [x] Tenant isolation, workspace scoping, and deny-by-default authorization are specified.
- [x] Sensitive AI data handling covers collection minimization, storage, transport, and retention.
- [x] Audit events and explainable policy/risk decisions are defined for sensitive flows.
- [x] Tests cover domain logic, integration paths, and public contracts in proportion to risk.
- [x] Logging, metrics, and standardized error handling are included in the delivery scope.
- [x] Any exception for microservices, ML-based risk scoring, or reduced coverage is justified in Complexity Tracking.

Gate status: PASS. The feature is backend-only, defines explicit API contracts
for workspace notification administration, scopes every job and delivery record
to a workspace boundary, limits outbound content to minimized summaries, and
includes tests plus auditability for protected background automation.

Post-design re-check: PASS after producing `research.md`, `data-model.md`,
`contracts/notifications.openapi.yaml`, and `quickstart.md`.

## Project Structure

### Documentation (this feature)

```text
specs/006-background-jobs-notifications/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── notifications.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── AIUsageGuard.Api/
│   ├── Controllers/
│   │   ├── NotificationPreferencesController.cs
│   │   └── NotificationsController.cs
│   ├── Contracts/
│   │   └── Notifications/
│   └── Program.cs
├── AIUsageGuard.Application/
│   ├── BackgroundJobs/
│   ├── Notifications/
│   │   ├── ConfigureWorkspaceNotifications/
│   │   ├── GetNotification/
│   │   ├── ListNotifications/
│   │   └── ProcessDueNotifications/
│   ├── Auditing/
│   ├── Abstractions/
│   └── Models/
├── AIUsageGuard.Domain/
│   ├── Notifications/
│   ├── RiskDetection/
│   ├── Users/
│   └── Workspaces/
└── AIUsageGuard.Infrastructure/
    ├── BackgroundProcessing/
    ├── Notifications/
    ├── Persistence/
    │   ├── Configurations/
    │   ├── Migrations/
    │   └── ApplicationDbContext.cs
    └── Tenancy/

tests/
├── AIUsageGuard.UnitTests/
│   ├── BackgroundJobs/
│   └── Notifications/
└── AIUsageGuard.IntegrationTests/
    └── Notifications/
```

**Structure Decision**: Keep the existing modular monolith and introduce
Phase 5 as coordinated `BackgroundJobs` and `Notifications` slices across API,
application, domain, infrastructure, and tests. This preserves current tenant
isolation and authorization patterns, keeps background automation auditable
inside one deployment boundary, and avoids premature scheduler or workflow
service decomposition while still separating operator APIs from internal job
processing.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitutional or architectural exceptions are required for the Phase 5 scope | N/A |
