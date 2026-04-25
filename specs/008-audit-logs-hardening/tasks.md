# Tasks: Phase 7 Audit Logs and Hardening

**Input**: Design documents from `/specs/008-audit-logs-hardening/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for this feature. This task list keeps
tests explicit and front-loaded so audit-history behavior, tenant isolation,
security-relevant evidence, login hardening, and protected-request integrity
can be verified in small slices.

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
- Feature docs: `specs/008-audit-logs-hardening/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Phase 7 audit-log and hardening surface area plus
manual verification entry points before deeper implementation begins

- [X] T001 Create the audit-log feature folders under `src/AIUsageGuard.Api/Contracts/AuditLogs/`, `src/AIUsageGuard.Api/Controllers/`, `src/AIUsageGuard.Application/Auditing/ListAuditLogs/`, `src/AIUsageGuard.Application/Auditing/GetAuditLog/`, `tests/AIUsageGuard.UnitTests/Auditing/`, `tests/AIUsageGuard.UnitTests/Identity/`, `tests/AIUsageGuard.UnitTests/Security/`, `tests/AIUsageGuard.IntegrationTests/Auditing/`, and `tests/AIUsageGuard.IntegrationTests/Security/`
- [X] T002 [P] Add an "Audit Logs and Hardening" manual request section to `src/AIUsageGuard.Api/AIUsageGuard.Api.http`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared audit-log models, persistence, query helpers, and
security options required by all user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Extend the audit record model in `src/AIUsageGuard.Application/Models/AuditRecord.cs` with category, security-relevance, correlation, and minimized client-context fields
- [X] T004 [P] Extend the user account model in `src/AIUsageGuard.Application/Models/UserAccount.cs` with failed-sign-in tracking and temporary lockout fields
- [X] T005 [P] Create shared audit-log read models in `src/AIUsageGuard.Application/Models/AuditLogFilter.cs`, `src/AIUsageGuard.Application/Models/AuditLogListItem.cs`, `src/AIUsageGuard.Application/Models/AuditLogDetail.cs`, and `src/AIUsageGuard.Application/Models/AuditLogPage.cs`
- [X] T006 [P] Create shared hardening option models in `src/AIUsageGuard.Application/Security/SignInHardeningOptions.cs` and `src/AIUsageGuard.Application/Security/ProtectedRequestIntegrityOptions.cs`
- [X] T007 Extend the audit and user hardening store contract in `src/AIUsageGuard.Application/Abstractions/IPlatformStore.cs` with audit-log list/count/detail and user-account lockout update methods
- [X] T008 Implement audit-log query helpers and user hardening persistence in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T009 Add audit-record and user-account persistence configuration updates in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/AuditRecordConfiguration.cs`, `src/AIUsageGuard.Infrastructure/Identity/ApplicationUser.cs`, and `src/AIUsageGuard.Infrastructure/Persistence/Migrations/`
- [X] T010 Create shared audit categorization and request-context helpers in `src/AIUsageGuard.Application/Auditing/AuditCategoryResolver.cs` and `src/AIUsageGuard.Infrastructure/Auditing/AuditRequestContextAccessor.cs`
- [X] T011 Register Phase 7 audit-log services, request-context access, antiforgery, and hardening options in `src/AIUsageGuard.Api/Program.cs`
- [X] T012 Add audit-log authorization action mapping in `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`

**Checkpoint**: Foundation ready - audit-history reads and hardening behavior
can now be implemented

---

## Phase 3: User Story 1 - Review Workspace Audit History (Priority: P1) 🎯 MVP

**Goal**: Allow workspace owners and admins to list and open workspace audit
history with stable paging and investigation-friendly fields

**Independent Test**: An authorized workspace owner or admin requests workspace
audit history and can inspect one entry detail with correct tenant scoping,
stable ordering, and useful actor/action/outcome fields, while member and
cross-workspace access attempts are denied

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T013 [P] [US1] Create audit-log list unit tests in `tests/AIUsageGuard.UnitTests/Auditing/ListAuditLogsServiceTests.cs`
- [X] T014 [P] [US1] Create audit-log detail unit tests in `tests/AIUsageGuard.UnitTests/Auditing/GetAuditLogServiceTests.cs`
- [X] T015 [P] [US1] Create audit-log API contract tests in `tests/AIUsageGuard.IntegrationTests/Auditing/AuditLogContractTests.cs`
- [X] T016 [P] [US1] Create audit-log history integration tests in `tests/AIUsageGuard.IntegrationTests/Auditing/AuditLogHistoryTests.cs`
- [X] T017 [P] [US1] Create audit-log authorization and cross-workspace tests in `tests/AIUsageGuard.IntegrationTests/Auditing/AuditLogAuthorizationTests.cs`

### Implementation for User Story 1

- [X] T018 [P] [US1] Create audit-log request and response contracts in `src/AIUsageGuard.Api/Contracts/AuditLogs/AuditLogListResponse.cs`, `src/AIUsageGuard.Api/Contracts/AuditLogs/AuditLogPageResponse.cs`, `src/AIUsageGuard.Api/Contracts/AuditLogs/AuditLogSummaryResponse.cs`, `src/AIUsageGuard.Api/Contracts/AuditLogs/AuditLogDetailResponse.cs`, and `src/AIUsageGuard.Api/Contracts/AuditLogs/AuditLogClientContextResponse.cs`
- [X] T019 [P] [US1] Create audit-log query and result models in `src/AIUsageGuard.Application/Auditing/ListAuditLogs/ListAuditLogsQuery.cs`, `src/AIUsageGuard.Application/Auditing/ListAuditLogs/ListAuditLogsResult.cs`, `src/AIUsageGuard.Application/Auditing/GetAuditLog/GetAuditLogQuery.cs`, and `src/AIUsageGuard.Application/Auditing/GetAuditLog/GetAuditLogResult.cs`
- [X] T020 [US1] Implement workspace audit-log list retrieval in `src/AIUsageGuard.Application/Auditing/ListAuditLogs/ListAuditLogsService.cs`
- [X] T021 [US1] Implement workspace audit-log detail retrieval in `src/AIUsageGuard.Application/Auditing/GetAuditLog/GetAuditLogService.cs`
- [X] T022 [US1] Create audit-log response mapping in `src/AIUsageGuard.Api/Contracts/AuditLogs/AuditLogResponseFactory.cs`
- [X] T023 [US1] Implement `GET /workspaces/{workspaceId}/audit-logs` and `GET /workspaces/{workspaceId}/audit-logs/{auditLogId}` in `src/AIUsageGuard.Api/Controllers/AuditLogsController.cs`
- [X] T024 [US1] Register audit-log services in `src/AIUsageGuard.Api/Program.cs`
- [X] T025 [US1] Extend audit integration-test helpers in `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs`

**Checkpoint**: User Story 1 is complete when an admin can investigate one
workspace's audit history through a stable API with correct tenant and
authorization behavior

---

## Phase 4: User Story 2 - Investigate Security-Relevant Activity (Priority: P2)

**Goal**: Make denied and security-relevant audit events easy to find, filter,
and explain during workspace investigations

**Independent Test**: A workspace triggers denied and sensitive actions, and an
authorized owner or admin can filter audit history by action, result, actor,
and security relevance to explain what happened without seeing another
workspace's evidence

### Tests for User Story 2 ⚠️

- [X] T026 [P] [US2] Create audit categorization unit tests in `tests/AIUsageGuard.UnitTests/Auditing/AuditCategoryResolverTests.cs`
- [X] T027 [P] [US2] Create audit context enrichment unit tests in `tests/AIUsageGuard.UnitTests/Auditing/AuditServiceTests.cs`
- [X] T028 [P] [US2] Create audit-log filtering integration tests in `tests/AIUsageGuard.IntegrationTests/Auditing/AuditLogFilteringTests.cs`
- [X] T029 [P] [US2] Create denied and security-relevant investigation integration tests in `tests/AIUsageGuard.IntegrationTests/Auditing/SecurityAuditInvestigationTests.cs`

### Implementation for User Story 2

- [X] T030 [P] [US2] Implement audit category and security-relevance resolution in `src/AIUsageGuard.Application/Auditing/AuditCategoryResolver.cs`
- [X] T031 [P] [US2] Implement minimized request-context capture in `src/AIUsageGuard.Infrastructure/Auditing/AuditRequestContextAccessor.cs`
- [X] T032 [US2] Enrich persisted audit records in `src/AIUsageGuard.Infrastructure/Auditing/AuditService.cs`
- [X] T033 [US2] Add correlation-id and request-context propagation in `src/AIUsageGuard.Api/Program.cs`
- [X] T034 [US2] Align denied and protected auth audit behavior in `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs` and `src/AIUsageGuard.Api/Controllers/AuthController.cs`
- [X] T035 [US2] Update audit-log query filtering and response mapping in `src/AIUsageGuard.Application/Auditing/ListAuditLogs/ListAuditLogsService.cs`, `src/AIUsageGuard.Application/Auditing/GetAuditLog/GetAuditLogService.cs`, and `src/AIUsageGuard.Api/Contracts/AuditLogs/AuditLogResponseFactory.cs`
- [X] T036 [US2] Align final OpenAPI examples and filter behavior in `specs/008-audit-logs-hardening/contracts/audit-logs.openapi.yaml`

**Checkpoint**: User Story 2 is complete when denied and security-relevant
events are discoverable and explainable through audit history with stable
filter behavior

---

## Phase 5: User Story 3 - Rely on Hardened Security Defaults (Priority: P3)

**Goal**: Apply stronger default protections for repeated invalid sign-in
attempts and authenticated protected writes while preserving auditable
outcomes

**Independent Test**: Repeated invalid sign-ins trigger temporary lockout, and
authenticated protected writes without valid integrity context are rejected with
safe failure responses and matching audit evidence

### Tests for User Story 3 ⚠️

- [X] T037 [P] [US3] Create sign-in hardening unit tests in `tests/AIUsageGuard.UnitTests/Identity/SignInGuardServiceTests.cs`
- [X] T038 [P] [US3] Create protected-request integrity unit tests in `tests/AIUsageGuard.UnitTests/Security/ProtectedRequestIntegrityServiceTests.cs`
- [X] T039 [P] [US3] Create login hardening integration tests in `tests/AIUsageGuard.IntegrationTests/Security/LoginHardeningTests.cs`
- [X] T040 [P] [US3] Create protected-write integrity integration tests in `tests/AIUsageGuard.IntegrationTests/Security/ProtectedWriteHardeningTests.cs`

### Implementation for User Story 3

- [X] T041 [P] [US3] Implement sign-in hardening rules in `src/AIUsageGuard.Application/Identity/SignInGuardService.cs`
- [X] T042 [P] [US3] Implement protected-request integrity evaluation in `src/AIUsageGuard.Application/Security/ProtectedRequestIntegrityService.cs`
- [X] T043 [US3] Implement temporary lockout and failed-sign-in audit handling in `src/AIUsageGuard.Api/Controllers/AuthController.cs`
- [X] T044 [US3] Add protected-request integrity filter support in `src/AIUsageGuard.Api/Security/RequireProtectedRequestIntegrityAttribute.cs` and `src/AIUsageGuard.Api/Security/ProtectedRequestIntegrityFilter.cs`
- [X] T045 [US3] Register hardening filters, antiforgery behavior, and minimized hardening failures in `src/AIUsageGuard.Api/Program.cs`
- [X] T046 [US3] Apply protected-request integrity to authenticated write endpoints in `src/AIUsageGuard.Api/Controllers/AIUsageEventsController.cs` and `src/AIUsageGuard.Api/Controllers/MembershipsController.cs`
- [X] T047 [US3] Apply protected-request integrity to authenticated write endpoints in `src/AIUsageGuard.Api/Controllers/NotificationPreferencesController.cs` and `src/AIUsageGuard.Api/Controllers/RiskPolicyController.cs`
- [X] T048 [US3] Ensure hardening rejections are audit-visible and workspace-safe in `src/AIUsageGuard.Infrastructure/Auditing/AuditService.cs`, `src/AIUsageGuard.Application/Identity/SignInGuardService.cs`, and `src/AIUsageGuard.Application/Security/ProtectedRequestIntegrityService.cs`

**Checkpoint**: User Story 3 is complete when invalid sign-in abuse and invalid
authenticated write context are safely rejected and recorded without weakening
workspace boundaries

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish regression safety, observability, manual verification
coverage, and task sharpness across all audit and hardening stories

- [X] T049 [P] Add cross-story audit and hardening regression coverage in `tests/AIUsageGuard.IntegrationTests/Auditing/AuditHardeningRegressionTests.cs`
- [X] T050 [P] Refine structured logging, counters, and standardized failure responses in `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Infrastructure/Auditing/AuditService.cs`, `src/AIUsageGuard.Application/Identity/SignInGuardService.cs`, and `src/AIUsageGuard.Application/Security/ProtectedRequestIntegrityService.cs`
- [X] T051 [P] Update the manual verification requests in `src/AIUsageGuard.Api/AIUsageGuard.Api.http`
- [X] T052 Run the quickstart validation pass and capture any wording fixes in `specs/008-audit-logs-hardening/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion and is the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and is easiest after User Story 1 because audit-history APIs are the primary investigation surface
- **User Story 3 (Phase 5)**: Depends on Foundational completion and is easiest after User Story 2 because hardening outcomes are most valuable once audit investigation is already available
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational and establishes the first working audit-log surface
- **User Story 2 (P2)**: Can start after Foundational, but is easiest after User Story 1 because investigation filtering builds directly on the audit-log read path
- **User Story 3 (P3)**: Can start after Foundational, but its full operator value depends on the audit-history visibility produced by User Stories 1 and 2

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Contracts and query models before service logic
- Shared persistence and request-context behavior before controllers or hardening integration that depends on them
- Audit, authorization, and observability work before story sign-off
- Cheaper models should execute one unchecked task at a time and avoid combining tasks that touch the same file

### Parallel Opportunities

- T002 can run in parallel with T001
- T004, T005, and T006 can run in parallel after T003
- T013, T014, T015, T016, and T017 can run in parallel
- T018 and T019 can run in parallel
- T026, T027, T028, and T029 can run in parallel
- T037, T038, T039, and T040 can run in parallel
- T049, T050, and T051 can run in parallel

---

## Parallel Example: User Story 1

```text
Task: "Create audit-log list unit tests in tests/AIUsageGuard.UnitTests/Auditing/ListAuditLogsServiceTests.cs"
Task: "Create audit-log detail unit tests in tests/AIUsageGuard.UnitTests/Auditing/GetAuditLogServiceTests.cs"
Task: "Create audit-log API contract tests in tests/AIUsageGuard.IntegrationTests/Auditing/AuditLogContractTests.cs"
Task: "Create audit-log history integration tests in tests/AIUsageGuard.IntegrationTests/Auditing/AuditLogHistoryTests.cs"
Task: "Create audit-log authorization and cross-workspace tests in tests/AIUsageGuard.IntegrationTests/Auditing/AuditLogAuthorizationTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Create audit categorization unit tests in tests/AIUsageGuard.UnitTests/Auditing/AuditCategoryResolverTests.cs"
Task: "Create audit context enrichment unit tests in tests/AIUsageGuard.UnitTests/Auditing/AuditServiceTests.cs"
Task: "Create audit-log filtering integration tests in tests/AIUsageGuard.IntegrationTests/Auditing/AuditLogFilteringTests.cs"
Task: "Create denied and security-relevant investigation integration tests in tests/AIUsageGuard.IntegrationTests/Auditing/SecurityAuditInvestigationTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "Create sign-in hardening unit tests in tests/AIUsageGuard.UnitTests/Identity/SignInGuardServiceTests.cs"
Task: "Create protected-request integrity unit tests in tests/AIUsageGuard.UnitTests/Security/ProtectedRequestIntegrityServiceTests.cs"
Task: "Create login hardening integration tests in tests/AIUsageGuard.IntegrationTests/Security/LoginHardeningTests.cs"
Task: "Create protected-write integrity integration tests in tests/AIUsageGuard.IntegrationTests/Security/ProtectedWriteHardeningTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm an admin can list and open workspace audit history through the audit-log API

### Incremental Delivery

1. Build the shared audit-log models, persistence, context capture, and hardening options
2. Deliver workspace audit-history reads for workspace admins
3. Deliver richer investigation filtering and security-relevant audit evidence
4. Deliver login hardening and protected-write integrity enforcement
5. Finish with regression coverage, observability cleanup, and quickstart validation

### Parallel Team Strategy

1. One developer handles shared audit models, persistence, and request-context plumbing
2. One developer handles audit-log APIs and tests
3. One developer handles security-relevant audit enrichment
4. After the foundation is stable, one developer handles login and protected-write hardening while shared files such as `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`, `src/AIUsageGuard.Api/Program.cs`, `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`, and `tests/AIUsageGuard.IntegrationTests/Infrastructure/ApiTestClient.cs` stay sequential

---

## Notes

- The tasks are intentionally small and file-scoped so a cheaper LLM can implement them one by one without broad architectural inference
- User Story 1 is the recommended MVP scope
- Do not start User Story 2 or User Story 3 before the Foundational phase is complete
- If a task touches the same file as an unfinished earlier task, finish the earlier task first instead of parallelizing
