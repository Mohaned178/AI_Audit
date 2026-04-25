# Implementation Plan: Phase 3 Risk Detection Engine

**Branch**: `004-risk-detection-engine` | **Date**: 2026-04-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-risk-detection-engine/spec.md`

## Summary

Extend the existing modular-monolith SaaS API so every accepted AI usage event
is evaluated against a fixed set of deterministic workspace-scoped risk rules.
The design adds built-in evaluators for sensitive data patterns, file upload
detection, unapproved tool usage, and cost threshold exceedance; persists
deduplicated risk findings and evaluation outcomes; exposes review APIs for
workspace admins; and adds simple workspace policy management for approved
tools and supported thresholds without introducing background jobs, ML-based
scoring, or automated enforcement.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS  
**Primary Dependencies**: ASP.NET Core 10 Web API controllers, ASP.NET Core Identity, Entity Framework Core 10, Npgsql Entity Framework provider, Microsoft.Extensions.Logging, ASP.NET Core integration testing support, existing application auditing abstractions  
**Storage**: PostgreSQL for application, event, risk finding, and workspace risk policy persistence; SQLite in-memory for integration test execution  
**Testing**: xUnit for unit tests, `WebApplicationFactory` integration tests, and OpenAPI contract verification for risk review and policy-management behavior  
**Target Platform**: Linux-hosted ASP.NET Core application with local Windows development support  
**Project Type**: Backend-first multi-tenant SaaS API service  
**Performance Goals**: 95% of matching events produce risk findings within 2 minutes of acceptance, synchronous risk evaluation adds less than 500 ms to typical event ingestion, and first-page risk finding review completes within 2 seconds for workspaces with at least 50,000 findings  
**Constraints**: Preserve strict workspace isolation, use deterministic built-in rule evaluation only, avoid deep prompt understanding and ML scoring, do not add notifications or background rescans in this phase, minimize copied sensitive evidence in findings, and keep policy changes effective for future events only  
**Scale/Scope**: Support four built-in rule families, workspace-level approved tool and threshold policy inputs, multi-match findings per event, list and detail review APIs for admins, and auditable evaluation outcomes for startup and small-team workloads  
**Tenant Boundary**: Every evaluation, finding, and policy row is owned by exactly one workspace; workspace identifiers remain route-scoped and validated against active membership; and finding review or policy changes must never read or modify another workspace's data  
**AuthZ Model**: Existing ASP.NET Core Identity cookie-backed authentication remains the only caller identity in Phase 3; accepted event evaluation runs inside the authenticated workspace context established at ingestion time; workspace owners and admins may review findings and manage risk policy inputs; members may not view findings or change policy values  
**Sensitive Data Handling**: Evaluate only already-ingested structured fields and bounded text metadata such as prompt previews and detail values; store only the minimum evidence needed to explain a trigger; reject raw file binaries and large copied payloads from findings; continue using authenticated transport; redact evidence from logs where possible; and defer retention and purge workflows to a later phase  
**Audit & Observability**: Record auditable outcomes for event evaluations, finding creation, finding review access, finding detail access, risk policy reads and updates, and denied policy-change attempts; enrich structured logs with correlation ID, workspace ID, triggering event ID, rule type, severity, and outcome; and add counters for evaluated events, matched rules, created findings, and denied review or policy operations  
**API / Contract Strategy**: Keep Phase 2 ingestion contracts unchanged, add workspace-scoped JSON endpoints under `/workspaces/{workspaceId}/risk-findings` and `/workspaces/{workspaceId}/risk-policy`, use stable enums for rule types, severities, and statuses, document ProblemDetails failures, and keep finding status read-only as `open` in this phase while retaining an explicit field for forward-compatible filtering

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

Gate status: PASS. The feature delivers backend-only governance value, defines
new review and policy contracts, keeps evaluation strictly workspace-scoped,
uses deterministic explainable rules, and includes auditing plus testing for the
new sensitive flows.

Post-design re-check: PASS after producing `research.md`, `data-model.md`,
`contracts/risk-detection.openapi.yaml`, and `quickstart.md`.

## Project Structure

### Documentation (this feature)

```text
specs/004-risk-detection-engine/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── risk-detection.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── AIUsageGuard.Api/
│   ├── Controllers/
│   │   ├── AIUsageEventsController.cs
│   │   ├── RiskFindingsController.cs
│   │   └── RiskPolicyController.cs
│   ├── Contracts/
│   │   ├── AIUsageEvents/
│   │   └── RiskDetection/
│   ├── Policies/
│   └── Program.cs
├── AIUsageGuard.Application/
│   ├── AIUsageEvents/
│   ├── RiskDetection/
│   │   ├── EvaluateEvent/
│   │   ├── ListFindings/
│   │   ├── GetFinding/
│   │   ├── GetRiskPolicy/
│   │   └── UpdateRiskPolicy/
│   ├── Auditing/
│   └── Abstractions/
├── AIUsageGuard.Domain/
│   ├── AIUsageEvents/
│   ├── RiskDetection/
│   ├── Memberships/
│   └── Workspaces/
└── AIUsageGuard.Infrastructure/
    ├── Auditing/
    ├── Persistence/
    │   ├── Configurations/
    │   └── Migrations/
    └── Tenancy/

tests/
├── AIUsageGuard.UnitTests/
│   └── RiskDetection/
└── AIUsageGuard.IntegrationTests/
    └── RiskDetection/
```

**Structure Decision**: Keep the existing modular monolith and add a
`RiskDetection` slice across API, application, domain, infrastructure, and test
projects. This preserves the Phase 1 and Phase 2 tenant boundary patterns,
keeps event ingestion and risk evaluation in one transactional backend flow, and
avoids premature service decomposition while adding the audit and policy
behavior required for explainable governance.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitutional or architectural exceptions are required for the Phase 3 scope | N/A |
