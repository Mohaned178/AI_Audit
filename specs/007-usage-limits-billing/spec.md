# Feature Specification: Phase 6 Usage Limits and Billing-Ready Design

**Feature Branch**: `007-usage-limits-billing`  
**Created**: 2026-04-23  
**Status**: Draft  
**Input**: User description: "Read PLAN.md then create specification for Phase 6 Usage Limits and Billing-Ready Design"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand Current Plan Status (Priority: P1)

As a workspace owner or admin, I want to see my workspace's active plan,
included allowances, and current-cycle usage so that I can understand how much
capacity remains before my team hits warnings or restrictions.

**Why this priority**: Operators cannot manage plan-based behavior if they
cannot first understand which plan applies, what is included, and how close the
workspace is to each supported limit.

**Independent Test**: An authorized workspace owner or admin opens the
workspace's plan status and receives the active plan, cycle dates, included
allowances, current usage, remaining capacity, and any warning or limit state
for that workspace only.

**Acceptance Scenarios**:

1. **Given** a workspace has an active plan with tracked usage in the current
   monthly cycle, **When** an authorized owner or admin requests plan status,
   **Then** the system returns that workspace's active plan, cycle dates,
   included allowances, current usage totals, remaining allowances, and current
   warning or enforcement state.
2. **Given** a workspace is nearing one of its supported plan thresholds,
   **When** the owner or admin reviews plan status, **Then** the system clearly
   identifies which allowance is close to exhaustion and what the expected next
   behavior will be if usage continues.
3. **Given** a member without plan-management access or a user from another
   workspace requests plan status, **When** the request is evaluated, **Then**
   the system denies access without exposing protected commercial or usage
   information.

---

### User Story 2 - Apply Warnings, Overage, or Restrictions at Limit Boundaries (Priority: P2)

As a workspace owner or admin, I want the platform to respond predictably when
my workspace approaches or exceeds plan limits so that usage stays aligned with
the workspace's allowed plan behavior.

**Why this priority**: Monetization readiness depends on more than reporting.
The product must also enforce the supported warning, overage, or restriction
behavior attached to each plan.

**Independent Test**: A workspace generates counted activity across supported
quota dimensions, and the system correctly allows, warns, records overage, or
restricts additional activity based on the active plan's configured response.

**Acceptance Scenarios**:

1. **Given** a workspace remains below its warning and hard-limit thresholds,
   **When** new counted AI activity is accepted, **Then** the system updates
   the workspace's current-cycle usage without creating an unnecessary warning
   or restriction.
2. **Given** a workspace reaches a supported warning threshold before a hard
   limit, **When** the next counted activity is processed, **Then** the system
   records a visible warning state for the workspace and still follows the
   active plan's allowed behavior below the hard limit.
3. **Given** a workspace attempts additional counted activity that would exceed
   a restricted hard limit, **When** the request is evaluated, **Then** the
   system prevents the protected activity and returns a clear workspace-scoped
   reason for the restriction.
4. **Given** a workspace is on a plan that allows overage for a supported
   dimension, **When** counted activity continues after the included allowance
   is exhausted, **Then** the system accepts the activity, records the excess
   separately from included usage, and makes that overage visible in plan
   status.

---

### User Story 3 - Review Billing-Ready Usage Cycles (Priority: P3)

As a workspace owner or admin, I want billing-ready monthly usage summaries and
plan history so that I can understand whether the workspace still fits its plan
and prepare for future paid billing workflows.

**Why this priority**: Phase 6 should leave the product ready for later pricing
and payment work. That requires stable cycle-level records, not only live limit
checks.

**Independent Test**: An authorized workspace owner or admin reviews current
and prior monthly cycle summaries and can identify the plan in effect, included
usage, overage-eligible usage, warnings, restrictions, and any later
adjustments for that workspace only.

**Acceptance Scenarios**:

1. **Given** a workspace has an active or recently completed monthly cycle,
   **When** an authorized owner or admin reviews cycle history, **Then** the
   system shows the cycle dates, plan in effect, included usage totals,
   overage totals, warning events, and restriction events for that cycle.
2. **Given** accepted activity arrives late or is corrected after a cycle has
   already been summarized, **When** the affected cycle is viewed again,
   **Then** the system reflects the revised totals and preserves a traceable
   explanation that the cycle summary changed after its initial state.
3. **Given** a member without the required access or a user from another
   workspace requests billing-ready cycle history, **When** access is
   evaluated, **Then** the system denies the request and does not reveal the
   protected plan or usage summary.

---

### Edge Cases

- What happens when duplicate, replayed, or late-arriving accepted usage would
  change a cycle from within-limit to warning, overage, or restricted after the
  fact?
- How does the system behave when a workspace's active plan changes while a
  monthly cycle is in progress or a future plan change is scheduled?
- What happens when a workspace is already above the new plan's included
  allowances at the moment a tighter plan assignment becomes effective?
- How does the system handle supported usage dimensions when some accepted
  activity lacks optional values such as estimated cost?
- What happens when multiple counted activities for the same workspace arrive
  at nearly the same time around a warning or hard-limit boundary?

## Security & Governance Considerations *(mandatory)*

### Tenant & Access Boundaries

- Every plan status view, quota counter, overage record, warning state, cycle
  summary, and plan assignment history item must remain scoped to exactly one
  workspace and must never reveal another workspace's commercial or usage data.
- Only workspace owners and admins may view workspace plan status or billing
  cycle summaries, and only separately authorized administrative actors may
  modify the supported plan catalog or workspace plan assignments.

### Sensitive Data Handling

- This phase may expose workspace plan names, cycle dates, active member
  counts, AI activity totals, estimated spend totals, warning states, overage
  totals, and restriction reasons when needed to explain plan behavior.
- The system must minimize copied AI-related content by relying on aggregated
  usage totals and summary explanations instead of raw prompts, raw files, or
  other unnecessary sensitive event details.

### Auditability & Policy Impact

- The system must produce auditable records for plan assignment changes, plan
  status access, cycle-summary access, warning generation, overage recording,
  restriction decisions, denied access attempts, and post-cycle adjustments.
- Workspace operators must be able to understand why a workspace remained
  within plan, entered warning, incurred overage, or was restricted, and which
  plan and monthly cycle those outcomes applied to.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support a catalog of named plans that define the
  included monthly allowances, per-workspace quotas, warning thresholds, and
  supported limit-response behavior for each plan.
- **FR-002**: The first release of this phase MUST support quotas for active
  workspace members, monthly AI activity volume, and monthly estimated usage
  cost.
- **FR-003**: Every workspace MUST have exactly one plan assignment in effect
  for any point in time, and the system MUST retain the effective period of
  each assignment so prior cycles can be interpreted correctly.
- **FR-004**: The system MUST maintain a monthly usage cycle for each workspace
  and evaluate counted activity against the active plan assignment for that
  cycle.
- **FR-005**: The system MUST keep current-cycle totals for each supported
  usage dimension and distinguish between included usage and overage usage when
  a plan allows overage.
- **FR-006**: Authorized workspace owners and admins MUST be able to view the
  workspace's active plan, cycle dates, included allowances, current-cycle
  usage, remaining allowances, and current warning or restriction state.
- **FR-007**: The system MUST make it clear which supported usage dimension
  caused a warning, overage, or restriction and what threshold or allowance was
  crossed.
- **FR-008**: When a workspace reaches a supported warning threshold, the
  system MUST record a workspace-scoped warning state and make it visible in
  current plan status.
- **FR-009**: When counted activity would exceed a supported hard limit for a
  restricted dimension, the system MUST prevent the protected activity and
  return a clear workspace-scoped explanation of the restriction.
- **FR-010**: When a plan allows overage for a supported dimension, the system
  MUST continue to accept counted activity after the included allowance is
  exhausted while separately recording the overage amount for the workspace and
  cycle.
- **FR-011**: The system MUST avoid duplicate warnings or duplicate unchanged
  restriction events within the same workspace and monthly cycle when the
  underlying limit state has not meaningfully changed.
- **FR-012**: Authorized workspace owners and admins MUST be able to review
  current and prior monthly cycle summaries for their workspace.
- **FR-013**: Each monthly cycle summary MUST identify the plan in effect,
  cycle dates, included usage totals, overage totals, warning events,
  restriction events, and any adjustments applied after the cycle's initial
  summary.
- **FR-014**: When late-arriving accepted activity or a supported correction
  changes a previously summarized cycle, the system MUST preserve a traceable
  record that explains the adjustment.
- **FR-015**: The system MUST preserve plan-assignment history so future
  billing or invoicing workflows can determine which plan applied to each
  workspace during each monthly cycle.
- **FR-016**: The system MUST enforce workspace-scoped authorization for every
  protected action and data access path introduced by this phase.
- **FR-017**: The system MUST emit audit records for plan assignment changes,
  plan status access, cycle-summary access, warning generation, overage
  recording, restriction decisions, denied access attempts, and post-cycle
  adjustments introduced by this phase.
- **FR-018**: The system MUST keep self-service checkout, direct payment
  collection, invoicing, tax calculation, coupons, and dispute handling out of
  scope for this phase.

### Key Entities *(include if feature involves data)*

- **Plan Definition**: A named commercial offering that describes the included
  monthly allowances, per-workspace quotas, warning thresholds, and supported
  response behavior when a workspace approaches or exceeds each limit.
- **Workspace Plan Assignment**: The workspace-scoped record that identifies
  which plan applies during a specific effective period and creates the history
  needed to interpret later cycle summaries.
- **Usage Cycle**: The monthly accounting period for one workspace during which
  counted activity is compared against the active plan's allowances and quota
  rules.
- **Limit Event**: A workspace-scoped warning, overage, or restriction outcome
  created when a usage dimension crosses a meaningful plan threshold during a
  cycle.
- **Billing-Ready Cycle Summary**: The stable cycle-level record that captures
  the plan in effect, counted usage, overage, warnings, restrictions, and any
  later adjustments for future monetization workflows.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of tested plan-status views show the correct active plan,
  cycle dates, and remaining allowances for the requested workspace on the
  first attempt.
- **SC-002**: 100% of tested hard-limit scenarios for restricted dimensions
  prevent additional counted activity after the allowed limit is reached.
- **SC-003**: 95% of tested warning and overage scenarios create the expected
  workspace-visible status within 1 minute of the triggering counted activity.
- **SC-004**: 95% of tested monthly cycle summaries match the workspace's
  counted usage, overage totals, and recorded limit events within the accepted
  adjustment rules for late activity.
- **SC-005**: 100% of tested cross-workspace or unauthorized member attempts
  to view protected plan status or billing-ready cycle summaries are denied.
- **SC-006**: 100% of plan assignment changes, warning events, overage
  recordings, restriction decisions, protected reads, and post-cycle
  adjustments defined in this phase produce auditable records.

## Assumptions

- Phase 1 already provides workspace membership and role boundaries, while
  Phases 2 through 5 already provide the accepted activity, estimated cost
  data, alerts, and background processing needed to evaluate monthly limits and
  summarize cycle usage.
- Plan catalog changes and workspace plan assignments are controlled
  administrative actions in this phase rather than self-service purchase or
  checkout workflows.
- Each workspace follows a monthly accounting cycle with one active plan at a
  time, and the product does not yet need full invoicing or payment
  collection.
- The first release keeps the supported quota dimensions intentionally narrow
  to active members, monthly AI activity volume, and monthly estimated spend.
- If a later phase introduces payment collection or invoicing, it will reuse
  the cycle history and plan-assignment records produced by this phase rather
  than replacing them.
