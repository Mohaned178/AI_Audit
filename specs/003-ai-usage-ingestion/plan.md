# Implementation Plan: Phase 2 AI Usage Event Ingestion

**Branch**: `003-ai-usage-ingestion` | **Date**: 2026-04-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-ai-usage-ingestion/spec.md`

## Summary

Extend the existing modular-monolith API so authenticated workspace members can
submit structured AI usage events and workspace owners or admins can retrieve
filterable event history. The design keeps the current SaaS foundation intact by
using workspace-scoped controller endpoints, deterministic idempotency for
duplicate protection, append-only event persistence, auditable ingestion
outcomes, and no new service-to-service authentication model in this phase.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS  
**Primary Dependencies**: ASP.NET Core 10 Web API controllers, ASP.NET Core Identity, Entity Framework Core 10, Npgsql Entity Framework provider, Microsoft.Extensions.Logging, ASP.NET Core integration testing support  
**Storage**: PostgreSQL for application and AI usage event persistence; SQLite in-memory for integration test execution  
**Testing**: xUnit for unit tests, `WebApplicationFactory` for integration tests, OpenAPI contract verification for request and response behavior  
**Target Platform**: Linux-hosted ASP.NET Core application with local Windows development support  
**Project Type**: Backend-first multi-tenant SaaS API service  
**Performance Goals**: Typical valid single-event ingestion completes within 1 second server-side; first-page filtered history retrieval completes within 2 seconds for workspaces with at least 100,000 stored events; valid event submissions are durably recorded within 1 minute end-to-end  
**Constraints**: Maintain strict workspace isolation, deny-by-default authorization, deterministic duplicate handling, no raw file binary storage, no risk evaluation or notification workflows in this phase, and no new machine or service identity model  
**Scale/Scope**: Initial support for the five defined event types, authenticated workspace-member ingestion, owner/admin history review, bounded event detail storage, and paged filtering for small-team workspaces and startups  
**Tenant Boundary**: Every event row is owned by exactly one workspace, workspace identifiers are route-scoped and validated against the caller's active membership, and history queries must never return cross-workspace records  
**AuthZ Model**: Existing ASP.NET Core Identity cookie-backed authentication remains the only caller identity in Phase 2; active workspace members may ingest events for their current workspace, while only workspace owners and admins may review event history  
**Sensitive Data Handling**: Persist only the structured event data needed for visibility and later governance, allow bounded optional text previews and file metadata, reject raw file binaries and unsupported free-form payloads, require authenticated transport, redact sensitive fields from logs, and retain event rows in the application database until a later retention feature introduces purge workflows  
**Audit & Observability**: Record audit outcomes for accepted submissions, rejected submissions, duplicate detections, and protected history access denials; enrich structured logs with correlation ID, workspace ID, actor ID, event type, and outcome; add ingestion and history counters aligned with those outcomes  
**API / Contract Strategy**: Controller-based HTTP endpoints under `/workspaces/{workspaceId}/events` with JSON request and response contracts, OpenAPI documentation, ProblemDetails failures, required idempotency keys for write safety, stable event-type enums, and explicit filter and pagination parameters for history retrieval

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

Gate status: PASS. The phase extends the backend API only, uses explicit event
contracts, preserves workspace-scoped authorization, and defines auditable
handling for accepted, rejected, and duplicate submissions.

Post-design re-check: PASS after producing `research.md`, `data-model.md`,
`contracts/ai-usage-events.openapi.yaml`, and `quickstart.md`.

## Project Structure

### Documentation (this feature)

```text
specs/003-ai-usage-ingestion/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ai-usage-events.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── AIUsageGuard.Api/
│   ├── Controllers/
│   │   └── AIUsageEventsController.cs
│   ├── Contracts/
│   │   └── AIUsageEvents/
│   ├── Policies/
│   └── Program.cs
├── AIUsageGuard.Application/
│   ├── AIUsageEvents/
│   │   ├── IngestEvent/
│   │   └── ListEvents/
│   ├── Auditing/
│   └── Abstractions/
├── AIUsageGuard.Domain/
│   ├── AIUsageEvents/
│   └── Auditing/
└── AIUsageGuard.Infrastructure/
    ├── Persistence/
    │   ├── Configurations/
    │   └── Migrations/
    ├── Auditing/
    └── Tenancy/

tests/
├── AIUsageGuard.UnitTests/
│   └── AIUsageEvents/
└── AIUsageGuard.IntegrationTests/
    └── AIUsageEvents/
```

**Structure Decision**: Keep the existing modular monolith and add an
`AIUsageEvents` slice across API, application, domain, infrastructure, and test
projects. This preserves the current boundary enforcement patterns, keeps
workspace resolution and audit behavior centralized, and avoids premature
service decomposition while the feature is still a single API capability.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitutional or architectural exceptions are required for the Phase 2 API-only scope | N/A |
