# Tasks: Phase 3 Risk Detection Engine

**Input**: Design documents from `/specs/004-risk-detection-engine/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for this feature. This task list keeps tests explicit and front-loaded so a cheaper LLM can work in narrow, verifiable slices without needing to infer hidden quality work.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this belongs to (e.g. `US1`, `US2`, `US3`)
- Include exact file paths in descriptions

## Path Conventions

- Solution root: `AIUsageGuard.slnx`
- Application code: `src/AIUsageGuard.Api/`, `src/AIUsageGuard.Application/`, `src/AIUsageGuard.Domain/`, `src/AIUsageGuard.Infrastructure/`
- Tests: `tests/AIUsageGuard.UnitTests/`, `tests/AIUsageGuard.IntegrationTests/`
- Feature docs: `specs/004-risk-detection-engine/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Phase 3 feature surface area and manual request entry points before deeper implementation begins

- [X] T001 Create the risk detection feature folders under `src/AIUsageGuard.Api/Contracts/RiskDetection/`, `src/AIUsageGuard.Application/RiskDetection/`, `src/AIUsageGuard.Domain/RiskDetection/`, `tests/AIUsageGuard.UnitTests/RiskDetection/`, and `tests/AIUsageGuard.IntegrationTests/RiskDetection/`
- [X] T002 [P] Add a "Risk Detection" manual request section to `src/AIUsageGuard.Api/AIUsageGuard.Api.http`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared risk models, persistence contracts, and storage infrastructure required by all user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create the application risk enums in `src/AIUsageGuard.Application/Models/RiskRuleType.cs`, `src/AIUsageGuard.Application/Models/RiskSeverity.cs`, `src/AIUsageGuard.Application/Models/RiskFindingStatus.cs`, and `src/AIUsageGuard.Application/Models/RiskEvaluationResult.cs`
- [X] T004 [P] Create the application `RiskFinding` model in `src/AIUsageGuard.Application/Models/RiskFinding.cs`
- [X] T005 [P] Create the application `RiskEvaluationOutcome` model in `src/AIUsageGuard.Application/Models/RiskEvaluationOutcome.cs`
- [X] T006 [P] Create the application `WorkspaceRiskPolicy` model in `src/AIUsageGuard.Application/Models/WorkspaceRiskPolicy.cs`
- [X] T007 Create the domain risk enums in `src/AIUsageGuard.Domain/RiskDetection/RiskRuleType.cs`, `src/AIUsageGuard.Domain/RiskDetection/RiskSeverity.cs`, `src/AIUsageGuard.Domain/RiskDetection/RiskFindingStatus.cs`, and `src/AIUsageGuard.Domain/RiskDetection/RiskEvaluationResult.cs`
- [X] T008 [P] Create the domain `RiskFinding` entity in `src/AIUsageGuard.Domain/RiskDetection/RiskFinding.cs`
- [X] T009 [P] Create the domain `RiskEvaluationOutcome` entity in `src/AIUsageGuard.Domain/RiskDetection/RiskEvaluationOutcome.cs`
- [X] T010 [P] Create the domain `WorkspaceRiskPolicy` entity in `src/AIUsageGuard.Domain/RiskDetection/WorkspaceRiskPolicy.cs`
- [X] T011 Extend the platform store contract with risk finding, evaluation outcome, and risk policy read or write methods in `src/AIUsageGuard.Application/Abstractions/IPlatformStore.cs`
- [X] T012 Add the risk detection `DbSet`s, model mappings, and shared store methods in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T013 Create EF Core configurations in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/RiskFindingConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/RiskEvaluationOutcomeConfiguration.cs`, and `src/AIUsageGuard.Infrastructure/Persistence/Configurations/WorkspaceRiskPolicyConfiguration.cs`
- [X] T014 Create the Phase 3 EF Core migration files in `src/AIUsageGuard.Infrastructure/Persistence/Migrations/`

**Checkpoint**: Foundation ready - risk evaluation, finding review, and risk policy stories can now be implemented

---

## Phase 3: User Story 1 - Detect Risky AI Activity Automatically (Priority: P1) 🎯 MVP

**Goal**: Evaluate each accepted AI usage event against the built-in rule catalog and persist deduplicated findings plus evaluation outcomes

**Independent Test**: Submit accepted AI usage events that match and do not match supported rules, then verify the workspace receives the expected findings, severities, reasons, and non-match evaluation outcomes without duplicate findings on replay

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T015 [P] [US1] Create sensitive-data and file-upload rule unit tests in `tests/AIUsageGuard.UnitTests/RiskDetection/BuiltInRiskRuleEvaluatorTests.cs`
- [X] T016 [P] [US1] Create evaluation orchestration unit tests in `tests/AIUsageGuard.UnitTests/RiskDetection/EvaluateAIUsageEventRiskServiceTests.cs`
- [X] T017 [P] [US1] Create ingestion-triggered finding integration tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/CreateRiskFindingsFromEventsTests.cs`
- [X] T018 [P] [US1] Create multi-match and no-match evaluation integration tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/RiskEvaluationOutcomeTests.cs`
- [X] T019 [P] [US1] Create replay and duplicate-finding integration tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/DeduplicateRiskFindingsTests.cs`

### Implementation for User Story 1

- [X] T020 [P] [US1] Create evaluation command and result models in `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/EvaluateAIUsageEventRiskCommand.cs`, `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/EvaluateAIUsageEventRiskResult.cs`, and `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/RiskRuleMatch.cs`
- [X] T021 [P] [US1] Implement sensitive-data pattern evaluation in `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/SensitiveDataPatternRiskEvaluator.cs`
- [X] T022 [P] [US1] Implement file-upload evaluation in `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/FileUploadRiskEvaluator.cs`
- [X] T023 [P] [US1] Implement unapproved-tool evaluation in `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/UnapprovedToolRiskEvaluator.cs`
- [X] T024 [P] [US1] Implement cost-threshold evaluation in `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/CostThresholdRiskEvaluator.cs`
- [X] T025 [US1] Implement evaluation orchestration, deduplicated finding persistence, and outcome persistence in `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/EvaluateAIUsageEventRiskService.cs`
- [X] T026 [US1] Invoke the risk evaluation workflow from `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventService.cs`
- [X] T027 [US1] Add finding-creation and evaluation audit recording in `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/EvaluateAIUsageEventRiskService.cs`
- [X] T028 [US1] Register risk detection services and evaluators in `src/AIUsageGuard.Api/Program.cs`

**Checkpoint**: User Story 1 is complete when accepted AI usage events automatically produce the correct workspace-scoped findings and evaluation outcomes

---

## Phase 4: User Story 2 - Review and Understand Risk Findings (Priority: P2)

**Goal**: Allow workspace owners and admins to retrieve paged risk findings and inspect a single finding with clear severity and explanation details

**Independent Test**: An authorized workspace admin retrieves only their workspace's findings, filters by supported criteria, opens a specific finding, and sees the matched rule, triggering event context, and reason

### Tests for User Story 2 ⚠️

- [X] T029 [P] [US2] Create risk finding list contract tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/ListRiskFindingsContractTests.cs`
- [X] T030 [P] [US2] Create risk finding detail contract tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/GetRiskFindingContractTests.cs`
- [X] T031 [P] [US2] Create list and detail integration tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/ReviewRiskFindingsTests.cs`
- [X] T032 [P] [US2] Create filter, tenant-boundary, and admin-authorization integration tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/RiskFindingsAuthorizationTests.cs`

### Implementation for User Story 2

- [X] T033 [P] [US2] Create finding review request and response contracts in `src/AIUsageGuard.Api/Contracts/RiskDetection/ListRiskFindingsRequest.cs`, `src/AIUsageGuard.Api/Contracts/RiskDetection/RiskFindingListItemResponse.cs`, `src/AIUsageGuard.Api/Contracts/RiskDetection/RiskFindingListResponse.cs`, and `src/AIUsageGuard.Api/Contracts/RiskDetection/RiskFindingDetailResponse.cs`
- [X] T034 [P] [US2] Create list and detail query or result models in `src/AIUsageGuard.Application/RiskDetection/ListFindings/ListRiskFindingsQuery.cs`, `src/AIUsageGuard.Application/RiskDetection/ListFindings/ListRiskFindingsResult.cs`, `src/AIUsageGuard.Application/RiskDetection/GetFinding/GetRiskFindingQuery.cs`, and `src/AIUsageGuard.Application/RiskDetection/GetFinding/GetRiskFindingResult.cs`
- [X] T035 [US2] Implement workspace-scoped finding list and detail store queries in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T036 [US2] Implement finding review services in `src/AIUsageGuard.Application/RiskDetection/ListFindings/ListRiskFindingsService.cs` and `src/AIUsageGuard.Application/RiskDetection/GetFinding/GetRiskFindingService.cs`
- [X] T037 [US2] Implement `GET /workspaces/{workspaceId}/risk-findings` and `GET /workspaces/{workspaceId}/risk-findings/{findingId}` in `src/AIUsageGuard.Api/Controllers/RiskFindingsController.cs`
- [X] T038 [US2] Add finding review audit recording and protected access handling in `src/AIUsageGuard.Application/RiskDetection/ListFindings/ListRiskFindingsService.cs`, `src/AIUsageGuard.Application/RiskDetection/GetFinding/GetRiskFindingService.cs`, and `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`
- [X] T039 [US2] Extend event and finding review helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 2 is complete when admins can investigate only their own workspace findings with stable filters and clear finding details

---

## Phase 5: User Story 3 - Maintain Simple Workspace Risk Policies (Priority: P3)

**Goal**: Allow workspace owners and admins to manage approved tools and threshold settings that affect future risk evaluation

**Independent Test**: An authorized workspace admin updates the workspace risk policy, future events use the new approved-tool and threshold values, and non-admin or cross-workspace callers cannot read or change those policy values

### Tests for User Story 3 ⚠️

- [X] T040 [P] [US3] Create workspace risk policy unit tests in `tests/AIUsageGuard.UnitTests/RiskDetection/WorkspaceRiskPolicyServiceTests.cs`
- [X] T041 [P] [US3] Create risk policy contract tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/WorkspaceRiskPolicyContractTests.cs`
- [X] T042 [P] [US3] Create policy update and future-evaluation integration tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/WorkspaceRiskPolicyIntegrationTests.cs`
- [X] T043 [P] [US3] Create policy authorization and cross-workspace denial tests in `tests/AIUsageGuard.IntegrationTests/RiskDetection/WorkspaceRiskPolicyAuthorizationTests.cs`

### Implementation for User Story 3

- [X] T044 [P] [US3] Create risk policy request and response contracts in `src/AIUsageGuard.Api/Contracts/RiskDetection/UpdateWorkspaceRiskPolicyRequest.cs` and `src/AIUsageGuard.Api/Contracts/RiskDetection/WorkspaceRiskPolicyResponse.cs`
- [X] T045 [P] [US3] Create risk policy query, command, and result models in `src/AIUsageGuard.Application/RiskDetection/GetRiskPolicy/GetWorkspaceRiskPolicyQuery.cs`, `src/AIUsageGuard.Application/RiskDetection/GetRiskPolicy/GetWorkspaceRiskPolicyResult.cs`, `src/AIUsageGuard.Application/RiskDetection/UpdateRiskPolicy/UpdateWorkspaceRiskPolicyCommand.cs`, and `src/AIUsageGuard.Application/RiskDetection/UpdateRiskPolicy/UpdateWorkspaceRiskPolicyResult.cs`
- [X] T046 [US3] Implement workspace risk policy read and write store methods in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T047 [US3] Implement risk policy services in `src/AIUsageGuard.Application/RiskDetection/GetRiskPolicy/GetWorkspaceRiskPolicyService.cs` and `src/AIUsageGuard.Application/RiskDetection/UpdateRiskPolicy/UpdateWorkspaceRiskPolicyService.cs`
- [X] T048 [US3] Implement `GET /workspaces/{workspaceId}/risk-policy` and `PUT /workspaces/{workspaceId}/risk-policy` in `src/AIUsageGuard.Api/Controllers/RiskPolicyController.cs`
- [X] T049 [US3] Apply workspace risk policy values inside `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/UnapprovedToolRiskEvaluator.cs` and `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/CostThresholdRiskEvaluator.cs`
- [X] T050 [US3] Add risk policy read/update audit recording and denied-change audit coverage in `src/AIUsageGuard.Application/RiskDetection/GetRiskPolicy/GetWorkspaceRiskPolicyService.cs`, `src/AIUsageGuard.Application/RiskDetection/UpdateRiskPolicy/UpdateWorkspaceRiskPolicyService.cs`, and `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`
- [X] T051 [US3] Extend `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs` with workspace risk policy helpers

**Checkpoint**: User Story 3 is complete when workspace-specific approved tool and threshold values drive future evaluation and stay protected by admin-only authorization

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish observability, regression safety, and verification artifacts across all stories

- [X] T052 [P] Add cross-story regression coverage in `tests/AIUsageGuard.IntegrationTests/RiskDetection/RiskDetectionRegressionTests.cs`
- [X] T053 [P] Refine structured logging and evaluation counters in `src/AIUsageGuard.Api/Program.cs` and `src/AIUsageGuard.Application/RiskDetection/EvaluateEvent/EvaluateAIUsageEventRiskService.cs`
- [X] T054 [P] Update Phase 3 manual verification requests in `src/AIUsageGuard.Api/AIUsageGuard.Api.http`
- [X] T055 [P] Align final HTTP contract examples and failure responses in `specs/004-risk-detection-engine/contracts/risk-detection.openapi.yaml`
- [X] T056 Run the quickstart validation pass and capture wording fixes in `specs/004-risk-detection-engine/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies, can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion and is the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and is easiest after User Story 1 because it reviews findings created by the evaluation flow
- **User Story 3 (Phase 5)**: Depends on Foundational completion and is easiest after User Story 1 because policy values are consumed by the evaluators built there
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational and establishes the first working risk signal flow
- **User Story 2 (P2)**: Can start after Foundational, but reviewing findings is easier once User Story 1 is already creating them
- **User Story 3 (P3)**: Can start after Foundational, but its full product value depends on the event evaluation flow from User Story 1

### Within Each User Story

- Tests MUST be written and fail before implementation
- Contracts and query models before controller wiring
- Shared evaluator or policy models before service logic
- Store/query changes before controller behavior that depends on them
- Audit and authorization handling before story sign-off
- Cheaper models should execute one unchecked task at a time and avoid combining tasks that touch the same file

### Parallel Opportunities

- T002 can run in parallel with T001
- T004, T005, T006, T008, T009, and T010 can run in parallel after T003 and T007
- T015, T016, T017, T018, and T019 can run in parallel
- T021, T022, T023, and T024 can run in parallel after foundational work
- T029, T030, T031, and T032 can run in parallel
- T033 and T034 can run in parallel
- T040, T041, T042, and T043 can run in parallel
- T044 and T045 can run in parallel
- T052, T053, T054, and T055 can run in parallel

---

## Parallel Example: User Story 1

```text
Task: "Create sensitive-data and file-upload rule unit tests in tests/AIUsageGuard.UnitTests/RiskDetection/BuiltInRiskRuleEvaluatorTests.cs"
Task: "Create evaluation orchestration unit tests in tests/AIUsageGuard.UnitTests/RiskDetection/EvaluateAIUsageEventRiskServiceTests.cs"
Task: "Create ingestion-triggered finding integration tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/CreateRiskFindingsFromEventsTests.cs"
Task: "Create multi-match and no-match evaluation integration tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/RiskEvaluationOutcomeTests.cs"
Task: "Create replay and duplicate-finding integration tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/DeduplicateRiskFindingsTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Create risk finding list contract tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/ListRiskFindingsContractTests.cs"
Task: "Create risk finding detail contract tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/GetRiskFindingContractTests.cs"
Task: "Create list and detail integration tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/ReviewRiskFindingsTests.cs"
Task: "Create filter, tenant-boundary, and admin-authorization integration tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/RiskFindingsAuthorizationTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "Create workspace risk policy unit tests in tests/AIUsageGuard.UnitTests/RiskDetection/WorkspaceRiskPolicyServiceTests.cs"
Task: "Create risk policy contract tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/WorkspaceRiskPolicyContractTests.cs"
Task: "Create policy update and future-evaluation integration tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/WorkspaceRiskPolicyIntegrationTests.cs"
Task: "Create policy authorization and cross-workspace denial tests in tests/AIUsageGuard.IntegrationTests/RiskDetection/WorkspaceRiskPolicyAuthorizationTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm accepted AI usage events produce the expected workspace-scoped findings and non-match evaluation outcomes

### Incremental Delivery

1. Build the shared risk entities, persistence, and store contract
2. Deliver synchronous event-triggered risk evaluation and finding persistence
3. Deliver admin review APIs for list and detail workflows
4. Deliver workspace policy management that changes future evaluations
5. Finish with structured logging, regression coverage, and contract or quickstart cleanup

### Parallel Team Strategy

1. One developer handles shared domain and persistence entities
2. One developer handles rule evaluator and orchestration logic
3. One developer handles review and policy API contracts plus tests
4. After Foundational work, assign one developer per user story phase while keeping shared files such as `ApplicationDbContext.cs`, `Program.cs`, and `WorkspaceAuthorizationHandler.cs` sequential

---

## Notes

- The tasks are intentionally small and file-scoped so a cheaper LLM can implement them one at a time without broad architectural inference
- User Story 1 is the recommended MVP scope
- Do not start User Story 2 or User Story 3 before the Foundational phase is complete
- If a task touches the same file as an unfinished earlier task, finish the earlier task first instead of parallelizing
