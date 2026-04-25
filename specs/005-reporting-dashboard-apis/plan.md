# Implementation Plan: Phase 4 Reporting and Dashboard APIs

**Branch**: `005-reporting-dashboard-apis` | **Date**: 2026-04-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-reporting-dashboard-apis/spec.md`

## Summary

Add a read-only reporting slice to the existing modular-monolith SaaS API so
workspace owners and admins can retrieve a dashboard overview, usage summaries
grouped by user and tool, alerts summaries, and estimated cost summaries for a
selected reporting period. The design reuses Phase 2 AI usage events and Phase
3 risk findings, computes results on demand from existing workspace-scoped
data, exposes stable JSON endpoints for simple API consumers, and keeps
advanced analytics, custom report building, scheduled delivery, and cross-
workspace benchmarking out of scope.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS  
**Primary Dependencies**: ASP.NET Core 10 Web API controllers, ASP.NET Core Identity, Entity Framework Core 10, Npgsql Entity Framework provider, Microsoft.Extensions.Logging, ASP.NET Core integration testing support, existing application auditing abstractions  
**Storage**: PostgreSQL for existing application, AI usage event, risk finding, and audit persistence; no new reporting-specific persisted tables in this phase; SQLite in-memory for integration test execution  
**Testing**: xUnit for unit tests, `WebApplicationFactory` integration tests, and OpenAPI contract verification for dashboard and reporting endpoints  
**Target Platform**: Linux-hosted ASP.NET Core application with local Windows development support  
**Project Type**: Backend-first multi-tenant SaaS API service  
**Performance Goals**: Dashboard overview responses complete within 2 seconds for 90-day periods in workspaces with at least 100,000 AI usage events; grouped report first pages complete within 2 seconds; supported reporting requests complete within 5 seconds in 95% of acceptance-test scenarios  
**Constraints**: Preserve strict workspace isolation, keep reporting read-only, avoid background jobs and snapshot materialization in this phase, cap supported reporting windows to a bounded range, expose only aggregated or minimally identifying values, make missing cost data explicit, and avoid prompt or file-content leakage in reporting outputs  
**Scale/Scope**: Workspace-scoped dashboard overview, grouped usage summaries by user and tool, alerts summary with severity distribution and daily trend buckets, estimated cost summary with completeness metadata, and stable API datasets for startup and small-team workspaces  
**Tenant Boundary**: Every dashboard or report query is scoped by route workspace ID and validated against the caller's active workspace membership; all aggregates are computed only from records owned by that workspace; joins to users or memberships must not leak actors from another workspace  
**AuthZ Model**: Existing ASP.NET Core Identity cookie-backed authentication remains the only caller identity in Phase 4; workspace owners and admins may retrieve dashboard and report datasets; regular members may not access reporting; "simple API consumers" in this phase are authenticated clients using the same protected JSON endpoints rather than a new API-key or service-account model  
**Sensitive Data Handling**: Reporting returns aggregated counts, user identifiers plus display names, tool names, severity counts, date buckets, and estimated cost figures derived from existing data; it must not expose raw prompts, file names, file metadata details, evidence previews, or copied rule evidence when a summary answer is sufficient; cost values remain labeled as estimated and may be marked partial when source records lack cost data  
**Audit & Observability**: Record audit outcomes for dashboard reads, grouped report reads, alerts summary reads, cost summary reads, denied reporting access, and invalid reporting period requests; enrich structured logs with correlation ID, workspace ID, actor ID, report type, requested period, result size, and partial-cost indicator; add counters for successful report generation, empty-state responses, denied accesses, and invalid range errors  
**API / Contract Strategy**: Add workspace-scoped JSON endpoints under `/workspaces/{workspaceId}/dashboard` and `/workspaces/{workspaceId}/reports/*`; standardize reporting periods on inclusive UTC `fromDate` and `toDate` query parameters; return paged grouped summaries for user and tool datasets; use stable response shapes with explicit cost-completeness metadata and ProblemDetails failures for invalid periods or unsupported parameters

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

Gate status: PASS. The feature is backend-only, defines explicit reporting
contracts, keeps all aggregates inside workspace boundaries, limits reporting to
minimized summary data, and includes tests plus auditability for protected
report access.

Post-design re-check: PASS after producing `research.md`, `data-model.md`,
`contracts/reporting.openapi.yaml`, and `quickstart.md`.

## Project Structure

### Documentation (this feature)

```text
specs/005-reporting-dashboard-apis/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── reporting.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── AIUsageGuard.Api/
│   ├── Controllers/
│   │   ├── DashboardController.cs
│   │   └── ReportsController.cs
│   ├── Contracts/
│   │   └── Reporting/
│   ├── Policies/
│   └── Program.cs
├── AIUsageGuard.Application/
│   ├── Reporting/
│   │   ├── GetDashboard/
│   │   ├── GetUsageByUser/
│   │   ├── GetUsageByTool/
│   │   ├── GetAlertsSummary/
│   │   └── GetCostSummary/
│   ├── Auditing/
│   ├── Abstractions/
│   └── Models/
├── AIUsageGuard.Domain/
│   ├── AIUsageEvents/
│   ├── RiskDetection/
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
│   └── Reporting/
└── AIUsageGuard.IntegrationTests/
    └── Reporting/
```

**Structure Decision**: Keep the existing modular monolith and implement
reporting as a read-only `Reporting` slice across API, application,
infrastructure, and tests while reusing the Phase 2 and Phase 3 domain models.
This preserves the current tenant-isolation and authorization patterns, avoids
premature service decomposition or snapshot infrastructure, and keeps audit and
error handling consistent with the rest of the backend.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitutional or architectural exceptions are required for the Phase 4 scope | N/A |
