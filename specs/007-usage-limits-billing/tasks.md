# Tasks: Phase 6 Usage Limits and Billing-Ready Design

**Input**: Design documents from `/specs/007-usage-limits-billing/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for this feature. This task list keeps
tests explicit and front-loaded so limit evaluation, tenant boundaries,
billing-cycle history, contract stability, and audit behavior can be verified
in small slices.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this belongs to (e.g. `US1`, `US2`, `US3`)
- Include exact file paths in descriptions

## Path Conventions

- Solution root: `AIUsageGuard.slnx`
- Application code: `src/AIUsageGuard.Api/`, `src/AIUsageGuard.Application/`, `src/AIUsageGuard.Infrastructure/`
- Tests: `tests/AIUsageGuard.UnitTests/`, `tests/AIUsageGuard.IntegrationTests/`
- Feature docs: `specs/007-usage-limits-billing/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Phase 6 billing and usage-limits surface area plus
manual verification entry points before deeper implementation begins

- [X] T001 Create the billing feature folders under `src/AIUsageGuard.Api/Contracts/Billing/`, `src/AIUsageGuard.Application/Billing/GetPlanStatus/`, `src/AIUsageGuard.Application/Billing/ListBillingCycles/`, `src/AIUsageGuard.Application/Billing/GetBillingCycle/`, `src/AIUsageGuard.Application/Billing/ApplyWorkspacePlanAssignment/`, `src/AIUsageGuard.Application/Billing/ReconcileUsageCycles/`, `src/AIUsageGuard.Infrastructure/Billing/`, `tests/AIUsageGuard.UnitTests/Billing/`, and `tests/AIUsageGuard.IntegrationTests/Billing/`
- [X] T002 [P] Add a "Billing" manual request section to `src/AIUsageGuard.Api/AIUsageGuard.Api.http`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared billing models, durable persistence, and
evaluation primitives required by all user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create the shared billing enums in `src/AIUsageGuard.Application/Models/BillingDimension.cs`, `src/AIUsageGuard.Application/Models/BillingLimitBehavior.cs`, `src/AIUsageGuard.Application/Models/UsageCycleStatus.cs`, `src/AIUsageGuard.Application/Models/UsageCycleMetricState.cs`, `src/AIUsageGuard.Application/Models/LimitEventType.cs`, and `src/AIUsageGuard.Application/Models/CycleAdjustmentType.cs`
- [X] T004 [P] Create the shared billing models in `src/AIUsageGuard.Application/Models/PlanDefinition.cs`, `src/AIUsageGuard.Application/Models/PlanLimitRule.cs`, `src/AIUsageGuard.Application/Models/WorkspacePlanAssignment.cs`, `src/AIUsageGuard.Application/Models/UsageCycle.cs`, `src/AIUsageGuard.Application/Models/UsageCycleMetric.cs`, `src/AIUsageGuard.Application/Models/LimitEvent.cs`, `src/AIUsageGuard.Application/Models/CycleAdjustment.cs`, `src/AIUsageGuard.Application/Models/PlanStatusSnapshot.cs`, and `src/AIUsageGuard.Application/Models/BillingCycleSummary.cs`
- [X] T005 [P] Create billing workflow models and options in `src/AIUsageGuard.Application/Billing/ApplyWorkspacePlanAssignment/ApplyWorkspacePlanAssignmentCommand.cs`, `src/AIUsageGuard.Application/Billing/ApplyWorkspacePlanAssignment/ApplyWorkspacePlanAssignmentResult.cs`, `src/AIUsageGuard.Application/Billing/ReconcileUsageCycles/ReconcileUsageCyclesCommand.cs`, `src/AIUsageGuard.Application/Billing/ReconcileUsageCycles/ReconcileUsageCyclesResult.cs`, and `src/AIUsageGuard.Application/Billing/BillingReconciliationOptions.cs`
- [X] T006 Extend the billing read/write contract in `src/AIUsageGuard.Application/Abstractions/IPlatformStore.cs` with plan catalog, assignment, usage-cycle, limit-event, and adjustment methods
- [X] T007 Implement billing persistence and query helpers in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T008 Add billing entity configurations and migration updates in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/PlanDefinitionConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/PlanLimitRuleConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/WorkspacePlanAssignmentConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/UsageCycleConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/UsageCycleMetricConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/LimitEventConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/CycleAdjustmentConfiguration.cs`, and `src/AIUsageGuard.Infrastructure/Persistence/Migrations/`
- [X] T009 Create shared billing calculation primitives in `src/AIUsageGuard.Application/Billing/BillingDimensionCounter.cs` and `src/AIUsageGuard.Application/Billing/BillingLimitEvaluator.cs`
- [X] T010 Create default plan seeding and cycle factory helpers in `src/AIUsageGuard.Infrastructure/Billing/DefaultPlanCatalogSeeder.cs` and `src/AIUsageGuard.Infrastructure/Billing/UsageCycleFactory.cs`
- [X] T011 Implement the billing background worker shell in `src/AIUsageGuard.Infrastructure/BackgroundProcessing/BillingCycleBackgroundWorker.cs`
- [X] T012 Add billing authorization action mapping in `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`
- [X] T013 Register billing services, default seeding, hosted reconciliation worker, and baseline observability in `src/AIUsageGuard.Api/Program.cs`

**Checkpoint**: Foundation ready - plan status, limit enforcement, and
billing-cycle history can now be implemented

---

## Phase 3: User Story 1 - Understand Current Plan Status (Priority: P1) 🎯 MVP

**Goal**: Allow workspace owners and admins to read the active plan, current
cycle dates, included allowances, current usage, remaining capacity, and
current warning or limit state for their workspace

**Independent Test**: An authorized workspace owner or admin requests current
plan status and receives the correct workspace-scoped plan, cycle, and
dimension summaries, while member and cross-workspace access attempts are
denied

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T014 [P] [US1] Create plan-status projection unit tests in `tests/AIUsageGuard.UnitTests/Billing/GetPlanStatusServiceTests.cs`
- [ ] T015 [P] [US1] Create plan-status API contract tests in `tests/AIUsageGuard.IntegrationTests/Billing/GetPlanStatusContractTests.cs`
- [ ] T016 [P] [US1] Create plan-status integration tests in `tests/AIUsageGuard.IntegrationTests/Billing/PlanStatusTests.cs`
- [ ] T017 [P] [US1] Create plan-status authorization and cross-workspace tests in `tests/AIUsageGuard.IntegrationTests/Billing/PlanStatusAuthorizationTests.cs`

### Implementation for User Story 1

- [X] T018 [P] [US1] Create plan-status request and response contracts in `src/AIUsageGuard.Api/Contracts/Billing/PlanStatusResponse.cs`, `src/AIUsageGuard.Api/Contracts/Billing/PlanAssignmentSummaryResponse.cs`, `src/AIUsageGuard.Api/Contracts/Billing/CurrentCycleSummaryResponse.cs`, and `src/AIUsageGuard.Api/Contracts/Billing/PlanMetricStatusResponse.cs`
- [X] T019 [P] [US1] Create plan-status query and result models in `src/AIUsageGuard.Application/Billing/GetPlanStatus/GetPlanStatusQuery.cs` and `src/AIUsageGuard.Application/Billing/GetPlanStatus/GetPlanStatusResult.cs`
- [X] T020 [US1] Implement workspace plan-status retrieval and audit recording in `src/AIUsageGuard.Application/Billing/GetPlanStatus/GetPlanStatusService.cs`
- [X] T021 [US1] Create billing response mapping in `src/AIUsageGuard.Api/Contracts/Billing/BillingResponseFactory.cs`
- [X] T022 [US1] Implement `GET /workspaces/{workspaceId}/billing/plan-status` in `src/AIUsageGuard.Api/Controllers/BillingController.cs`
- [X] T023 [US1] Extend billing integration-test helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 1 is complete when an admin can inspect one
workspace's current plan status through a stable API with correct tenant and
authorization behavior

---

## Phase 4: User Story 2 - Apply Warnings, Overage, or Restrictions at Limit Boundaries (Priority: P2)

**Goal**: Enforce supported warning, overage, and restriction behavior as
counted membership and AI-usage activity moves across plan thresholds

**Independent Test**: A workspace generates counted membership or AI-usage
activity and the system correctly allows, warns, records overage, or rejects
protected writes based on the active plan while preserving auditability and
deduplicated limit events

### Tests for User Story 2 ⚠️

- [ ] T024 [P] [US2] Create billing limit-evaluator unit tests in `tests/AIUsageGuard.UnitTests/Billing/BillingLimitEvaluatorTests.cs`
- [ ] T025 [P] [US2] Create plan-assignment and cycle-rollover unit tests in `tests/AIUsageGuard.UnitTests/Billing/ApplyWorkspacePlanAssignmentServiceTests.cs`
- [ ] T026 [P] [US2] Create membership and AI-usage limit-enforcement integration tests in `tests/AIUsageGuard.IntegrationTests/Billing/BillingLimitEnforcementTests.cs`
- [ ] T027 [P] [US2] Create warning, overage, and restriction transition integration tests in `tests/AIUsageGuard.IntegrationTests/Billing/BillingOverageAndRestrictionTests.cs`
- [ ] T028 [P] [US2] Create cycle-reconciliation and late-activity integration tests in `tests/AIUsageGuard.IntegrationTests/Billing/BillingCycleReconciliationTests.cs`

### Implementation for User Story 2

- [X] T029 [P] [US2] Create internal plan-assignment workflow models in `src/AIUsageGuard.Application/Billing/ApplyWorkspacePlanAssignment/ApplyWorkspacePlanAssignmentService.cs` and `src/AIUsageGuard.Infrastructure/Billing/DefaultPlanCatalogSeeder.cs`
- [X] T030 [US2] Implement plan assignment, cycle opening, and cycle-boundary plan activation in `src/AIUsageGuard.Application/Billing/ApplyWorkspacePlanAssignment/ApplyWorkspacePlanAssignmentService.cs`
- [X] T031 [US2] Implement synchronous active-member limit evaluation in `src/AIUsageGuard.Application/Memberships/CreateMembership/CreateMembershipService.cs` and `src/AIUsageGuard.Application/Memberships/UpdateMembership/UpdateMembershipService.cs`
- [X] T032 [US2] Implement synchronous activity-volume and estimated-spend limit evaluation in `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventService.cs`
- [X] T033 [US2] Implement limit-event persistence, audit recording, and deduplication in `src/AIUsageGuard.Application/Billing/BillingLimitEvaluator.cs`
- [X] T034 [US2] Implement cycle reconciliation and post-cycle adjustment handling in `src/AIUsageGuard.Application/Billing/ReconcileUsageCycles/ReconcileUsageCyclesService.cs`
- [X] T035 [US2] Wire billing reconciliation scheduling and structured logging in `src/AIUsageGuard.Infrastructure/BackgroundProcessing/BillingCycleBackgroundWorker.cs` and `src/AIUsageGuard.Api/Program.cs`

**Checkpoint**: User Story 2 is complete when protected writes honor plan
behavior, overage is tracked where allowed, restricted actions are blocked, and
late activity produces auditable cycle adjustments

---

## Phase 5: User Story 3 - Review Billing-Ready Usage Cycles (Priority: P3)

**Goal**: Allow workspace owners and admins to review current and prior monthly
cycle summaries, limit events, and post-cycle adjustments for their workspace

**Independent Test**: An authorized workspace owner or admin lists billing
cycles and opens one cycle detail to inspect plan context, per-dimension
totals, limit events, and adjustments, while unauthorized and cross-workspace
access attempts are denied

### Tests for User Story 3 ⚠️

- [ ] T036 [P] [US3] Create billing-cycle list unit tests in `tests/AIUsageGuard.UnitTests/Billing/ListBillingCyclesServiceTests.cs`
- [ ] T037 [P] [US3] Create billing-cycle detail unit tests in `tests/AIUsageGuard.UnitTests/Billing/GetBillingCycleServiceTests.cs`
- [ ] T038 [P] [US3] Create billing-cycle API contract tests in `tests/AIUsageGuard.IntegrationTests/Billing/BillingCycleContractTests.cs`
- [ ] T039 [P] [US3] Create billing-cycle history integration tests in `tests/AIUsageGuard.IntegrationTests/Billing/BillingCycleHistoryTests.cs`
- [ ] T040 [P] [US3] Create billing-cycle authorization and isolation tests in `tests/AIUsageGuard.IntegrationTests/Billing/BillingCycleAuthorizationTests.cs`

### Implementation for User Story 3

- [X] T041 [P] [US3] Create billing-cycle request and response contracts in `src/AIUsageGuard.Api/Contracts/Billing/BillingCycleListResponse.cs`, `src/AIUsageGuard.Api/Contracts/Billing/BillingCycleSummaryResponse.cs`, `src/AIUsageGuard.Api/Contracts/Billing/BillingCycleDetailResponse.cs`, `src/AIUsageGuard.Api/Contracts/Billing/BillingCycleMetricResponse.cs`, `src/AIUsageGuard.Api/Contracts/Billing/LimitEventResponse.cs`, and `src/AIUsageGuard.Api/Contracts/Billing/CycleAdjustmentResponse.cs`
- [X] T042 [P] [US3] Create billing-cycle query and result models in `src/AIUsageGuard.Application/Billing/ListBillingCycles/ListBillingCyclesQuery.cs`, `src/AIUsageGuard.Application/Billing/ListBillingCycles/ListBillingCyclesResult.cs`, `src/AIUsageGuard.Application/Billing/GetBillingCycle/GetBillingCycleQuery.cs`, and `src/AIUsageGuard.Application/Billing/GetBillingCycle/GetBillingCycleResult.cs`
- [X] T043 [US3] Implement billing-cycle list and detail read services in `src/AIUsageGuard.Application/Billing/ListBillingCycles/ListBillingCyclesService.cs` and `src/AIUsageGuard.Application/Billing/GetBillingCycle/GetBillingCycleService.cs`
- [X] T044 [US3] Implement `GET /workspaces/{workspaceId}/billing/cycles` and `GET /workspaces/{workspaceId}/billing/cycles/{cycleId}` in `src/AIUsageGuard.Api/Controllers/BillingController.cs`
- [X] T045 [US3] Extend billing integration-test helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 3 is complete when admins can inspect workspace-only
billing-cycle history and adjustment evidence through stable APIs

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish regression safety, telemetry, contract alignment, and
manual verification coverage across all billing stories

- [ ] T046 [P] Add cross-story billing regression coverage in `tests/AIUsageGuard.IntegrationTests/Billing/BillingRegressionTests.cs`
- [X] T047 [P] Refine structured logging, counters, and failure responses in `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Application/Billing/GetPlanStatus/GetPlanStatusService.cs`, `src/AIUsageGuard.Application/Billing/ReconcileUsageCycles/ReconcileUsageCyclesService.cs`, `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventService.cs`, and `src/AIUsageGuard.Application/Memberships/CreateMembership/CreateMembershipService.cs`
- [X] T048 [P] Align final OpenAPI examples and failure responses in `specs/007-usage-limits-billing/contracts/billing.openapi.yaml`
- [X] T049 [P] Update the manual verification requests in `src/AIUsageGuard.Api/AIUsageGuard.Api.http`
- [X] T050 Run the quickstart validation pass and capture any wording fixes in `specs/007-usage-limits-billing/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion and is the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and is easiest after User Story 1 because the plan-status read path is the clearest way to validate warning, overage, and restriction state
- **User Story 3 (Phase 5)**: Depends on Foundational completion and is easiest after User Story 2 because cycle history is most valuable once enforcement and reconciliation are already producing durable cycle events
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational and establishes the first working billing surface
- **User Story 2 (P2)**: Can start after Foundational, but is easiest after User Story 1 because plan-status APIs make limit transitions easier to verify
- **User Story 3 (P3)**: Can start after Foundational, but its full product value depends on the durable limit-event and adjustment data produced by User Story 2

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Contracts and query models before service logic
- Shared persistence and evaluation behavior before controllers or history views that depend on it
- Audit, authorization, and observability work before story sign-off
- Cheaper models should execute one unchecked task at a time and avoid combining tasks that touch the same file

### Parallel Opportunities

- T002 can run in parallel with T001
- T004 and T005 can run in parallel after T003
- T014, T015, T016, and T017 can run in parallel
- T018 and T019 can run in parallel
- T024, T025, T026, T027, and T028 can run in parallel
- T036, T037, T038, T039, and T040 can run in parallel
- T041 and T042 can run in parallel
- T046, T047, T048, and T049 can run in parallel

---

## Parallel Example: User Story 1

```text
Task: "Create plan-status projection unit tests in tests/AIUsageGuard.UnitTests/Billing/GetPlanStatusServiceTests.cs"
Task: "Create plan-status API contract tests in tests/AIUsageGuard.IntegrationTests/Billing/GetPlanStatusContractTests.cs"
Task: "Create plan-status integration tests in tests/AIUsageGuard.IntegrationTests/Billing/PlanStatusTests.cs"
Task: "Create plan-status authorization and cross-workspace tests in tests/AIUsageGuard.IntegrationTests/Billing/PlanStatusAuthorizationTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Create billing limit-evaluator unit tests in tests/AIUsageGuard.UnitTests/Billing/BillingLimitEvaluatorTests.cs"
Task: "Create plan-assignment and cycle-rollover unit tests in tests/AIUsageGuard.UnitTests/Billing/ApplyWorkspacePlanAssignmentServiceTests.cs"
Task: "Create membership and AI-usage limit-enforcement integration tests in tests/AIUsageGuard.IntegrationTests/Billing/BillingLimitEnforcementTests.cs"
Task: "Create warning, overage, and restriction transition integration tests in tests/AIUsageGuard.IntegrationTests/Billing/BillingOverageAndRestrictionTests.cs"
Task: "Create cycle-reconciliation and late-activity integration tests in tests/AIUsageGuard.IntegrationTests/Billing/BillingCycleReconciliationTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "Create billing-cycle list unit tests in tests/AIUsageGuard.UnitTests/Billing/ListBillingCyclesServiceTests.cs"
Task: "Create billing-cycle detail unit tests in tests/AIUsageGuard.UnitTests/Billing/GetBillingCycleServiceTests.cs"
Task: "Create billing-cycle API contract tests in tests/AIUsageGuard.IntegrationTests/Billing/BillingCycleContractTests.cs"
Task: "Create billing-cycle history integration tests in tests/AIUsageGuard.IntegrationTests/Billing/BillingCycleHistoryTests.cs"
Task: "Create billing-cycle authorization and isolation tests in tests/AIUsageGuard.IntegrationTests/Billing/BillingCycleAuthorizationTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm an admin can read one workspace's current
   plan, cycle, and limit status through the billing API

### Incremental Delivery

1. Build the shared billing models, persistence, evaluation helpers, and worker foundation
2. Deliver current plan-status reads for workspace admins
3. Deliver warning, overage, restriction, and reconciliation behavior on counted writes
4. Deliver billing-cycle history and detail APIs for admins
5. Finish with regression coverage, telemetry, contract cleanup, and quickstart validation

### Parallel Team Strategy

1. One developer handles shared billing models, persistence, and hosted-worker plumbing
2. One developer handles plan-status APIs and tests
3. One developer handles limit enforcement and reconciliation
4. After the foundation is stable, one developer handles billing-cycle history APIs while shared files such as `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`, `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Api/Controllers/BillingController.cs`, and `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs` stay sequential

---

## Notes

- The tasks are intentionally small and file-scoped so a cheaper LLM can implement them one by one without broad architectural inference
- User Story 1 is the recommended MVP scope
- Do not start User Story 2 or User Story 3 before the Foundational phase is complete
- If a task touches the same file as an unfinished earlier task, finish the earlier task first instead of parallelizing
