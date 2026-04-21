# Tasks: Phase 2 AI Usage Event Ingestion

**Input**: Design documents from `/specs/003-ai-usage-ingestion/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for this feature. This task list is intentionally decomposed into small, low-ambiguity steps so a cheaper LLM can complete one task at a time without needing large cross-file reasoning jumps.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Solution root: `AIUsageGuard.slnx`
- Application code: `src/AIUsageGuard.Api/`, `src/AIUsageGuard.Application/`, `src/AIUsageGuard.Domain/`, `src/AIUsageGuard.Infrastructure/`
- Tests: `tests/AIUsageGuard.UnitTests/`, `tests/AIUsageGuard.IntegrationTests/`
- Feature docs: `specs/003-ai-usage-ingestion/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the feature surface area and manual verification entry points before deeper implementation begins

- [X] T001 Create the AI usage event feature folders under `src/AIUsageGuard.Api/Contracts/AIUsageEvents/`, `src/AIUsageGuard.Application/AIUsageEvents/`, `src/AIUsageGuard.Domain/AIUsageEvents/`, `tests/AIUsageGuard.UnitTests/AIUsageEvents/`, and `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/`
- [X] T002 [P] Add an "AI Usage Events" manual request section to `src/AIUsageGuard.Api/AIUsageGuard.Api.http`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared event model, persistence contract, and database infrastructure required by all user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create the application event type enum in `src/AIUsageGuard.Application/Models/AIUsageEventType.cs`
- [X] T004 [P] Create the application AI usage event model in `src/AIUsageGuard.Application/Models/AIUsageEvent.cs`
- [X] T005 [P] Create the domain event type enum in `src/AIUsageGuard.Domain/AIUsageEvents/AIUsageEventType.cs`
- [X] T006 [P] Create the domain AI usage event entity in `src/AIUsageGuard.Domain/AIUsageEvents/AIUsageEvent.cs`
- [X] T007 [P] Create the ingestion result model in `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventResult.cs`
- [X] T008 [P] Create the history query model in `src/AIUsageGuard.Application/AIUsageEvents/ListEvents/ListAIUsageEventsQuery.cs`
- [X] T009 [P] Create the history result model in `src/AIUsageGuard.Application/AIUsageEvents/ListEvents/ListAIUsageEventsResult.cs`
- [X] T010 Extend the platform store contract with AI usage event read/write methods in `src/AIUsageGuard.Application/Abstractions/IPlatformStore.cs`
- [X] T011 Add the AI usage event `DbSet`, store mappings, and query methods in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T012 Create the EF Core AI usage event configuration in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/AIUsageEventConfiguration.cs`
- [X] T013 Register the new entity configuration in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T014 Create the AI usage event migration files in `src/AIUsageGuard.Infrastructure/Persistence/Migrations/`

**Checkpoint**: Foundation ready - event ingestion, event history, and duplicate handling stories can now be implemented

---

## Phase 3: User Story 1 - Capture AI Usage Events (Priority: P1) 🎯 MVP

**Goal**: Let an authenticated workspace member submit a valid AI usage event and persist it inside the correct tenant history

**Independent Test**: A signed-in workspace member can submit a valid `prompt_submitted` or `tool_used` event and receive an accepted response with the event stored under the correct workspace and actor

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T015 [P] [US1] Create ingestion unit tests in `tests/AIUsageGuard.UnitTests/AIUsageEvents/IngestAIUsageEventServiceTests.cs`
- [X] T016 [P] [US1] Create accepted-ingestion integration tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/IngestAIUsageEventTests.cs`
- [X] T017 [P] [US1] Create POST contract tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/IngestAIUsageEventContractTests.cs`
- [X] T018 [P] [US1] Create member-ingestion authorization tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/IngestAIUsageEventMemberAccessTests.cs`

### Implementation for User Story 1

- [X] T019 [P] [US1] Create the POST request contract in `src/AIUsageGuard.Api/Contracts/AIUsageEvents/IngestAIUsageEventRequest.cs`
- [X] T020 [P] [US1] Create the POST response contract in `src/AIUsageGuard.Api/Contracts/AIUsageEvents/EventIngestionResponse.cs`
- [X] T021 [P] [US1] Create the ingestion command model in `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventCommand.cs`
- [X] T022 [US1] Implement accepted-event persistence in `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventService.cs`
- [X] T023 [US1] Implement `POST /workspaces/{workspaceId}/events` in `src/AIUsageGuard.Api/Controllers/AIUsageEventsController.cs`
- [X] T024 [US1] Register the ingestion service and controller dependencies in `src/AIUsageGuard.Api/Program.cs`
- [X] T025 [US1] Add accepted-ingestion audit recording in `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventService.cs`
- [X] T026 [US1] Add event-ingestion client helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 1 is complete when valid workspace members can submit supported events and those events are stored with the correct tenant and actor context

---

## Phase 4: User Story 2 - Review Event History (Priority: P2)

**Goal**: Allow workspace owners and admins to retrieve paged AI usage history with basic filters and stable ordering

**Independent Test**: A signed-in workspace owner or admin can retrieve only their workspace's events, filter by event type, actor, tool, and date range, and receive consistently ordered results

### Tests for User Story 2 ⚠️

- [X] T027 [P] [US2] Create history-query unit tests in `tests/AIUsageGuard.UnitTests/AIUsageEvents/ListAIUsageEventsServiceTests.cs`
- [X] T028 [P] [US2] Create event-history integration tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/ListAIUsageEventsTests.cs`
- [X] T029 [P] [US2] Create filter-and-pagination integration tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/FilterAIUsageEventsTests.cs`
- [X] T030 [P] [US2] Create owner/admin history authorization tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/ListAIUsageEventsAuthorizationTests.cs`

### Implementation for User Story 2

- [X] T031 [P] [US2] Create the GET query contract in `src/AIUsageGuard.Api/Contracts/AIUsageEvents/ListAIUsageEventsRequest.cs`
- [X] T032 [P] [US2] Create the history item response contract in `src/AIUsageGuard.Api/Contracts/AIUsageEvents/AIUsageEventHistoryItemResponse.cs`
- [X] T033 [P] [US2] Create the paged history response contract in `src/AIUsageGuard.Api/Contracts/AIUsageEvents/AIUsageEventHistoryResponse.cs`
- [X] T034 [US2] Implement history filtering, paging, and stable ordering in `src/AIUsageGuard.Application/AIUsageEvents/ListEvents/ListAIUsageEventsService.cs`
- [X] T035 [US2] Add workspace-scoped history queries in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T036 [US2] Implement `GET /workspaces/{workspaceId}/events` in `src/AIUsageGuard.Api/Controllers/AIUsageEventsController.cs`
- [X] T037 [US2] Add history-read audit recording in `src/AIUsageGuard.Application/AIUsageEvents/ListEvents/ListAIUsageEventsService.cs`
- [X] T038 [US2] Extend event-history client helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 2 is complete when authorized admins can inspect only their own workspace event history with the supported filters and paging rules

---

## Phase 5: User Story 3 - Handle Invalid or Duplicate Submissions Safely (Priority: P3)

**Goal**: Reject malformed submissions, detect duplicate replays deterministically, and keep the stored event history trustworthy

**Independent Test**: Invalid submissions fail with ProblemDetails, duplicate submissions return a duplicate outcome instead of creating another record, and cross-workspace replay attempts never pollute another tenant's history

### Tests for User Story 3 ⚠️

- [X] T039 [P] [US3] Create duplicate/idempotency unit tests in `tests/AIUsageGuard.UnitTests/AIUsageEvents/IngestAIUsageEventDeduplicationTests.cs`
- [X] T040 [P] [US3] Create invalid-payload integration tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/RejectInvalidAIUsageEventTests.cs`
- [X] T041 [P] [US3] Create duplicate-replay integration tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/DuplicateAIUsageEventTests.cs`
- [X] T042 [P] [US3] Create cross-workspace submission denial tests in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/IngestAIUsageEventIsolationTests.cs`

### Implementation for User Story 3

- [X] T043 [US3] Enforce workspace-scoped idempotency lookups in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T044 [US3] Add the workspace/idempotency unique index and filter indexes in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/AIUsageEventConfiguration.cs`
- [X] T045 [US3] Implement invalid-payload, late-event, and duplicate decision rules in `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventService.cs`
- [X] T046 [US3] Return duplicate and validation ProblemDetails outcomes from `src/AIUsageGuard.Api/Controllers/AIUsageEventsController.cs`
- [X] T047 [US3] Record rejected and duplicate ingestion audit outcomes in `src/AIUsageGuard.Application/AIUsageEvents/IngestEvent/IngestAIUsageEventService.cs`
- [X] T048 [US3] Add duplicate and invalid-submission client helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 3 is complete when invalid and replayed submissions no longer distort persisted workspace history and every decision is explainable through API responses and audit output

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish observability, regression safety, and verification artifacts across all stories

- [X] T049 [P] Refine structured logging and correlation enrichment for event operations in `src/AIUsageGuard.Api/Program.cs`
- [X] T050 [P] Add cross-story regression coverage in `tests/AIUsageGuard.IntegrationTests/AIUsageEvents/AIUsageEventsRegressionTests.cs`
- [X] T051 [P] Add page-boundary and date-range validation tests in `tests/AIUsageGuard.UnitTests/AIUsageEvents/ListAIUsageEventsQueryValidationTests.cs`
- [X] T052 [P] Align the final HTTP contract examples and failure responses in `specs/003-ai-usage-ingestion/contracts/ai-usage-events.openapi.yaml`
- [X] T053 [P] Update the manual verification requests in `src/AIUsageGuard.Api/AIUsageGuard.Api.http`
- [X] T054 Run the quickstart validation pass and capture any wording fixes in `specs/003-ai-usage-ingestion/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion and is the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and reuses the event model built for User Story 1
- **User Story 3 (Phase 5)**: Depends on Foundational completion and hardens the ingestion path introduced in User Story 1
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - establishes the first working event capture flow
- **User Story 2 (P2)**: Can start after Foundational, but is easiest after User Story 1 because it reuses the persisted event shape and controller surface
- **User Story 3 (P3)**: Depends on the POST ingestion flow from User Story 1 and should be implemented after the happy path exists

### Within Each User Story

- Tests MUST be written and fail before implementation
- Request and response contracts before controller wiring
- Application models before service logic
- Store/query changes before controller behavior that depends on them
- Audit and error handling before story sign-off
- Cheaper models should execute one unchecked task at a time and avoid combining tasks that touch the same file

### Parallel Opportunities

- T002 can run in parallel with T001
- T004, T005, T006, T007, T008, and T009 can run in parallel after T003
- T015, T016, T017, and T018 can run in parallel
- T019, T020, and T021 can run in parallel
- T027, T028, T029, and T030 can run in parallel
- T031, T032, and T033 can run in parallel
- T039, T040, T041, and T042 can run in parallel
- T049, T050, T051, T052, and T053 can run in parallel

---

## Parallel Example: User Story 1

```text
Task: "Create ingestion unit tests in tests/AIUsageGuard.UnitTests/AIUsageEvents/IngestAIUsageEventServiceTests.cs"
Task: "Create accepted-ingestion integration tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/IngestAIUsageEventTests.cs"
Task: "Create POST contract tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/IngestAIUsageEventContractTests.cs"
Task: "Create member-ingestion authorization tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/IngestAIUsageEventMemberAccessTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Create history-query unit tests in tests/AIUsageGuard.UnitTests/AIUsageEvents/ListAIUsageEventsServiceTests.cs"
Task: "Create event-history integration tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/ListAIUsageEventsTests.cs"
Task: "Create filter-and-pagination integration tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/FilterAIUsageEventsTests.cs"
Task: "Create owner/admin history authorization tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/ListAIUsageEventsAuthorizationTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "Create duplicate/idempotency unit tests in tests/AIUsageGuard.UnitTests/AIUsageEvents/IngestAIUsageEventDeduplicationTests.cs"
Task: "Create invalid-payload integration tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/RejectInvalidAIUsageEventTests.cs"
Task: "Create duplicate-replay integration tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/DuplicateAIUsageEventTests.cs"
Task: "Create cross-workspace submission denial tests in tests/AIUsageGuard.IntegrationTests/AIUsageEvents/IngestAIUsageEventIsolationTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm a workspace member can submit a valid event and receive an accepted response with persisted history

### Incremental Delivery

1. Build the feature folders and shared event persistence foundation
2. Deliver event ingestion for valid workspace members
3. Deliver admin history retrieval with filters and paging
4. Deliver invalid and duplicate submission hardening
5. Finish with logging, regression coverage, and contract/quickstart cleanup

### Parallel Team Strategy

1. One developer handles domain/application models and store interfaces
2. One developer handles persistence configuration and migration work
3. One developer handles tests and API contracts
4. After Foundational work, assign one developer per user story phase with controller and service ownership kept separate

---

## Notes

- The tasks are intentionally small and file-scoped so a cheaper LLM can implement them one by one without large cross-file reasoning jumps
- User Story 1 is the recommended MVP scope
- Do not start User Story 2 or User Story 3 before the Foundational phase is complete
- If a task touches the same file as an unfinished earlier task, finish the earlier task first instead of parallelizing
