# Implementation Plan: Phase 1 Core SaaS Foundation

**Branch**: `002-core-saas-foundation` | **Date**: 2026-04-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-core-saas-foundation/spec.md`

## Summary

Build the tenant-aware SaaS foundation for AI Usage Guard by delivering account
registration, workspace creation, authentication, workspace membership
management, role-based access control, and a workspace context API. The
implementation remains backend-first and prepares later AI usage
features without introducing event ingestion, policy evaluation, reporting, or
notifications.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS  
**Primary Dependencies**: ASP.NET Core 10 Web API controllers, ASP.NET Core Identity, Entity Framework Core 10, Npgsql Entity Framework provider, Microsoft.Extensions.Logging, ASP.NET Core integration testing support  
**Storage**: PostgreSQL for application data and Identity persistence  
**Testing**: xUnit for unit tests, `WebApplicationFactory` for integration tests, SQLite in-memory for integration test database scenarios  
**Target Platform**: Linux-hosted ASP.NET Core application with local Windows development support  
**Project Type**: Backend-first multi-tenant SaaS API service  
**Performance Goals**: New customer onboarding completes in under 5 minutes; valid sign-in reaches the correct workspace context on first attempt; member access changes complete in under 2 minutes  
**Constraints**: Maintain strict tenant isolation, deny-by-default authorization, auditable sensitive actions, no AI event ingestion or reporting in this phase, API-only delivery in this phase  
**Scale/Scope**: Initial support for small teams and startups with multiple workspaces, owner/admin/member roles, and foundational user management  
**Tenant Boundary**: Every authenticated action resolves a current workspace and rejects access when the caller lacks a valid membership for that workspace  
**AuthZ Model**: ASP.NET Core Identity authentication with cookie-backed first-party sessions; policy-based authorization over workspace membership roles owner, admin, and member  
**Sensitive Data Handling**: User credentials, account profiles, workspace metadata, membership status, and role assignments are sensitive and must be minimized, access-controlled, and auditable  
**Audit & Observability**: Structured logs, audit records for workspace creation and membership/role changes, consistent authorization failure handling, health checks for app and database  
**API / Contract Strategy**: Controller-based HTTP endpoints for onboarding, session management, workspace context, and membership administration; stable JSON contracts with ProblemDetails-style failures

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Backend-first value is delivered without depending on an unplanned interactive client.
- [x] API or event contracts, validation rules, and failure modes are defined.
- [x] Tenant isolation, workspace scoping, and deny-by-default authorization are specified.
- [x] Sensitive AI data handling covers collection minimization, storage, transport, and retention.
- [x] Audit events and explainable policy/risk decisions are defined for sensitive flows.
- [x] Tests cover domain logic, integration paths, and public contracts in proportion to risk.
- [x] Logging, metrics, and standardized error handling are included in the delivery scope.
- [x] Any exception for microservices, ML-based risk scoring, or reduced coverage is justified in Complexity Tracking.

Gate status: PASS. The phase delivers backend onboarding and access control,
uses stable contracts, and keeps the UI limited to the minimum needed to confirm
authenticated workspace context.

Post-design re-check: PASS after producing `research.md`, `data-model.md`,
`contracts/foundation-api.openapi.yaml`, and `quickstart.md`.

## Project Structure

### Documentation (this feature)

```text
specs/002-core-saas-foundation/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── foundation-api.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── AIUsageGuard.Api/
│   ├── Controllers/
│   ├── Contracts/
│   ├── Policies/
│   └── Program.cs
├── AIUsageGuard.Application/
│   ├── Workspaces/
│   ├── Memberships/
│   ├── Identity/
│   └── Auditing/
├── AIUsageGuard.Domain/
│   ├── Workspaces/
│   ├── Memberships/
│   ├── Users/
│   └── Auditing/
└── AIUsageGuard.Infrastructure/
    ├── Persistence/
    ├── Identity/
    ├── Auditing/
    └── Tenancy/

tests/
├── AIUsageGuard.UnitTests/
└── AIUsageGuard.IntegrationTests/
```

**Structure Decision**: Use a modular monolith with an ASP.NET Core host,
application services, domain model, and infrastructure layer. This structure
keeps tenant resolution, authorization, and audit concerns traceable while
avoiding premature service decomposition. The host exposes a controller-based
HTTP API only.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitutional or architectural exceptions are required for the API-only Phase 1 scope | N/A |
