# Implementation Plan: Phase 7 Audit Logs and Hardening

**Branch**: `008-audit-logs-hardening` | **Date**: 2026-04-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/008-audit-logs-hardening/spec.md`

## Summary

Add a workspace-scoped audit-log and security-hardening slice to the existing
modular-monolith SaaS API so workspace owners and admins can investigate
protected actions, denied access, and suspicious activity while the platform
applies stronger defaults around login abuse, request integrity, and sensitive
failure handling. The design extends the existing audit backbone with richer
queryable evidence, introduces dedicated audit-history read APIs, strengthens
authentication-adjacent protections and protected write flows, and keeps
external SIEM integration, customer-managed retention, and legal-hold
workflows out of scope.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS  
**Primary Dependencies**: ASP.NET Core 10 Web API controllers, ASP.NET Core Identity cookie authentication, ASP.NET Core antiforgery support, Entity Framework Core 10, Npgsql Entity Framework provider, Microsoft.Extensions.Logging, existing auditing and tenancy abstractions  
**Storage**: PostgreSQL for existing application state plus enriched audit-record and user-account hardening fields; SQLite in-memory for unit and integration test execution  
**Testing**: xUnit for unit tests, `WebApplicationFactory` integration tests, and OpenAPI contract verification for audit-log endpoints  
**Target Platform**: Linux-hosted ASP.NET Core application with local Windows development support  
**Project Type**: Backend-first multi-tenant SaaS API service  
**Performance Goals**: Audit-log first-page reads complete within 2 seconds for workspaces with at least 1 million audit records; filtered audit investigations complete within 3 seconds in 95% of acceptance-test scenarios; failed-login and protected-write hardening decisions add no more than 500 ms in 95% of acceptance-test scenarios  
**Constraints**: Preserve strict workspace isolation, keep audit evidence append-only and queryable, avoid leaking sensitive details in failure responses, keep cookie-based authentication as the only interactive identity model, apply hardening without breaking normal admin workflows, and avoid introducing external security infrastructure in this phase  
**Scale/Scope**: Workspace-scoped audit history, filtered audit investigation, login-failure resistance, stronger request-integrity enforcement for authenticated writes, and audit evidence enrichment for startup and small-team workspaces across hundreds of active workspaces  
**Tenant Boundary**: Every audit-history read is scoped to one workspace, validated against the caller's active workspace membership, and must not expose another workspace's evidence through records, filters, counts, or error responses; system-initiated audit events may be created globally but remain queryable only within the owning workspace when workspace-scoped  
**AuthZ Model**: Existing ASP.NET Core Identity cookie-backed authentication remains the only interactive identity model in Phase 7; workspace owners and admins may read workspace audit history; regular members may not access audit history; hardening decisions apply before or during protected request processing and must remain deny-by-default for missing or invalid protection context  
**Sensitive Data Handling**: Audit history may expose actor identifiers, action types, outcomes, target identifiers, timestamps, correlation identifiers, and concise investigation reasons; it must not expose raw prompts, raw files, full secrets, full tokens, full client addresses, or unnecessary request payloads; client context used for hardening is minimized and stored only to the degree needed for traceability and abuse resistance  
**Audit & Observability**: Record audit outcomes for audit-history reads, denied audit access, failed and locked sign-in attempts, protected-request hardening rejections, sensitive setting changes, and existing protected feature actions; enrich structured logs with workspace ID, actor ID, audit record ID, correlation ID, action type, outcome, and hardening rule label; add counters for audit-history reads, denied audit-history reads, failed sign-ins, temporary lockouts, and hardening rejections  
**API / Contract Strategy**: Add stable workspace-scoped JSON endpoints under `/workspaces/{workspaceId}/audit-logs` and `/workspaces/{workspaceId}/audit-logs/{auditLogId}`; support filterable list reads with stable paging and minimized detail fields; keep hardening controls internal to authentication and protected request workflows; use `ProblemDetails` for invalid filters, missing audit records, denied access, and hardened request rejections

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

Gate status: PASS. The feature remains backend-only, defines explicit audit-log
contracts, keeps evidence and hardening decisions inside workspace boundaries,
extends existing auditability rather than bypassing it, and includes test plus
operational expectations for sensitive request handling.

Post-design re-check: PASS after producing `research.md`, `data-model.md`,
`contracts/audit-logs.openapi.yaml`, and `quickstart.md`.

## Project Structure

### Documentation (this feature)

```text
specs/008-audit-logs-hardening/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── audit-logs.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── AIUsageGuard.Api/
│   ├── Controllers/
│   │   ├── AuditLogsController.cs
│   │   └── AuthController.cs
│   ├── Contracts/
│   │   └── AuditLogs/
│   ├── Policies/
│   └── Program.cs
├── AIUsageGuard.Application/
│   ├── Auditing/
│   │   ├── GetAuditLog/
│   │   └── ListAuditLogs/
│   ├── Identity/
│   ├── Security/
│   ├── Abstractions/
│   └── Models/
├── AIUsageGuard.Domain/
│   ├── Auditing/
│   ├── Users/
│   └── Workspaces/
└── AIUsageGuard.Infrastructure/
    ├── Auditing/
    ├── Persistence/
    │   ├── Configurations/
    │   ├── Migrations/
    │   └── ApplicationDbContext.cs
    └── Tenancy/

tests/
├── AIUsageGuard.UnitTests/
│   ├── Auditing/
│   ├── Identity/
│   └── Security/
└── AIUsageGuard.IntegrationTests/
    ├── Auditing/
    └── Security/
```

**Structure Decision**: Keep the existing modular monolith and add Phase 7 as
coordinated `Auditing` and `Security` slices across API, application,
infrastructure, and tests while reusing the current tenancy, authorization,
and audit-record persistence patterns. This preserves backend modularity,
avoids premature external security-service decomposition, and keeps audit
history plus hardening behavior traceable through one deployment boundary.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitutional or architectural exceptions are required for the Phase 7 scope | N/A |
