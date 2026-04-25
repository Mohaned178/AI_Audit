# Research: Phase 4 Reporting and Dashboard APIs

## Decision 1: Compute reporting results on demand from existing event and finding data

- **Decision**: Generate dashboard and reporting responses directly from the
  existing `AIUsageEvent`, `RiskFinding`, `WorkspaceMembership`, and
  `UserAccount` data already persisted by earlier phases instead of adding
  snapshot tables or scheduled materialization jobs.
- **Rationale**: Phase 4 is explicitly about read-only dashboard and reporting
  APIs, while background jobs are deferred to Phase 5. On-demand aggregation
  keeps the architecture simpler, preserves explainability, and avoids
  introducing operational infrastructure early.
- **Alternatives considered**:
  - Precomputed daily snapshot tables: rejected because they require job
    scheduling, backfill logic, and late-arriving data reconciliation before
    those capabilities are in scope.
  - External analytics store: rejected because it introduces cross-system
    consistency and security concerns not justified for the current phase.

## Decision 2: Standardize reporting periods on inclusive UTC calendar dates

- **Decision**: Accept reporting periods as inclusive `fromDate` and `toDate`
  query parameters using UTC calendar dates, then normalize internal event and
  finding queries to those day boundaries.
- **Rationale**: The feature is meant for operational dashboards and simple API
  consumers, so date-based periods are easier to reason about than arbitrary
  timestamps and keep alerts, usage, and cost summaries aligned to a single
  reporting window.
- **Alternatives considered**:
  - Arbitrary `fromOccurredAt` and `toOccurredAt` date-time filters: rejected
    because they complicate trend bucketing and make dashboard comparisons less
    predictable for common day-based reporting.
  - No explicit range validation: rejected because bounded periods are needed
    for predictable performance and safe API behavior.

## Decision 3: Keep reporting outputs aggregated and omit raw AI content

- **Decision**: Return only aggregated counts, grouped usage rows, severity
  breakdowns, trend buckets, user identity references, tool names, and cost
  summaries; do not include prompt previews, file names, file metadata, or
  evidence previews in reporting responses.
- **Rationale**: The specification calls for operational visibility, not event
  inspection. Aggregated answers satisfy the use cases while honoring the
  constitution's data-minimization requirements for sensitive AI telemetry.
- **Alternatives considered**:
  - Include top triggering prompts or evidence snippets in dashboard responses:
    rejected because it increases sensitive-data exposure and duplicates the
    purpose of existing event-history and risk-finding detail APIs.
  - Provide raw export endpoints now: rejected because exports and custom
    reporting are explicitly out of scope.

## Decision 4: Expose a small fixed endpoint set for simple API consumers

- **Decision**: Publish a fixed read-only contract consisting of one dashboard
  overview endpoint and focused report endpoints for usage by user, usage by
  tool, alerts summary, and cost summary.
- **Rationale**: A compact, stable endpoint set is easier to document, test,
  and consume than a generic query language or custom report builder, while
  still covering the feature's required operational questions.
- **Alternatives considered**:
  - Single generic report endpoint with dynamic grouping parameters: rejected
    because validation, authorization, and response shaping become harder to
    reason about and test.
  - Separate endpoint for every metric card: rejected because it would fragment
    the dashboard contract and create avoidable request overhead for consumers.

## Decision 5: Represent cost completeness explicitly

- **Decision**: Include completeness metadata alongside estimated cost totals,
  including counts of events with and without estimated cost values and a
  boolean partial indicator.
- **Rationale**: Phase 2 events permit missing cost data, and the Phase 4 spec
  requires the API to indicate when totals are estimated or partial rather than
  implying exact financial accuracy.
- **Alternatives considered**:
  - Treat missing costs as zero with no warning: rejected because it can hide
    incomplete source data and mislead administrators.
  - Reject reports when some events lack costs: rejected because the feature
    should still provide partial operational value even when upstream data is
    incomplete.

## Decision 6: Reuse the existing workspace-admin authorization model

- **Decision**: Protect all reporting endpoints with the existing
  workspace-admin authorization policy and do not introduce new API-key,
  service-account, or analyst-only roles in this phase.
- **Rationale**: The codebase already enforces deny-by-default admin access for
  governance data, and the specification does not require a new identity model.
  Reusing current roles reduces security surface and keeps the phase narrow.
- **Alternatives considered**:
  - Add a new reporting-specific role: rejected because current owner and admin
    roles already match the intended operators.
  - Allow all workspace members to read reporting: rejected because dashboards
    include governance and cost signals that should remain restricted.

## Decision 7: Use daily trend buckets and ranked top lists

- **Decision**: For dashboard and alerts trend views, aggregate data into daily
  buckets across the requested period and expose ranked top lists for grouped
  user and tool summaries with pagination for larger result sets.
- **Rationale**: Daily buckets are sufficient for the phase's operational
  visibility goals and are easy for simple API consumers to plot. Ranked top
  lists provide actionable insight without requiring unbounded result payloads.
- **Alternatives considered**:
  - Hourly trend buckets: rejected because they add payload volume and
    complexity without being required for the current scope.
  - Full unpaged grouped results: rejected because they do not scale cleanly
    and complicate response-time guarantees.
