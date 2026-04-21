# Research: Phase 1 Core SaaS Foundation

## Decision 1: Use .NET 10 LTS and ASP.NET Core 10 as the Phase 1 baseline

- **Decision**: Target `.NET 10` LTS and ASP.NET Core `10` for the SaaS
  foundation implementation.
- **Rationale**: The project is greenfield, the Phase 0 plan already established
  `.NET 10` as the baseline, and the latest stable ASP.NET Core guidance fits
  the long-lived staged roadmap.
- **Alternatives considered**:
  - `.NET 9`: rejected because it is STS and shortens the support window.
  - `.NET 8`: rejected because there is no repository constraint requiring the
    older runtime.

## Decision 2: Use ASP.NET Core Identity for first-party authentication

- **Decision**: Use ASP.NET Core Identity for user registration, sign-in, and
  account persistence in Phase 1.
- **Rationale**: Phase 1 needs first-party user accounts, authenticated sessions,
  and secure account handling. Identity covers these workflows cleanly and keeps
  the phase focused on tenant logic rather than custom credential management.
- **Alternatives considered**:
  - Custom authentication stack: rejected because it adds security risk and
    unnecessary scope.
  - External enterprise identity only: rejected because the initial phase needs a
    self-contained SaaS onboarding path.

## Decision 3: Use PostgreSQL for tenant and identity persistence

- **Decision**: Store workspace, membership, audit, and account data in
  PostgreSQL.
- **Rationale**: PostgreSQL is a strong default for SaaS workloads, fits Linux
  hosting well, supports relational integrity for memberships and roles, and is
  a good foundation for later event and reporting features.
- **Alternatives considered**:
  - SQL Server: rejected because the project is targeting Linux-hosted deployment
    and does not require Microsoft-only infrastructure.
  - SQLite in production: rejected because it is not suitable for the intended
    multi-user SaaS foundation.

## Decision 4: Use controller-based HTTP contracts for all Phase 1 access flows

- **Decision**: Expose onboarding, workspace, and membership flows through
  controller-based HTTP endpoints only, including an endpoint that returns the
  authenticated workspace context.
- **Rationale**: The platform is backend-first and Phase 1 is explicitly API-only.
  Controllers offer stable contracts and mature validation behaviors while still
  proving that an authenticated user reaches the correct workspace context.
- **Alternatives considered**:
  - Minimal APIs everywhere: rejected because the access-control surface is broad
    enough to benefit from controller conventions and richer authorization
    structure.
  - Adding an interactive client in Phase 1: rejected because the current
    project scope is API-only.

## Decision 5: Use policy-based workspace authorization with explicit membership checks

- **Decision**: Apply deny-by-default authorization with workspace policies that
  validate membership and role before protected actions execute.
- **Rationale**: Phase 1 exists to establish tenant boundaries. Authorization must
  happen at the request boundary and use workspace membership as the primary
  access signal.
- **Alternatives considered**:
  - Role checks only inside handlers or services: rejected because it weakens
    boundary enforcement and makes authorization less auditable.
  - Global roles without workspace membership context: rejected because it breaks
    tenant isolation.

## Decision 6: Capture sensitive foundation actions in audit records

- **Decision**: Create audit records for workspace creation, membership changes,
  role changes, and sensitive authorization outcomes.
- **Rationale**: The constitution requires auditability for sensitive actions, and
  later governance features depend on trustworthy identity and access history.
- **Alternatives considered**:
  - Plain application logs only: rejected because they are insufficient for
    governance-grade traceability.
  - Deferring audit records to a later phase: rejected because access-control
    changes are already sensitive in Phase 1.

## Decision 7: Use layered testing with xUnit and WebApplicationFactory

- **Decision**: Test domain and application logic with xUnit and verify end-to-end
  auth, tenant isolation, and controller behavior with `WebApplicationFactory`.
- **Rationale**: This matches current ASP.NET Core guidance and allows realistic
  verification of the request pipeline, dependency injection, authorization, and
  persistence integration.
- **Alternatives considered**:
  - Unit tests only: rejected because tenant isolation and auth behaviors are
    request-pipeline concerns.
  - Browser-only testing: rejected because this phase delivers an API, not a UI.
