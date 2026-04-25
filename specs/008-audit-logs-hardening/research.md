# Research: Phase 7 Audit Logs and Hardening

## Decision 1: Expose workspace audit history through dedicated read APIs over the existing audit store

- **Decision**: Build workspace-scoped audit-history list and detail reads on
  top of the existing audit-record persistence model instead of introducing a
  separate audit warehouse or external evidence store.
- **Rationale**: The product already records audit events across protected
  workflows, so the fastest and most coherent Phase 7 value is to make that
  evidence queryable for workspace operators without splitting audit truth
  across systems.
- **Alternatives considered**:
  - External SIEM-first integration: rejected because it would delay user value
    and add infrastructure outside the current phase scope.
  - Building only raw database access for operators: rejected because it would
    bypass authorization, stable contracts, and user-facing filtering behavior.

## Decision 2: Treat audit evidence as append-only and non-editable through workspace workflows

- **Decision**: Preserve audit records as append-only evidence for normal
  product operation and do not add workspace-facing edit or delete workflows.
- **Rationale**: Investigation value depends on trust in the evidence trail.
  Allowing normal workspace users to alter recorded history would weaken both
  governance credibility and operator confidence.
- **Alternatives considered**:
  - Allowing admins to redact or delete records inline: rejected because it
    conflicts with auditability and would require a separate evidence-governance
    model.
  - Soft-deleting audit records for convenience: rejected because it complicates
    investigation semantics without satisfying the phase's core operator needs.

## Decision 3: Enrich existing audit records with investigation metadata instead of creating a parallel event type

- **Decision**: Extend the current audit record shape with richer
  investigation-friendly metadata such as correlation context, security
  relevance, and minimized client context where needed.
- **Rationale**: The current model already contains actor, tenant, action,
  target, result, and reason. Incremental enrichment preserves compatibility
  with existing features while making audit history useful for real
  investigations.
- **Alternatives considered**:
  - Introducing a second security-event stream beside audit records: rejected
    because it would fragment investigation evidence and duplicate semantics.
  - Leaving the current shape unchanged: rejected because it limits filtering,
    traceability, and operator understanding for security-relevant events.

## Decision 4: Use temporary login lockouts backed by persisted failure tracking for repeated invalid credentials

- **Decision**: Harden authentication-adjacent flows by tracking repeated
  failed sign-in attempts and applying temporary lockouts after supported
  thresholds are exceeded.
- **Rationale**: The current sign-in flow validates credentials but does not
  slow repeated invalid attempts. Temporary lockout is a bounded, explainable
  defense that fits the current cookie-auth model and existing user-account
  state.
- **Alternatives considered**:
  - No login-abuse protection in this phase: rejected because it leaves a clear
    hardening gap in a security-focused phase.
  - Permanent account disablement after failures: rejected because it is too
    punitive and would create unnecessary operator recovery work.

## Decision 5: Add request-integrity protections and secure failure minimization to authenticated write workflows

- **Decision**: Strengthen authenticated write workflows with explicit
  request-integrity validation and minimized failure responses for rejected
  protected actions.
- **Rationale**: Cookie-authenticated APIs need stronger protection than
  authentication alone. Request-integrity checks and conservative failure
  messaging reduce misuse risk without requiring a new identity model.
- **Alternatives considered**:
  - Relying only on authorization checks: rejected because it does not address
    request forgery or malformed authenticated writes.
  - Returning highly specific rejection details for all failures: rejected
    because it can leak protected context to unauthorized callers.

## Decision 6: Keep hardening controls internal and expose only operator-facing audit history in public contracts

- **Decision**: Limit public Phase 7 contracts to audit-history reads while
  keeping hardening decisions embedded in authentication and protected request
  workflows.
- **Rationale**: The user-facing value is audit visibility; hardening is most
  effective when it remains an internal enforcement concern rather than an
  operator-tuned configuration surface in this phase.
- **Alternatives considered**:
  - Exposing hardening policy management APIs now: rejected because the spec
    does not require self-service security policy administration.
  - Hiding audit history and delivering only internal hardening: rejected
    because it would not satisfy the primary operator investigation use case.
