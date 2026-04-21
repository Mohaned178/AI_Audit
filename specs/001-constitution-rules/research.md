# Research: Phase 0 Constitution and Project Rules

## Decision 1: Adopt .NET 10 LTS as the project baseline

- **Decision**: Use `.NET 10` and ASP.NET Core `10` as the default stack for new
  production work in later phases.
- **Rationale**: The project is greenfield, the user chose a `.NET` stack, and
  current official support guidance lists `.NET 10` as the active LTS release,
  which reduces upgrade churn during the staged roadmap.
- **Alternatives considered**:
  - `.NET 9`: rejected because it is STS and shortens the support window for a
    multi-phase portfolio project.
  - `.NET 8`: rejected because it is older and there is no repository constraint
    requiring it.

## Decision 2: Use a modular monolith rather than microservices

- **Decision**: Plan the future backend as a modular monolith with explicit
  domain, application, infrastructure, and host boundaries.
- **Rationale**: The constitution explicitly prohibits premature microservice
  decomposition, and the roadmap benefits more from clear module boundaries,
  simpler local development, and easier audit of tenant and policy behavior.
- **Alternatives considered**:
  - Microservices from Phase 1: rejected because they add deployment and
    integration overhead before the product surface is proven.
  - Single project without internal boundaries: rejected because it weakens
    traceability and separation of concerns for a governance-heavy SaaS.

## Decision 3: Use controller-based Web APIs as the default external interface

- **Decision**: Treat controller-based ASP.NET Core Web APIs as the default for
  public and admin-facing HTTP endpoints in later phases.
- **Rationale**: The planned surface includes authentication, workspace
  management, event ingestion, reporting, alerts, and policy workflows. That
  breadth favors mature controller conventions, filters, attribute routing, and
  ProblemDetails-friendly API behaviors over ad hoc growth in Minimal APIs.
- **Alternatives considered**:
  - Minimal APIs everywhere: rejected as the default because the surface is
    expected to grow into a richer administrative API.
  - An interactive client as the default delivery surface: rejected because the
    project is explicitly backend-first and Phase 0 excludes client work.

## Decision 4: Keep Phase 0 runtime-free and model it as document contracts

- **Decision**: Phase 0 will not introduce executable runtime services; its
  deliverables are governance artifacts, template alignment, and validation
  instructions.
- **Rationale**: The roadmap defines Phase 0 as constitution and project rules
  only. Treating these as document contracts keeps the planning honest and
  avoids inventing endpoints or storage flows that belong to later phases.
- **Alternatives considered**:
  - Creating placeholder APIs or projects in Phase 0: rejected because the spec
    scopes this phase to governance, not application behavior.
  - Skipping contracts entirely: rejected because the amendment workflow and
    template obligations still need a documented interface.

## Decision 5: Use proportionate validation for Phase 0 and layered automated
testing in later phases

- **Decision**: Validate Phase 0 through artifact review, checklist completion,
  and template consistency; use `xUnit` and `WebApplicationFactory` for future
  runtime phases.
- **Rationale**: Phase 0 has no executable runtime behavior, so automated HTTP or
  domain tests would be artificial. Later phases will need layered testing to
  verify routing, dependency injection, authorization, tenant isolation, and
  policy outcomes.
- **Alternatives considered**:
  - Requiring automated runtime tests in Phase 0: rejected because there is no
    runtime surface yet.
  - Relying only on manual testing in later phases: rejected because it would
    violate the constitution's testability principle.

## Sources

- Microsoft Learn: `.NET` releases and support policy confirms `.NET 10` is the
  active LTS release and `.NET 9` is STS.
- ASP.NET Core guidance: current stack-selection and API guidance favor the
  latest stable ASP.NET Core for greenfield work and recommend choosing
  controllers when the API surface is broad and convention-heavy.
