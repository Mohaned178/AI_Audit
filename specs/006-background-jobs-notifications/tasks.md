# Tasks: Phase 5 Background Jobs and Notifications

**Input**: Design documents from `/specs/006-background-jobs-notifications/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for this feature. This task list keeps
tests explicit and front-loaded so background automation, tenant boundaries,
delivery reliability, contract stability, and audit behavior can be verified in
small slices.

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
- Feature docs: `specs/006-background-jobs-notifications/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Phase 5 notification and background-job surface area
plus manual verification entry points before deeper implementation begins

- [X] T001 Create the notification feature folders under `src/AIUsageGuard.Api/Contracts/Notifications/`, `src/AIUsageGuard.Application/BackgroundJobs/RunUrgentAlertScan/`, `src/AIUsageGuard.Application/BackgroundJobs/RunDigestGeneration/`, `src/AIUsageGuard.Application/BackgroundJobs/RunDeliveryRetry/`, `src/AIUsageGuard.Application/Notifications/GetNotificationPreferences/`, `src/AIUsageGuard.Application/Notifications/ConfigureWorkspaceNotifications/`, `src/AIUsageGuard.Application/Notifications/ListNotifications/`, `src/AIUsageGuard.Application/Notifications/GetNotification/`, `src/AIUsageGuard.Application/Notifications/DeliverPendingNotifications/`, `src/AIUsageGuard.Infrastructure/BackgroundProcessing/`, `src/AIUsageGuard.Infrastructure/Notifications/`, `tests/AIUsageGuard.UnitTests/BackgroundJobs/`, `tests/AIUsageGuard.UnitTests/Notifications/`, and `tests/AIUsageGuard.IntegrationTests/Notifications/`
- [X] T002 [P] Add a "Notifications" manual request section to `src/AIUsageGuard.Api/AIUsageGuard.Api.http`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared notification models, durable persistence, and
background-processing primitives required by all user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create the shared notification enums in `src/AIUsageGuard.Application/Models/NotificationType.cs`, `src/AIUsageGuard.Application/Models/NotificationStatus.cs`, `src/AIUsageGuard.Application/Models/DeliveryStatus.cs`, `src/AIUsageGuard.Application/Models/DigestCadence.cs`, and `src/AIUsageGuard.Application/Models/RecipientSelectionMode.cs`
- [X] T004 [P] Create the shared notification and job models in `src/AIUsageGuard.Application/Models/NotificationPreference.cs`, `src/AIUsageGuard.Application/Models/BackgroundJobRun.cs`, `src/AIUsageGuard.Application/Models/NotificationMessage.cs`, `src/AIUsageGuard.Application/Models/NotificationDeliveryOutcome.cs`, `src/AIUsageGuard.Application/Models/DigestSummary.cs`, and `src/AIUsageGuard.Application/Models/UrgentAlertSnapshot.cs`
- [X] T005 [P] Create notification-processing options and sender abstractions in `src/AIUsageGuard.Application/Notifications/NotificationProcessingOptions.cs`, `src/AIUsageGuard.Application/Notifications/DigestSchedulingOptions.cs`, and `src/AIUsageGuard.Application/Abstractions/INotificationSender.cs`
- [X] T006 Extend the notification read/write contract in `src/AIUsageGuard.Application/Abstractions/IPlatformStore.cs` with workspace-scoped notification preference, job run, notification, and delivery-outcome methods
- [X] T007 Implement notification persistence and query helpers in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T008 Add notification entity configurations and migration updates in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/NotificationPreferenceConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/BackgroundJobRunConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/NotificationMessageConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Persistence/Configurations/NotificationDeliveryOutcomeConfiguration.cs`, and `src/AIUsageGuard.Infrastructure/Persistence/Migrations/`
- [X] T009 Implement shared background-processing primitives in `src/AIUsageGuard.Infrastructure/BackgroundProcessing/NotificationBackgroundWorker.cs`, `src/AIUsageGuard.Infrastructure/BackgroundProcessing/BackgroundProcessingClock.cs`, and `src/AIUsageGuard.Infrastructure/BackgroundProcessing/BackgroundJobScopeRunner.cs`
- [X] T010 Register notification options, hosted services, and baseline observability in `src/AIUsageGuard.Api/Program.cs`

**Checkpoint**: Foundation ready - urgent alerts, digests, and notification
administration can now be implemented

---

## Phase 3: User Story 1 - Receive Automated Governance Alerts (Priority: P1) 🎯 MVP

**Goal**: Allow workspace owners and admins to receive urgent workspace-scoped
notifications for high-priority governance conditions without duplicate alert
spam or cross-workspace leakage

**Independent Test**: A workspace triggers a supported urgent condition and the
system sends one clear notification to eligible recipients in that workspace,
while repeated scans suppress duplicates and other workspaces never receive the
message

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T011 [P] [US1] Create urgent alert aggregation and deduplication unit tests in `tests/AIUsageGuard.UnitTests/BackgroundJobs/RunUrgentAlertScanServiceTests.cs`
- [X] T012 [P] [US1] Create pending-notification delivery unit tests in `tests/AIUsageGuard.UnitTests/Notifications/DeliverPendingNotificationsServiceTests.cs`
- [X] T013 [P] [US1] Create urgent alert delivery integration tests in `tests/AIUsageGuard.IntegrationTests/Notifications/UrgentAlertNotificationTests.cs`
- [X] T014 [P] [US1] Create urgent alert isolation and duplicate-suppression tests in `tests/AIUsageGuard.IntegrationTests/Notifications/UrgentAlertIsolationTests.cs`

### Implementation for User Story 1

- [X] T015 [P] [US1] Create urgent alert workflow models in `src/AIUsageGuard.Application/BackgroundJobs/RunUrgentAlertScan/UrgentAlertCandidate.cs`, `src/AIUsageGuard.Application/BackgroundJobs/RunUrgentAlertScan/RunUrgentAlertScanCommand.cs`, and `src/AIUsageGuard.Application/BackgroundJobs/RunUrgentAlertScan/RunUrgentAlertScanResult.cs`
- [X] T016 [P] [US1] Create delivery workflow models in `src/AIUsageGuard.Application/Notifications/DeliverPendingNotifications/DeliverPendingNotificationsCommand.cs` and `src/AIUsageGuard.Application/Notifications/DeliverPendingNotifications/DeliverPendingNotificationsResult.cs`
- [X] T017 [US1] Implement urgent alert scanning, trigger fingerprinting, and notification composition in `src/AIUsageGuard.Application/BackgroundJobs/RunUrgentAlertScan/RunUrgentAlertScanService.cs`
- [X] T018 [US1] Implement pending-notification delivery and eligible-recipient resolution in `src/AIUsageGuard.Application/Notifications/DeliverPendingNotifications/DeliverPendingNotificationsService.cs`
- [X] T019 [US1] Implement the email sender and non-production delivery capture in `src/AIUsageGuard.Infrastructure/Notifications/EmailNotificationSender.cs`, `src/AIUsageGuard.Infrastructure/Notifications/LoggedNotificationSender.cs`, and `tests/AIUsageGuard.IntegrationTests/Infrastructure/NotificationTestSink.cs`
- [X] T020 [US1] Register urgent alert and delivery workers plus audit recording in `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Application/BackgroundJobs/RunUrgentAlertScan/RunUrgentAlertScanService.cs`, and `src/AIUsageGuard.Application/Notifications/DeliverPendingNotifications/DeliverPendingNotificationsService.cs`
- [X] T021 [US1] Extend notification integration-test helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/TestWebApplicationFactory.cs` and `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 1 is complete when urgent governance conditions
produce one workspace-scoped notification for eligible recipients with durable
delivery outcomes and duplicate suppression

---

## Phase 4: User Story 2 - Receive Scheduled Activity Digests (Priority: P2)

**Goal**: Allow workspace owners and admins to receive scheduled digest
notifications that summarize recent workspace AI activity, flagged activity,
and estimated cost for completed daily or weekly periods

**Independent Test**: A workspace with digest delivery enabled reaches its next
completed digest window and receives one workspace-scoped digest summary for
that period, while retry handling and delayed runs preserve correctness

### Tests for User Story 2 ⚠️

- [X] T022 [P] [US2] Create digest summary unit tests in `tests/AIUsageGuard.UnitTests/BackgroundJobs/RunDigestGenerationServiceTests.cs`
- [X] T023 [P] [US2] Create delivery retry and backoff unit tests in `tests/AIUsageGuard.UnitTests/BackgroundJobs/RunDeliveryRetryServiceTests.cs`
- [X] T024 [P] [US2] Create digest generation integration tests in `tests/AIUsageGuard.IntegrationTests/Notifications/DigestNotificationTests.cs`
- [X] T025 [P] [US2] Create retry, empty-state digest, and delayed-run integration tests in `tests/AIUsageGuard.IntegrationTests/Notifications/NotificationRetryTests.cs`

### Implementation for User Story 2

- [X] T026 [P] [US2] Create digest generation and retry workflow models in `src/AIUsageGuard.Application/BackgroundJobs/RunDigestGeneration/RunDigestGenerationCommand.cs`, `src/AIUsageGuard.Application/BackgroundJobs/RunDigestGeneration/RunDigestGenerationResult.cs`, `src/AIUsageGuard.Application/BackgroundJobs/RunDeliveryRetry/RunDeliveryRetryCommand.cs`, and `src/AIUsageGuard.Application/BackgroundJobs/RunDeliveryRetry/RunDeliveryRetryResult.cs`
- [X] T027 [US2] Implement scheduled digest generation and reporting-period shaping in `src/AIUsageGuard.Application/BackgroundJobs/RunDigestGeneration/RunDigestGenerationService.cs`
- [X] T028 [US2] Implement retry scheduling, capped backoff, and final-outcome promotion in `src/AIUsageGuard.Application/BackgroundJobs/RunDeliveryRetry/RunDeliveryRetryService.cs` and `src/AIUsageGuard.Application/Notifications/DeliverPendingNotifications/DeliverPendingNotificationsService.cs`
- [X] T029 [US2] Extend background-worker orchestration for digest and retry scans in `src/AIUsageGuard.Infrastructure/BackgroundProcessing/NotificationBackgroundWorker.cs` and `src/AIUsageGuard.Infrastructure/BackgroundProcessing/BackgroundJobScopeRunner.cs`
- [X] T030 [US2] Add digest and retry audit recording plus structured logging in `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Application/BackgroundJobs/RunDigestGeneration/RunDigestGenerationService.cs`, and `src/AIUsageGuard.Application/BackgroundJobs/RunDeliveryRetry/RunDeliveryRetryService.cs`

**Checkpoint**: User Story 2 is complete when scheduled digest windows generate
one workspace-only summary for the correct completed period and failed delivery
attempts retry to a clear final outcome

---

## Phase 5: User Story 3 - Manage Notification Preferences and Outcomes (Priority: P3)

**Goal**: Allow workspace owners and admins to configure notification
preferences and review workspace-scoped notification history and delivery
outcomes without exposing other workspaces

**Independent Test**: An authorized workspace owner or admin updates
notification preferences, then retrieves notification history and detail for
that workspace while non-admin and cross-workspace access attempts are denied

### Tests for User Story 3 ⚠️

- [X] T031 [P] [US3] Create notification preference validation unit tests in `tests/AIUsageGuard.UnitTests/Notifications/UpdateWorkspaceNotificationPreferencesServiceTests.cs`
- [X] T032 [P] [US3] Create notification API contract tests in `tests/AIUsageGuard.IntegrationTests/Notifications/NotificationContractsTests.cs`
- [X] T033 [P] [US3] Create notification preference and history integration tests in `tests/AIUsageGuard.IntegrationTests/Notifications/NotificationPreferencesAndHistoryTests.cs`
- [X] T034 [P] [US3] Create notification authorization and cross-workspace tests in `tests/AIUsageGuard.IntegrationTests/Notifications/NotificationAuthorizationTests.cs`

### Implementation for User Story 3

- [X] T035 [P] [US3] Create notification preference and history request/response contracts in `src/AIUsageGuard.Api/Contracts/Notifications/UpdateNotificationPreferenceRequest.cs`, `src/AIUsageGuard.Api/Contracts/Notifications/NotificationPreferenceResponse.cs`, `src/AIUsageGuard.Api/Contracts/Notifications/NotificationListResponse.cs`, `src/AIUsageGuard.Api/Contracts/Notifications/NotificationListItemResponse.cs`, `src/AIUsageGuard.Api/Contracts/Notifications/NotificationDetailResponse.cs`, and `src/AIUsageGuard.Api/Contracts/Notifications/NotificationDeliveryOutcomeResponse.cs`
- [X] T036 [P] [US3] Create preference and history query/result models in `src/AIUsageGuard.Application/Notifications/GetNotificationPreferences/GetNotificationPreferencesQuery.cs`, `src/AIUsageGuard.Application/Notifications/GetNotificationPreferences/GetNotificationPreferencesResult.cs`, `src/AIUsageGuard.Application/Notifications/ConfigureWorkspaceNotifications/UpdateWorkspaceNotificationPreferencesCommand.cs`, `src/AIUsageGuard.Application/Notifications/ConfigureWorkspaceNotifications/UpdateWorkspaceNotificationPreferencesResult.cs`, `src/AIUsageGuard.Application/Notifications/ListNotifications/ListNotificationsQuery.cs`, `src/AIUsageGuard.Application/Notifications/ListNotifications/ListNotificationsResult.cs`, `src/AIUsageGuard.Application/Notifications/GetNotification/GetNotificationQuery.cs`, and `src/AIUsageGuard.Application/Notifications/GetNotification/GetNotificationResult.cs`
- [X] T037 [US3] Implement notification preference read/update logic in `src/AIUsageGuard.Application/Notifications/GetNotificationPreferences/GetNotificationPreferencesService.cs` and `src/AIUsageGuard.Application/Notifications/ConfigureWorkspaceNotifications/UpdateWorkspaceNotificationPreferencesService.cs`
- [X] T038 [US3] Implement notification history read models and delivery-outcome filtering in `src/AIUsageGuard.Application/Notifications/ListNotifications/ListNotificationsService.cs` and `src/AIUsageGuard.Application/Notifications/GetNotification/GetNotificationService.cs`
- [X] T039 [US3] Implement `GET/PUT /workspaces/{workspaceId}/notification-preferences`, `GET /workspaces/{workspaceId}/notifications`, and `GET /workspaces/{workspaceId}/notifications/{notificationId}` in `src/AIUsageGuard.Api/Controllers/NotificationPreferencesController.cs` and `src/AIUsageGuard.Api/Controllers/NotificationsController.cs`
- [X] T040 [US3] Register notification administration services and preference/history audit recording in `src/AIUsageGuard.Api/Program.cs` and `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`
- [X] T041 [US3] Extend notification client helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 3 is complete when admins can manage notification
preferences and inspect workspace-only notification history and delivery
outcomes through stable APIs

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish observability, regression safety, contract alignment, and
manual verification coverage across all notification stories

- [X] T042 [P] Add cross-story notification regression coverage in `tests/AIUsageGuard.IntegrationTests/Notifications/NotificationRegressionTests.cs`
- [X] T043 [P] Refine structured logging, counters, and health-oriented telemetry in `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Application/BackgroundJobs/RunUrgentAlertScan/RunUrgentAlertScanService.cs`, `src/AIUsageGuard.Application/BackgroundJobs/RunDigestGeneration/RunDigestGenerationService.cs`, `src/AIUsageGuard.Application/BackgroundJobs/RunDeliveryRetry/RunDeliveryRetryService.cs`, and `src/AIUsageGuard.Application/Notifications/DeliverPendingNotifications/DeliverPendingNotificationsService.cs`
- [X] T044 [P] Align final OpenAPI examples and failure responses in `specs/006-background-jobs-notifications/contracts/notifications.openapi.yaml`
- [X] T045 [P] Update the manual verification requests in `src/AIUsageGuard.Api/AIUsageGuard.Api.http`
- [X] T046 Run the quickstart validation pass and capture any wording fixes in `specs/006-background-jobs-notifications/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion and is the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and is easiest after User Story 1 because it reuses the same delivery pipeline and durable notification records
- **User Story 3 (Phase 5)**: Depends on Foundational completion and is easiest after User Story 1 and User Story 2 because it exposes the operator APIs over the persisted preference and notification data they create
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational and establishes the first working notification delivery surface
- **User Story 2 (P2)**: Can start after Foundational, but is easiest after User Story 1 because it reuses the pending-delivery workflow and notification persistence
- **User Story 3 (P3)**: Can start after Foundational, but its full product value depends on the durable preference and history data exercised by User Story 1 and User Story 2

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Contracts and query models before service logic
- Shared delivery and persistence behavior before controllers or history views that depend on it
- Background automation before story sign-off
- Audit, authorization, and observability work before story sign-off
- Cheaper models should execute one unchecked task at a time and avoid combining tasks that touch the same file

### Parallel Opportunities

- T002 can run in parallel with T001
- T004 and T005 can run in parallel after T003
- T011, T012, T013, and T014 can run in parallel
- T015 and T016 can run in parallel
- T022, T023, T024, and T025 can run in parallel
- T031, T032, T033, and T034 can run in parallel
- T035 and T036 can run in parallel
- T042, T043, T044, and T045 can run in parallel

---

## Parallel Example: User Story 1

```text
Task: "Create urgent alert aggregation and deduplication unit tests in tests/AIUsageGuard.UnitTests/BackgroundJobs/RunUrgentAlertScanServiceTests.cs"
Task: "Create pending-notification delivery unit tests in tests/AIUsageGuard.UnitTests/Notifications/DeliverPendingNotificationsServiceTests.cs"
Task: "Create urgent alert delivery integration tests in tests/AIUsageGuard.IntegrationTests/Notifications/UrgentAlertNotificationTests.cs"
Task: "Create urgent alert isolation and duplicate-suppression tests in tests/AIUsageGuard.IntegrationTests/Notifications/UrgentAlertIsolationTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Create digest summary unit tests in tests/AIUsageGuard.UnitTests/BackgroundJobs/RunDigestGenerationServiceTests.cs"
Task: "Create delivery retry and backoff unit tests in tests/AIUsageGuard.UnitTests/BackgroundJobs/RunDeliveryRetryServiceTests.cs"
Task: "Create digest generation integration tests in tests/AIUsageGuard.IntegrationTests/Notifications/DigestNotificationTests.cs"
Task: "Create retry, empty-state digest, and delayed-run integration tests in tests/AIUsageGuard.IntegrationTests/Notifications/NotificationRetryTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "Create notification preference validation unit tests in tests/AIUsageGuard.UnitTests/Notifications/UpdateWorkspaceNotificationPreferencesServiceTests.cs"
Task: "Create notification API contract tests in tests/AIUsageGuard.IntegrationTests/Notifications/NotificationContractsTests.cs"
Task: "Create notification preference and history integration tests in tests/AIUsageGuard.IntegrationTests/Notifications/NotificationPreferencesAndHistoryTests.cs"
Task: "Create notification authorization and cross-workspace tests in tests/AIUsageGuard.IntegrationTests/Notifications/NotificationAuthorizationTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm a workspace can receive one urgent
   workspace-scoped governance alert with durable outcomes and duplicate suppression

### Incremental Delivery

1. Build the shared notification models, persistence, and worker foundation
2. Deliver urgent governance alerts for eligible workspace admins
3. Deliver scheduled digest generation plus retry and final-outcome handling
4. Deliver notification preference and history APIs for admins
5. Finish with regression coverage, telemetry, contract cleanup, and quickstart validation

### Parallel Team Strategy

1. One developer handles shared notification models, persistence, and hosted-worker plumbing
2. One developer handles urgent alert generation and delivery tests
3. One developer handles digest and retry workflows
4. After the foundation is stable, one developer handles notification preference and history APIs while shared files such as `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`, `src/AIUsageGuard.Api/Program.cs`, and `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs` stay sequential

---

## Notes

- The tasks are intentionally small and file-scoped so a cheaper LLM can implement them one by one without broad architectural inference
- User Story 1 is the recommended MVP scope
- Do not start User Story 2 or User Story 3 before the Foundational phase is complete
- If a task touches the same file as an unfinished earlier task, finish the earlier task first instead of parallelizing
