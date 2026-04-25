# Research: Phase 5 Background Jobs and Notifications

## Decision 1: Use durable database-backed background processing with hosted workers

- **Decision**: Implement Phase 5 automation with ASP.NET Core hosted workers
  that scan for due work and persist job runs, notification records, delivery
  outcomes, and retry state in the primary application database.
- **Rationale**: The current product is a modular monolith with no existing
  scheduler or queue service. A durable database-backed approach fits the
  current deployment model, survives restarts, supports idempotent retries, and
  keeps auditability and workspace scoping inside the existing persistence
  boundary.
- **Alternatives considered**:
  - In-memory timers only: rejected because pending work and retry state would
    be lost on restart and operators could not review durable outcomes.
  - External scheduler or queue infrastructure: rejected because it adds
    operational complexity that is not justified for the current startup-scale
    scope.

## Decision 2: Make email the only outbound notification channel in this phase

- **Decision**: Deliver Phase 5 notifications through one outbound email-style
  channel while also persisting notification history and delivery outcomes for
  API review.
- **Rationale**: The product is backend-first and has no planned frontend
  notification surface. Email provides direct operator value without inventing
  unplanned UI work, while persisted history keeps delivery outcomes visible to
  authorized admins and makes testing feasible with a fake or captured sender.
- **Alternatives considered**:
  - In-app notifications only: rejected because they would depend on a
    frontend/operator surface that is not part of the approved scope.
  - Multiple channels such as SMS, chat, and email: rejected because
    multi-channel orchestration is explicitly out of scope for this phase.

## Decision 3: Limit digest schedules to predictable daily and weekly UTC periods

- **Decision**: Support digest generation for completed daily and weekly UTC
  periods with one active cadence per workspace.
- **Rationale**: Daily and weekly periods cover the operational reporting needs
  described in the roadmap, align cleanly with the date-based reporting model
  introduced in Phase 4, and avoid the complexity of custom calendars or
  arbitrary cron expressions.
- **Alternatives considered**:
  - Fully custom scheduling expressions: rejected because they complicate
    validation, due-work tracking, and operator understanding.
  - Monthly or ad hoc digests in the first release: rejected because they add
    configuration surface without clear incremental value for the current
    scope.

## Decision 4: Deduplicate urgent alerts with a workspace-scoped trigger fingerprint

- **Decision**: Generate urgent notifications from a stable
  workspace-scoped trigger fingerprint derived from the supported underlying
  condition and suppress repeated notifications while that condition remains
  materially unchanged.
- **Rationale**: Background scans and repeated event evaluations can observe the
  same issue multiple times. A persisted fingerprint avoids noisy duplicate
  alerts while preserving a clear trail of the first notification and any later
  change-worthy notifications.
- **Alternatives considered**:
  - No deduplication: rejected because operators would receive repeated alerts
    for unchanged issues.
  - Manual acknowledgement as the only suppression mechanism: rejected because
    it introduces workflow complexity beyond the intended Phase 5 scope.

## Decision 5: Retry failed deliveries with capped exponential backoff and final outcomes

- **Decision**: Persist delivery attempts and retry failed notifications with a
  capped exponential backoff policy that ends in a final delivered, failed, or
  skipped outcome.
- **Rationale**: Temporary delivery failures are common enough to justify
  automatic retries, but infinite retries obscure the operator signal and make
  operational support harder. Persisted retry scheduling keeps the workflow
  observable and deterministic.
- **Alternatives considered**:
  - No automatic retries: rejected because temporary delivery issues would
    reduce notification reliability too sharply.
  - Unlimited retries: rejected because they can hide permanent failures and
    cause unbounded background churn.

## Decision 6: Reuse existing admin authorization and expose only preference/history APIs

- **Decision**: Protect notification preference management and notification
  history review with the existing workspace-admin authorization model, and
  expose only operator APIs for preferences and history rather than manual job
  control endpoints.
- **Rationale**: Owners and admins already represent the intended governance
  operators. Restricting the public API to preference and history management
  keeps the surface area small, preserves deny-by-default behavior, and avoids
  exposing scheduler controls that could blur operational boundaries.
- **Alternatives considered**:
  - Add a new notification-manager role: rejected because the current role
    model already covers the intended operators.
  - Expose manual job trigger endpoints publicly: rejected because background
    processing should remain an internal workflow in this phase.
