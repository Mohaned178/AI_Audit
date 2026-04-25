# Research: Phase 3 Risk Detection Engine

## Decision 1: Evaluate risk rules synchronously when a Phase 2 event is accepted

- **Decision**: Run risk evaluation immediately after a valid AI usage event is
  accepted and persisted, inside the same backend request flow.
- **Rationale**: The feature success criteria require prompt findings, the rule
  set is intentionally simple and deterministic, and synchronous evaluation
  avoids introducing background-job infrastructure before Phase 5.
- **Alternatives considered**:
  - Deferred queue-based evaluation: rejected because it adds operational
    complexity and delay before notifications or job infrastructure are in
    scope.
  - Periodic batch scans: rejected because it weakens the immediacy of the risk
    signal and makes audit timing harder to explain.

## Decision 2: Use a fixed built-in rule catalog rather than user-authored rules

- **Decision**: Implement the Phase 3 rules as a fixed, versioned catalog of
  built-in evaluators for sensitive data patterns, file uploads, unapproved
  tool usage, and cost threshold exceedance.
- **Rationale**: The project plan explicitly calls for a simple rule engine and
  excludes complex policy language. A fixed catalog is easier to test,
  document, version, and explain.
- **Alternatives considered**:
  - User-defined rule expressions: rejected because it expands scope into
    parsing, validation, and sandboxing concerns that are not needed for this
    phase.
  - ML or LLM-based classification: rejected by constitution and project scope
    because it reduces determinism and explainability.

## Decision 3: Persist both findings and evaluation outcomes

- **Decision**: Store a deduplicated `RiskFinding` for every matched rule and a
  `RiskEvaluationOutcome` summary for each evaluated event.
- **Rationale**: The spec requires traceability for both risky and non-risky
  events. Findings alone cannot distinguish "no match" from "not evaluated."
- **Alternatives considered**:
  - Findings only: rejected because it leaves no durable explanation for clean
    events or skipped rules.
  - Audit log only: rejected because review APIs need queryable domain data,
    not just append-only audit records.

## Decision 4: Keep finding review read-only in Phase 3

- **Decision**: Findings are created with a read-only `open` status in this
  phase, and the review API supports status filtering for forward compatibility
  without introducing acknowledgment or resolution workflows yet.
- **Rationale**: The feature specification centers on detection, explanation,
  and review. Manual finding lifecycle management would add extra workflows
  without increasing core governance value for this phase.
- **Alternatives considered**:
  - Add resolve or dismiss actions now: rejected because it increases API,
    audit, and lifecycle scope beyond the requested phase.
  - Remove status entirely: rejected because the spec requires filtering by
    finding status and later phases will likely extend the lifecycle.

## Decision 5: Scope workspace policy inputs to approved tools and cost thresholds

- **Decision**: Manage a workspace risk policy with approved tool identifiers,
  a per-event estimated-cost threshold, and a rolling daily workspace estimated
  cost threshold.
- **Rationale**: These inputs satisfy the spec's need for unapproved tool and
  threshold or budget exceedance rules while staying narrow and using fields
  already available in Phase 2 events.
- **Alternatives considered**:
  - Add token thresholds and custom regex configuration: rejected because it
    introduces more configuration surface than the feature requires.
  - Hardcode one global policy for all workspaces: rejected because governance
    must reflect tenant-specific approved tools and limits.

## Decision 6: Apply sensitive-data pattern checks only to bounded text fields already captured

- **Decision**: Run email and phone-pattern checks against bounded text fields
  already stored from Phase 2, specifically prompt previews and key-value
  detail values, and exclude raw file contents or unbounded payloads.
- **Rationale**: This preserves data minimization, avoids expanding storage
  scope, and keeps the detection logic deterministic and testable.
- **Alternatives considered**:
  - Re-ingest or inspect uploaded binaries: rejected because raw file handling
    is out of scope and raises unnecessary sensitive-data exposure.
  - Scan all arbitrary serialized payload content: rejected because it makes
    validation and explanation less predictable.

## Decision 7: Reuse existing workspace admin authorization patterns

- **Decision**: Protect finding review and policy management endpoints with the
  existing workspace-admin authorization model and deny-by-default membership
  checks already used in the API.
- **Rationale**: The current application already differentiates workspace
  members from admins and owners, so Phase 3 can extend that pattern instead of
  inventing a new role model.
- **Alternatives considered**:
  - Add a new compliance-specific role: rejected because the current product
    roles are sufficient for this phase and role expansion is not part of the
    feature scope.
  - Allow all members to review findings: rejected because risk findings are
    governance data and should remain admin-only.
