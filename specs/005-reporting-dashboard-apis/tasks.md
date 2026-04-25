# Tasks: Phase 4 Reporting and Dashboard APIs

**Input**: Design documents from `/specs/005-reporting-dashboard-apis/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for this feature. This task list keeps
tests explicit and front-loaded so reporting behavior, tenant boundaries,
contract stability, and audit expectations can be verified in small slices.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this belongs to (e.g. `US1`, `US2`, `US3`)
- Include exact file paths in descriptions

## Path Conventions

- Solution root: `AIUsageGuard.slnx`
- Application code: `src/AIUsageGuard.Api/`, `src/AIUsageGuard.Application/`, `src/AIUsageGuard.Domain/`, `src/AIUsageGuard.Infrastructure/`
- Tests: `tests/AIUsageGuard.UnitTests/`, `tests/AIUsageGuard.IntegrationTests/`
- Feature docs: `specs/005-reporting-dashboard-apis/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Phase 4 reporting feature surface area and manual
verification entry points before deeper implementation begins

- [X] T001 Create the reporting feature folders under `src/AIUsageGuard.Api/Contracts/Reporting/`, `src/AIUsageGuard.Application/Reporting/GetDashboard/`, `src/AIUsageGuard.Application/Reporting/GetUsageByUser/`, `src/AIUsageGuard.Application/Reporting/GetUsageByTool/`, `src/AIUsageGuard.Application/Reporting/GetAlertsSummary/`, `src/AIUsageGuard.Application/Reporting/GetCostSummary/`, `tests/AIUsageGuard.UnitTests/Reporting/`, and `tests/AIUsageGuard.IntegrationTests/Reporting/`
- [X] T002 [P] Add a "Reporting" manual request section to `src/AIUsageGuard.Api/AIUsageGuard.Api.http`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared reporting models, query validation, and
aggregation infrastructure required by all user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create the shared reporting period and trend models in `src/AIUsageGuard.Application/Models/ReportingPeriod.cs` and `src/AIUsageGuard.Application/Models/DailyTrendPoint.cs`
- [X] T004 [P] Create the shared dashboard and summary models in `src/AIUsageGuard.Application/Models/DashboardSummary.cs`, `src/AIUsageGuard.Application/Models/DashboardTotals.cs`, `src/AIUsageGuard.Application/Models/UsageSummaryRow.cs`, `src/AIUsageGuard.Application/Models/UsageSummaryPage.cs`, `src/AIUsageGuard.Application/Models/AlertsSummary.cs`, and `src/AIUsageGuard.Application/Models/EstimatedCostSummary.cs`
- [X] T005 [P] Create the shared reporting query and validation helpers in `src/AIUsageGuard.Application/Reporting/ReportingPeriodQuery.cs` and `src/AIUsageGuard.Application/Reporting/ReportingPeriodValidator.cs`
- [X] T006 Extend the reporting read contract in `src/AIUsageGuard.Application/Abstractions/IPlatformStore.cs` with workspace-scoped aggregation methods for dashboard totals, grouped usage, alerts trends, and cost completeness
- [X] T007 Implement workspace-scoped reporting aggregation helpers in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T008 Add reporting-oriented indexes and migration updates in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/AIUsageEventConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/RiskFindingConfiguration.cs`, and `src/AIUsageGuard.Infrastructure/Persistence/Migrations/`

**Checkpoint**: Foundation ready - dashboard, grouped reporting, and reusable
API-consumer datasets can now be implemented

---

## Phase 3: User Story 1 - View Workspace Dashboard Summary (Priority: P1) 🎯 MVP

**Goal**: Allow workspace owners and admins to retrieve a single dashboard
overview for a reporting period with workspace-scoped totals, top users, top
tools, alerts, and estimated cost

**Independent Test**: An authorized workspace owner or admin requests
`GET /workspaces/{workspaceId}/dashboard` for a valid date range and receives
only that workspace's period totals, top summary breakdowns, alerts summary,
and cost summary, including empty-state behavior when no matching data exists

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T009 [P] [US1] Create dashboard aggregation unit tests in `tests/AIUsageGuard.UnitTests/Reporting/GetDashboardServiceTests.cs`
- [X] T010 [P] [US1] Create dashboard contract tests in `tests/AIUsageGuard.IntegrationTests/Reporting/GetDashboardContractTests.cs`
- [X] T011 [P] [US1] Create dashboard integration tests in `tests/AIUsageGuard.IntegrationTests/Reporting/GetWorkspaceDashboardTests.cs`
- [X] T012 [P] [US1] Create dashboard authorization and cross-workspace tests in `tests/AIUsageGuard.IntegrationTests/Reporting/DashboardAuthorizationTests.cs`

### Implementation for User Story 1

- [X] T013 [P] [US1] Create the dashboard request and shared response contracts in `src/AIUsageGuard.Api/Contracts/Reporting/ReportingPeriodRequest.cs`, `src/AIUsageGuard.Api/Contracts/Reporting/ReportingPeriodResponse.cs`, `src/AIUsageGuard.Api/Contracts/Reporting/DashboardTotalsResponse.cs`, `src/AIUsageGuard.Api/Contracts/Reporting/UsageSummaryRowResponse.cs`, `src/AIUsageGuard.Api/Contracts/Reporting/UsageSummaryPageResponse.cs`, `src/AIUsageGuard.Api/Contracts/Reporting/DailyTrendPointResponse.cs`, `src/AIUsageGuard.Api/Contracts/Reporting/AlertsSummaryViewResponse.cs`, `src/AIUsageGuard.Api/Contracts/Reporting/CostSummaryViewResponse.cs`, and `src/AIUsageGuard.Api/Contracts/Reporting/DashboardResponse.cs`
- [X] T014 [P] [US1] Create the dashboard query and result models in `src/AIUsageGuard.Application/Reporting/GetDashboard/GetDashboardQuery.cs` and `src/AIUsageGuard.Application/Reporting/GetDashboard/GetDashboardResult.cs`
- [X] T015 [US1] Implement dashboard aggregation, top-list shaping, and date-range validation in `src/AIUsageGuard.Application/Reporting/GetDashboard/GetDashboardService.cs`
- [X] T016 [US1] Implement `GET /workspaces/{workspaceId}/dashboard` in `src/AIUsageGuard.Api/Controllers/DashboardController.cs`
- [X] T017 [US1] Register the dashboard service and controller dependencies in `src/AIUsageGuard.Api/Program.cs`
- [X] T018 [US1] Add dashboard-read audit recording, denied-access audit coverage, and structured logging in `src/AIUsageGuard.Application/Reporting/GetDashboard/GetDashboardService.cs`, `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`, and `src/AIUsageGuard.Api/Program.cs`
- [X] T019 [US1] Extend dashboard client helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 1 is complete when admins can open a workspace-only
dashboard for a date range and see correct totals, top breakdowns, alerts, and
cost data without exposing raw AI content

---

## Phase 4: User Story 2 - Analyze Usage and Risk Trends (Priority: P2)

**Goal**: Allow workspace owners and admins to retrieve grouped usage reports by
user and tool plus an alerts summary with severity and daily trend visibility

**Independent Test**: An authorized workspace owner or admin retrieves
`usage-by-user`, `usage-by-tool`, and `alerts-summary` for a selected period and
can identify top actors, top tools, and flagged-activity trends without seeing
data from any other workspace

### Tests for User Story 2 ⚠️

- [X] T020 [P] [US2] Create usage-by-user aggregation unit tests in `tests/AIUsageGuard.UnitTests/Reporting/GetUsageByUserServiceTests.cs`
- [X] T021 [P] [US2] Create usage-by-tool aggregation unit tests in `tests/AIUsageGuard.UnitTests/Reporting/GetUsageByToolServiceTests.cs`
- [X] T022 [P] [US2] Create grouped-report contract tests in `tests/AIUsageGuard.IntegrationTests/Reporting/GetUsageReportsContractTests.cs`
- [X] T023 [P] [US2] Create grouped usage and alerts integration tests in `tests/AIUsageGuard.IntegrationTests/Reporting/UsageAndAlertsReportTests.cs`
- [X] T024 [P] [US2] Create grouped-report authorization and cross-workspace tests in `tests/AIUsageGuard.IntegrationTests/Reporting/ReportingAuthorizationTests.cs`

### Implementation for User Story 2

- [X] T025 [P] [US2] Create grouped reporting request and response contracts in `src/AIUsageGuard.Api/Contracts/Reporting/PagedReportingRequest.cs`, `src/AIUsageGuard.Api/Contracts/Reporting/UsageSummaryResponse.cs`, and `src/AIUsageGuard.Api/Contracts/Reporting/AlertsSummaryResponse.cs`
- [X] T026 [P] [US2] Create grouped usage and alerts query or result models in `src/AIUsageGuard.Application/Reporting/GetUsageByUser/GetUsageByUserQuery.cs`, `src/AIUsageGuard.Application/Reporting/GetUsageByUser/GetUsageByUserResult.cs`, `src/AIUsageGuard.Application/Reporting/GetUsageByTool/GetUsageByToolQuery.cs`, `src/AIUsageGuard.Application/Reporting/GetUsageByTool/GetUsageByToolResult.cs`, `src/AIUsageGuard.Application/Reporting/GetAlertsSummary/GetAlertsSummaryQuery.cs`, and `src/AIUsageGuard.Application/Reporting/GetAlertsSummary/GetAlertsSummaryResult.cs`
- [X] T027 [US2] Implement grouped usage aggregation services in `src/AIUsageGuard.Application/Reporting/GetUsageByUser/GetUsageByUserService.cs` and `src/AIUsageGuard.Application/Reporting/GetUsageByTool/GetUsageByToolService.cs`
- [X] T028 [US2] Implement alerts summary aggregation in `src/AIUsageGuard.Application/Reporting/GetAlertsSummary/GetAlertsSummaryService.cs`
- [X] T029 [US2] Implement `GET /workspaces/{workspaceId}/reports/usage-by-user`, `GET /workspaces/{workspaceId}/reports/usage-by-tool`, and `GET /workspaces/{workspaceId}/reports/alerts-summary` in `src/AIUsageGuard.Api/Controllers/ReportsController.cs`
- [X] T030 [US2] Register grouped reporting services and add grouped-report audit recording in `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Application/Reporting/GetUsageByUser/GetUsageByUserService.cs`, `src/AIUsageGuard.Application/Reporting/GetUsageByTool/GetUsageByToolService.cs`, and `src/AIUsageGuard.Application/Reporting/GetAlertsSummary/GetAlertsSummaryService.cs`
- [X] T031 [US2] Extend grouped reporting client helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 2 is complete when admins can answer who used AI
most, which tools were most active, and where risk was concentrated for one
workspace and period

---

## Phase 5: User Story 3 - Reuse Reporting Data in Simple API Consumers (Priority: P3)

**Goal**: Provide a stable cost-summary dataset and predictable failure or
empty-state behavior so authenticated API consumers can reuse reporting outputs
without building custom reporting logic

**Independent Test**: An authorized consumer requests
`GET /workspaces/{workspaceId}/reports/cost-summary` and receives a stable
period-scoped dataset with explicit partial-cost metadata, while invalid date
ranges and unauthorized requests fail cleanly without leaking unrelated
workspace data

### Tests for User Story 3 ⚠️

- [X] T032 [P] [US3] Create cost summary aggregation unit tests in `tests/AIUsageGuard.UnitTests/Reporting/GetCostSummaryServiceTests.cs`
- [X] T033 [P] [US3] Create cost summary contract tests in `tests/AIUsageGuard.IntegrationTests/Reporting/GetCostSummaryContractTests.cs`
- [X] T034 [P] [US3] Create cost summary and empty-state integration tests in `tests/AIUsageGuard.IntegrationTests/Reporting/GetCostSummaryTests.cs`
- [X] T035 [P] [US3] Create invalid-range and reporting-failure integration tests in `tests/AIUsageGuard.IntegrationTests/Reporting/ReportingFailureTests.cs`

### Implementation for User Story 3

- [X] T036 [P] [US3] Create the cost summary response contract in `src/AIUsageGuard.Api/Contracts/Reporting/CostSummaryResponse.cs`
- [X] T037 [P] [US3] Create the cost summary query and result models in `src/AIUsageGuard.Application/Reporting/GetCostSummary/GetCostSummaryQuery.cs` and `src/AIUsageGuard.Application/Reporting/GetCostSummary/GetCostSummaryResult.cs`
- [X] T038 [US3] Implement cost completeness aggregation and partial-cost handling in `src/AIUsageGuard.Application/Reporting/GetCostSummary/GetCostSummaryService.cs`
- [X] T039 [US3] Implement `GET /workspaces/{workspaceId}/reports/cost-summary` and shared invalid-range ProblemDetails handling in `src/AIUsageGuard.Api/Controllers/ReportsController.cs` and `src/AIUsageGuard.Api/Controllers/DashboardController.cs`
- [X] T040 [US3] Register cost summary services and add cost-summary plus invalid-period audit recording in `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Application/Reporting/GetCostSummary/GetCostSummaryService.cs`, and `src/AIUsageGuard.Api/Controllers/ReportsController.cs`
- [X] T041 [US3] Extend cost-summary client helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 3 is complete when simple API consumers can rely on
stable cost-summary and failure semantics across the reporting surface without
needing custom aggregation code

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish observability, regression safety, contract alignment, and
manual verification coverage across all reporting stories

- [X] T042 [P] Add cross-story reporting regression coverage in `tests/AIUsageGuard.IntegrationTests/Reporting/ReportingRegressionTests.cs`
- [X] T043 [P] Refine structured logging, counters, and empty-state telemetry in `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Application/Reporting/GetDashboard/GetDashboardService.cs`, `src/AIUsageGuard.Application/Reporting/GetUsageByUser/GetUsageByUserService.cs`, `src/AIUsageGuard.Application/Reporting/GetUsageByTool/GetUsageByToolService.cs`, `src/AIUsageGuard.Application/Reporting/GetAlertsSummary/GetAlertsSummaryService.cs`, and `src/AIUsageGuard.Application/Reporting/GetCostSummary/GetCostSummaryService.cs`
- [X] T044 [P] Align final OpenAPI examples and failure responses in `specs/005-reporting-dashboard-apis/contracts/reporting.openapi.yaml`
- [X] T045 [P] Update the manual verification requests in `src/AIUsageGuard.Api/AIUsageGuard.Api.http`
- [X] T046 Run the quickstart validation pass and capture any wording fixes in `specs/005-reporting-dashboard-apis/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion and is the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and is easiest after User Story 1 because the dashboard work establishes shared period validation and summary models
- **User Story 3 (Phase 5)**: Depends on Foundational completion and is easiest after User Story 1 and User Story 2 because it stabilizes and extends the reusable reporting datasets they introduce
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational and establishes the first working reporting surface
- **User Story 2 (P2)**: Can start after Foundational, but is easiest after User Story 1 because it reuses the same reporting period and summary primitives
- **User Story 3 (P3)**: Can start after Foundational, but its full product value depends on the dataset patterns introduced by User Story 1 and User Story 2

### Within Each User Story

- Tests MUST be written and fail before implementation
- Request and response contracts before controller wiring
- Query or result models before service logic
- Store and aggregation changes before controller behavior that depends on them
- Audit and authorization handling before story sign-off
- Cheaper models should execute one unchecked task at a time and avoid combining tasks that touch the same file

### Parallel Opportunities

- T002 can run in parallel with T001
- T004 and T005 can run in parallel after T003
- T009, T010, T011, and T012 can run in parallel
- T013 and T014 can run in parallel
- T020, T021, T022, T023, and T024 can run in parallel
- T025 and T026 can run in parallel
- T032, T033, T034, and T035 can run in parallel
- T042, T043, T044, and T045 can run in parallel

---

## Parallel Example: User Story 1

```text
Task: "Create dashboard aggregation unit tests in tests/AIUsageGuard.UnitTests/Reporting/GetDashboardServiceTests.cs"
Task: "Create dashboard contract tests in tests/AIUsageGuard.IntegrationTests/Reporting/GetDashboardContractTests.cs"
Task: "Create dashboard integration tests in tests/AIUsageGuard.IntegrationTests/Reporting/GetWorkspaceDashboardTests.cs"
Task: "Create dashboard authorization and cross-workspace tests in tests/AIUsageGuard.IntegrationTests/Reporting/DashboardAuthorizationTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Create usage-by-user aggregation unit tests in tests/AIUsageGuard.UnitTests/Reporting/GetUsageByUserServiceTests.cs"
Task: "Create usage-by-tool aggregation unit tests in tests/AIUsageGuard.UnitTests/Reporting/GetUsageByToolServiceTests.cs"
Task: "Create grouped-report contract tests in tests/AIUsageGuard.IntegrationTests/Reporting/GetUsageReportsContractTests.cs"
Task: "Create grouped usage and alerts integration tests in tests/AIUsageGuard.IntegrationTests/Reporting/UsageAndAlertsReportTests.cs"
Task: "Create grouped-report authorization and cross-workspace tests in tests/AIUsageGuard.IntegrationTests/Reporting/ReportingAuthorizationTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "Create cost summary aggregation unit tests in tests/AIUsageGuard.UnitTests/Reporting/GetCostSummaryServiceTests.cs"
Task: "Create cost summary contract tests in tests/AIUsageGuard.IntegrationTests/Reporting/GetCostSummaryContractTests.cs"
Task: "Create cost summary and empty-state integration tests in tests/AIUsageGuard.IntegrationTests/Reporting/GetCostSummaryTests.cs"
Task: "Create invalid-range and reporting-failure integration tests in tests/AIUsageGuard.IntegrationTests/Reporting/ReportingFailureTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm a workspace admin can open the dashboard and see correct workspace-only totals for a reporting period

### Incremental Delivery

1. Build the shared reporting models, validation, and aggregation foundation
2. Deliver the dashboard overview endpoint for workspace admins
3. Deliver grouped usage and alerts report endpoints
4. Deliver the reusable cost summary endpoint plus stable failure handling
5. Finish with regression coverage, telemetry, contract cleanup, and quickstart validation

### Parallel Team Strategy

1. One developer handles shared models, period validation, and store aggregation methods
2. One developer handles dashboard contracts, service, and controller wiring
3. One developer handles grouped report services plus integration tests
4. After Foundational work, assign one developer per user story phase while keeping shared files such as `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`, `src/AIUsageGuard.Api/Program.cs`, and `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs` sequential

---

## Notes

- The tasks are intentionally small and file-scoped so a cheaper LLM can implement them one by one without broad architectural inference
- User Story 1 is the recommended MVP scope
- Do not start User Story 2 or User Story 3 before the Foundational phase is complete
- If a task touches the same file as an unfinished earlier task, finish the earlier task first instead of parallelizing
