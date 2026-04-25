# Research: Phase 6 Usage Limits and Billing-Ready Design

## Decision 1: Use persisted UTC calendar-month usage cycles

- **Decision**: Track usage in persisted UTC calendar-month cycles with one
  open cycle per workspace at a time and retain closed cycles as durable
  billing-ready history.
- **Rationale**: Calendar-month cycles are easy for operators to understand,
  line up with the existing UTC-oriented reporting model, and avoid the
  ambiguity of rolling windows when explaining warnings, overage, or later
  cycle adjustments.
- **Alternatives considered**:
  - Rolling 30-day windows: rejected because they complicate limit explanations
    and make historical billing summaries harder to reason about.
  - Purely query-time monthly summaries with no persisted cycles: rejected
    because they make adjustments, auditability, and stable historical records
    weaker.

## Decision 2: Make plan changes effective at cycle boundaries

- **Decision**: Support workspace plan assignment changes as current or
  future-dated assignments that become effective only at the next cycle
  boundary, not in the middle of a monthly cycle.
- **Rationale**: Boundary-based activation avoids proration logic, keeps cycle
  interpretation deterministic, and makes later invoicing or pricing work
  simpler because one cycle maps cleanly to one plan assignment snapshot.
- **Alternatives considered**:
  - Immediate mid-cycle plan switches: rejected because they introduce
    proration, retroactive recalculation, and more confusing operator
    explanations.
  - No future scheduling support: rejected because commercial plan changes
    often need to be staged safely ahead of the next billing period.

## Decision 3: Model quotas as per-dimension limit rules with explicit response strategies

- **Decision**: Represent each plan as a set of limit rules by supported
  dimension, with each rule carrying included allowance, warning threshold, and
  a response strategy of `WarnOnly`, `Restrict`, or `AllowOverage`.
- **Rationale**: Phase 6 needs to support different monetization behavior
  across active members, usage volume, and estimated spend without hard-coding
  separate enforcement logic for every plan. A dimension-based rule model keeps
  the engine extensible and the operator explanation consistent.
- **Alternatives considered**:
  - A single global workspace limit state: rejected because it cannot explain
    which dimension triggered the issue or support mixed behaviors.
  - Bespoke hard-coded logic per plan tier: rejected because it would make
    later pricing changes harder and more error-prone.

## Decision 4: Evaluate restricted limits synchronously and reconcile later changes in the background

- **Decision**: Update cycle metrics and enforce restricted limits
  synchronously on protected membership and AI-usage write workflows, while
  using background reconciliation to process late-arriving or corrected source
  activity after the fact.
- **Rationale**: Hard limits must be enforced at the time protected activity is
  attempted, but billing-ready history also needs a safe path for late or
  corrected data. This hybrid model gives immediate enforcement where needed
  and preserves eventual accuracy for cycle history.
- **Alternatives considered**:
  - Background-only limit evaluation: rejected because it would allow protected
    writes to exceed restricted limits before enforcement catches up.
  - Full recalculation on every read or write: rejected because it adds too
    much cost and complexity for startup-scale workloads.

## Decision 5: Reuse existing trusted sources for the three supported quota dimensions

- **Decision**: Derive the first release's quota dimensions from existing
  platform data: active-member count from active workspace memberships, monthly
  activity volume from accepted AI usage events, and estimated spend from the
  sum of accepted event cost estimates.
- **Rationale**: These data sources already exist in the product and are
  aligned with the Phase 6 specification. Reusing them keeps the billing-ready
  design grounded in the system's existing authoritative records.
- **Alternatives considered**:
  - Token-count quotas in the first release: rejected because they complicate
    plan communication and depend on more uneven source completeness.
  - Provider invoice import as the billing source of truth: rejected because
    direct payment-provider integration is out of scope for this phase.

## Decision 6: Keep the public Phase 6 API read-only for workspace operators

- **Decision**: Expose only workspace-scoped read APIs for plan status and
  cycle history in Phase 6, while keeping plan catalog and workspace plan
  assignment mutation as internal administrative workflows backed by
  application services and audited persistence.
- **Rationale**: The current interactive authorization model is workspace-role
  based and does not yet define a platform-admin surface. Read-only workspace
  APIs deliver the user value in the spec without inventing premature
  self-service checkout or global administration endpoints.
- **Alternatives considered**:
  - Self-service checkout and plan changes by workspace admins: rejected
    because payment collection and self-service billing are explicitly out of
    scope.
  - Public platform-admin APIs in this phase: rejected because they would add a
    new authorization surface not yet established in the product.

## Decision 7: Deduplicate limit events by workspace, cycle, dimension, and state transition

- **Decision**: Persist warning, overage, and restriction transitions as
  deduplicated limit events keyed by workspace, cycle, dimension, and the new
  state so the system records only meaningful changes.
- **Rationale**: Protected writes and reconciliation can observe the same limit
  condition more than once. Deduplicated state-transition events keep audit
  history readable, prevent warning spam, and make later notification hooks
  possible without changing the accounting model.
- **Alternatives considered**:
  - Write a new limit event on every evaluation: rejected because it creates
    noisy, redundant history.
  - Rely on logs alone instead of persisted limit events: rejected because
    logs do not provide stable billing-ready evidence or operator-facing cycle
    explanations.
