# Feature Specification: Phase 3 Risk Detection Engine

**Feature Branch**: `004-risk-detection-engine`  
**Created**: 2026-04-21  
**Status**: Draft  
**Input**: User description: "read plan.md and create specfication for Phase 3 — Risk Detection Engine"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Detect Risky AI Activity Automatically (Priority: P1)

As a workspace owner or admin, I want the platform to evaluate incoming AI
usage events against simple governance rules so that risky activity is flagged
without waiting for a manual review.

**Why this priority**: Raw event storage is not sufficient for governance. The
first meaningful value in this phase is turning AI usage activity into
workspace-scoped risk signals that operators can act on.

**Independent Test**: A workspace submits AI usage events that match supported
risk conditions, and the system creates linked risk findings with the expected
severity and reason while leaving non-matching events unflagged.

**Acceptance Scenarios**:

1. **Given** a workspace has supported risk rules in effect, **When** a newly
   accepted AI usage event matches one of those rules, **Then** the system
   creates a risk finding for that workspace tied to the triggering event with
   a severity level and human-readable reason.
2. **Given** a newly accepted AI usage event does not match any active risk
   rule, **When** the system evaluates the event, **Then** no risk finding is
   created and the evaluation outcome remains traceable for audit purposes.
3. **Given** a single event matches more than one supported rule, **When** the
   evaluation completes, **Then** the system records each applicable finding
   distinctly so the workspace can understand all triggered concerns.

---

### User Story 2 - Review and Understand Risk Findings (Priority: P2)

As a workspace owner or admin, I want to review risk findings with clear
severity and explanation details so that I can quickly understand what happened
and why the platform considered it risky.

**Why this priority**: Detection without understandable review output creates
little operational value. Administrators need a simple way to inspect findings
and investigate workspace risk trends.

**Independent Test**: An authorized workspace admin retrieves workspace risk
findings, filters them by common investigation criteria, and can identify the
triggering event, severity, and explanation for each result without seeing data
from any other workspace.

**Acceptance Scenarios**:

1. **Given** a workspace has recorded risk findings, **When** an authorized
   owner or admin requests the findings list, **Then** the system returns only
   findings belonging to that workspace.
2. **Given** an authorized owner or admin narrows the view by severity, rule
   type, date range, actor, or tool, **When** the findings are retrieved,
   **Then** the system returns only matching results with stable ordering.
3. **Given** an authorized owner or admin opens a specific finding, **When**
   the detail is shown, **Then** the system explains the matched rule, the
   triggering event context, and the reason for the assigned severity.

---

### User Story 3 - Maintain Simple Workspace Risk Policies (Priority: P3)

As a workspace owner or admin, I want to manage the simple policy inputs that
the supported rules depend on so that risk detection reflects my workspace's
approved tools and basic threshold boundaries.

**Why this priority**: Several planned rules, such as unapproved tool usage or
threshold exceedance, are not trustworthy unless the workspace can define the
baseline they are measured against.

**Independent Test**: An authorized workspace admin updates approved tool and
basic threshold settings, and subsequent matching events are evaluated using the
new workspace policy values.

**Acceptance Scenarios**:

1. **Given** a workspace owner or admin updates the workspace's approved tool
   list, **When** future AI usage events reference a tool outside that list,
   **Then** the system flags the event as unapproved tool usage for that
   workspace.
2. **Given** a workspace owner or admin updates a supported threshold such as a
   spending or usage boundary, **When** a future event causes the workspace to
   exceed that threshold, **Then** the system creates a finding using the
   configured workspace boundary.
3. **Given** a non-admin member attempts to change workspace risk policy
   inputs, **When** the request is evaluated, **Then** the system denies the
   change and records the denied access attempt for audit review.

---

### Edge Cases

- What happens when an event includes too little detail for some supported rules
  to evaluate, but enough detail for other rules to proceed?
- How does the system behave when a single event triggers multiple supported
  rules with different severity levels?
- What happens when a workspace changes its approved tool list or thresholds
  after earlier events were already evaluated?
- How does the system handle late-arriving usage or cost events that push a
  workspace over a threshold after lower-risk activity was already recorded?
- What happens when the same event is processed more than once and would
  otherwise generate duplicate findings for the same rule?

## Security & Governance Considerations *(mandatory)*

### Tenant & Access Boundaries

- Every risk evaluation, risk finding, and policy input must remain scoped to a
  single workspace boundary, and the system must prevent cross-workspace
  creation, retrieval, or modification of findings and policy values.
- Only workspace owners and admins may review risk findings or change workspace
  policy inputs, and the system must block member-level privilege escalation or
  manipulated tenant context during both detection and review flows.

### Sensitive Data Handling

- Risk evaluation may inspect structured AI usage metadata and limited text
  fields already captured by the platform, such as tool identifiers, timestamps,
  prompt previews, file metadata, and estimated cost data.
- The system must minimize what is copied into a finding by storing only the
  evidence needed to explain the trigger, avoiding unnecessary repetition of
  sensitive content or raw file payloads in the risk output.

### Auditability & Policy Impact

- The system must produce auditable records for risk evaluations, finding
  creation, protected review access, and workspace policy changes introduced by
  this phase.
- Workspace operators must be able to understand why the platform flagged an
  event through clear severity labels, matched rule identifiers, and
  human-readable reasons attached to each finding.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST evaluate each newly accepted AI usage event
  against the supported set of simple risk rules within the correct workspace
  context.
- **FR-002**: The supported rule set for this phase MUST include sensitive data
  pattern checks on eligible event text, file upload detection, unapproved tool
  usage detection, and threshold exceedance detection for supported usage or
  cost values.
- **FR-003**: The system MUST create a workspace-scoped risk finding whenever
  an event matches a supported rule.
- **FR-004**: Each risk finding MUST retain, at a minimum, the triggering
  workspace, triggering event reference, matched rule identifier, severity
  level, reason, and detection time.
- **FR-005**: The system MUST support more than one risk finding for a single
  event when multiple supported rules match that event.
- **FR-006**: The system MUST avoid creating duplicate findings for the same
  workspace, triggering event, and matched rule when the same event is
  reprocessed or replayed.
- **FR-007**: The system MUST preserve a traceable evaluation outcome for
  events that do not create findings so workspace operators can distinguish
  between "not risky" and "not evaluated."
- **FR-008**: Authorized workspace owners and admins MUST be able to retrieve
  risk findings for their workspace.
- **FR-009**: The system MUST support basic risk finding filtering by severity,
  rule type, date range, actor, tool, and finding status.
- **FR-010**: The system MUST present each risk finding with a human-readable
  explanation that identifies what triggered the finding and why the assigned
  severity was used.
- **FR-011**: Authorized workspace owners and admins MUST be able to manage the
  simple workspace policy inputs needed for this phase, including approved tool
  lists and supported threshold values.
- **FR-012**: Updated workspace policy inputs MUST affect subsequent event
  evaluations for that workspace.
- **FR-013**: The system MUST enforce workspace-scoped authorization for every
  protected action and data access path introduced by this phase.
- **FR-014**: The system MUST emit audit records for risk evaluations, finding
  creation, protected review access, policy changes, and policy change denials
  introduced by this phase.
- **FR-015**: The system MUST keep machine-learning classification, deep prompt
  understanding, automated enforcement actions, bulk historical rescans, and
  notification delivery out of scope for this phase.

### Key Entities *(include if feature involves data)*

- **Risk Rule**: A supported governance check that evaluates AI usage activity
  against a simple condition such as sensitive data patterns, file uploads,
  unapproved tools, or threshold exceedance.
- **Risk Finding**: A workspace-scoped record created when an AI usage event
  matches a supported rule, including severity, reason, and a link to the
  triggering event.
- **Risk Evaluation Outcome**: The traceable result of checking an AI usage
  event against the active supported rules for its workspace, whether or not a
  finding was created.
- **Workspace Risk Policy**: The set of simple workspace-managed values used by
  supported rules, such as approved tool lists and basic threshold boundaries.
- **Finding Review View**: The workspace-scoped list and detail view used by
  authorized operators to inspect risk findings and understand why they were
  created.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of AI usage events that match supported risk rules produce
  the expected workspace finding within 2 minutes of the event being accepted.
- **SC-002**: 100% of tested cross-workspace attempts to view findings or
  change workspace risk policy inputs are denied.
- **SC-003**: Workspace owners and admins can identify why a finding was
  created, including the triggering event and matched rule, in under 2 minutes
  during acceptance testing.
- **SC-004**: 95% of tested supported rule scenarios return the expected
  severity and reason on the first evaluation attempt.
- **SC-005**: 100% of finding creations, protected review access outcomes, and
  workspace policy changes defined in this phase produce auditable records.

## Assumptions

- Phase 2 event ingestion already provides the structured AI usage events this
  phase evaluates.
- This phase uses simple, deterministic rule checks against stored event data
  and limited text metadata already captured by the platform rather than deep
  content analysis or machine-learning classification.
- Workspace owners and admins are the only users who need to review findings or
  manage workspace risk policy inputs in this phase.
- Workspace policy changes apply to future event evaluations by default; bulk
  reevaluation of historical events can be introduced in a later phase if
  needed.
- Notification delivery, escalation workflows, and advanced reporting remain
  outside the scope of this phase and can build on the findings created here.
