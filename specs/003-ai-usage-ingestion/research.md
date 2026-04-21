# Research: Phase 2 AI Usage Event Ingestion

## Decision 1: Extend the existing modular monolith instead of adding a new service boundary

- **Decision**: Implement Phase 2 inside the current API, application, domain,
  and infrastructure projects by adding an `AIUsageEvents` slice.
- **Rationale**: Event ingestion is a direct continuation of the current
  tenant-aware SaaS backend. Reusing the existing workspace resolution,
  authorization, audit service, and persistence approach keeps the feature
  reviewable and avoids distributed-system complexity before it is justified.
- **Alternatives considered**:
  - Separate ingestion service: rejected because the constitution prohibits
    premature service decomposition and the current scope does not need it.
  - Background-only ingestion pipeline: rejected because the phase requires a
    direct API surface and immediate event history retrieval.

## Decision 2: Use existing authenticated workspace members as Phase 2 ingestion callers

- **Decision**: Restrict Phase 2 event submission to authenticated users with an
  active membership in the target workspace; owners and admins can review
  history, while all active members can submit events for their own workspace
  context.
- **Rationale**: The current platform already has first-party user
  authentication and workspace membership checks. Reusing that identity model
  keeps the phase narrow while still satisfying the requirement for trusted
  event sources.
- **Alternatives considered**:
  - Introduce API keys or service principals now: rejected because it expands
    identity scope beyond the current foundation and would delay the core event
    ingestion capability.
  - Allow anonymous submission with a workspace identifier: rejected because it
    breaks deny-by-default authorization and weakens tenant safety.

## Decision 3: Define a fixed event envelope with controlled typed details

- **Decision**: Support the five Phase 2 event types through a shared ingestion
  envelope that always includes `workspaceId`, `idempotencyKey`, `eventType`,
  `occurredAt`, and server-derived actor context, plus bounded optional detail
  fields such as tool name, model name, prompt preview, file metadata, token
  usage, and estimated cost.
- **Rationale**: A common envelope keeps the API contract stable while allowing
  each event type to carry only the details relevant to that activity. It also
  simplifies validation, auditing, and filtering.
- **Alternatives considered**:
  - Separate endpoint per event type: rejected because it fragments the API
    without adding clear value in this early phase.
  - Opaque arbitrary JSON payloads: rejected because they weaken validation and
    make governance behavior harder to explain.

## Decision 4: Use required idempotency keys for deterministic duplicate and replay protection

- **Decision**: Require every event submission to include an idempotency key
  that is unique within a workspace, and treat repeated submissions with the
  same workspace and idempotency key as duplicates.
- **Rationale**: Duplicate protection needs to be deterministic and easy to
  reason about. A required idempotency key gives both the API caller and the
  backend a clear replay contract without relying on fragile payload hashing.
- **Alternatives considered**:
  - Hash the full payload to detect duplicates: rejected because semantically
    equivalent events may differ in inconsequential fields.
  - Accept duplicates and clean them up later: rejected because the feature
    explicitly requires trustworthy event history.

## Decision 5: Store append-only event records with first-class filter columns and stable sort order

- **Decision**: Persist accepted events as append-only records with explicit
  columns for workspace, actor, event type, occurrence time, receipt time,
  tool, and idempotency key, plus a controlled details payload for the remaining
  event-specific attributes. History retrieval sorts by `occurredAt` descending
  and then `receivedAt` descending, and supports pagination plus filters for
  date range, event type, actor, and tool.
- **Rationale**: The feature needs both reliable ingestion and immediately useful
  retrieval. First-class filter columns keep the common queries simple, while
  append-only storage preserves an accurate operational history even when events
  arrive late.
- **Alternatives considered**:
  - Fully normalized tables per event type: rejected because it increases schema
    complexity too early.
  - Free-form blob storage only: rejected because it makes filtering and
    explainability harder.

## Decision 6: Minimize sensitive stored content and keep raw binaries out of scope

- **Decision**: Store only structured event metadata and bounded optional text
  previews needed for visibility, exclude raw file binaries and model outputs,
  and redact sensitive request fields from application logs. Phase 2 retains
  event records in the application database until a later retention feature adds
  purge workflows.
- **Rationale**: The constitution requires data minimization and explicit data
  handling rules before implementation. This approach supports governance use
  cases without expanding into document storage or unbounded prompt retention.
- **Alternatives considered**:
  - Persist full raw files or arbitrary payload dumps: rejected because they add
    security and storage risk without being necessary for Phase 2 outcomes.
  - Refuse all prompt or file-related metadata: rejected because the feature
    needs enough detail to support future policy and reporting phases.

## Decision 7: Verify the feature with layered tests and explicit contract examples

- **Decision**: Cover event validation and duplicate logic with unit tests,
  verify ingestion, filtering, tenant isolation, and authorization with
  integration tests, and publish an OpenAPI contract describing the public HTTP
  surface and ProblemDetails failures.
- **Rationale**: Event ingestion correctness depends on request validation,
  persistence, and authorization all working together. Layered tests align with
  the constitution and the repository's existing testing style.
- **Alternatives considered**:
  - Integration tests only: rejected because validation and duplicate rules are
    easier to prove and maintain with focused unit tests.
  - Unit tests only: rejected because tenant isolation and policy behavior are
    request-pipeline concerns.
