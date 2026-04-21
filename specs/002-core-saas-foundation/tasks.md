# Tasks: Phase 1 Core SaaS Foundation

**Input**: Design documents from `/specs/002-core-saas-foundation/`
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
- Feature docs: `specs/002-core-saas-foundation/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the solution and project skeleton with explicit paths and minimal ambiguity

- [X] T001 Create the solution file in `AIUsageGuard.slnx`
- [X] T002 Create the API host project file in `src/AIUsageGuard.Api/AIUsageGuard.Api.csproj`
- [X] T003 [P] Create the application project file in `src/AIUsageGuard.Application/AIUsageGuard.Application.csproj`
- [X] T004 [P] Create the domain project file in `src/AIUsageGuard.Domain/AIUsageGuard.Domain.csproj`
- [X] T005 [P] Create the infrastructure project file in `src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj`
- [X] T006 [P] Create the unit test project file in `tests/AIUsageGuard.UnitTests/AIUsageGuard.UnitTests.csproj`
- [X] T007 [P] Create the integration test project file in `tests/AIUsageGuard.IntegrationTests/AIUsageGuard.IntegrationTests.csproj`
- [X] T008 Add solution membership entries in `AIUsageGuard.slnx`
- [X] T009 Add API project references in `src/AIUsageGuard.Api/AIUsageGuard.Api.csproj`
- [X] T010 Add application project references in `src/AIUsageGuard.Application/AIUsageGuard.Application.csproj`
- [X] T011 Add infrastructure project references in `src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj`
- [X] T012 Create base configuration in `src/AIUsageGuard.Api/appsettings.json`
- [X] T013 Create development configuration in `src/AIUsageGuard.Api/appsettings.Development.json`
- [X] T014 Create the initial host bootstrap in `src/AIUsageGuard.Api/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared tenant, identity, authorization, persistence, and audit infrastructure required by all stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T015 Create the workspace entity in `src/AIUsageGuard.Domain/Workspaces/Workspace.cs`
- [X] T016 [P] Create the membership role enum in `src/AIUsageGuard.Domain/Memberships/WorkspaceRole.cs`
- [X] T017 [P] Create the membership status enum in `src/AIUsageGuard.Domain/Memberships/MembershipStatus.cs`
- [X] T018 Create the workspace membership entity in `src/AIUsageGuard.Domain/Memberships/WorkspaceMembership.cs`
- [X] T019 Create the audit record entity in `src/AIUsageGuard.Domain/Auditing/AuditRecord.cs`
- [X] T020 Create the Identity user type in `src/AIUsageGuard.Infrastructure/Identity/ApplicationUser.cs`
- [X] T021 Create the EF Core database context in `src/AIUsageGuard.Infrastructure/Persistence/ApplicationDbContext.cs`
- [X] T022 [P] Create the workspace entity configuration in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/WorkspaceConfiguration.cs`
- [X] T023 [P] Create the membership entity configuration in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/WorkspaceMembershipConfiguration.cs`
- [X] T024 [P] Create the audit entity configuration in `src/AIUsageGuard.Infrastructure/Persistence/Configurations/AuditRecordConfiguration.cs`
- [X] T025 Create the workspace context model in `src/AIUsageGuard.Infrastructure/Tenancy/WorkspaceContext.cs`
- [X] T026 Create the workspace context accessor in `src/AIUsageGuard.Infrastructure/Tenancy/WorkspaceContextAccessor.cs`
- [X] T027 [P] Create the member authorization requirement in `src/AIUsageGuard.Api/Policies/WorkspaceMemberRequirement.cs`
- [X] T028 [P] Create the role authorization requirement in `src/AIUsageGuard.Api/Policies/WorkspaceRoleRequirement.cs`
- [X] T029 Create the authorization handler in `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`
- [X] T030 Create the audit service contract in `src/AIUsageGuard.Application/Auditing/IAuditService.cs`
- [X] T031 Create the audit service implementation in `src/AIUsageGuard.Infrastructure/Auditing/AuditService.cs`
- [X] T032 Configure Identity and database registration in `src/AIUsageGuard.Api/Program.cs`
- [X] T033 Configure authorization policies and handlers in `src/AIUsageGuard.Api/Program.cs`
- [X] T034 Configure ProblemDetails and health checks in `src/AIUsageGuard.Api/Program.cs`
- [X] T035 Create the initial database migration files in `src/AIUsageGuard.Infrastructure/Persistence/Migrations/`
- [X] T036 Create the integration test host fixture in `tests/AIUsageGuard.IntegrationTests/Infrastructure/TestWebApplicationFactory.cs`
- [X] T037 Create the integration test database helper in `tests/AIUsageGuard.IntegrationTests/Infrastructure/DatabaseFixture.cs`

**Checkpoint**: Foundation ready - onboarding, membership, and member access stories can now be implemented

---

## Phase 3: User Story 1 - Create Workspace and Owner Access (Priority: P1) 🎯 MVP

**Goal**: Let a new customer register, create a workspace, sign in, and land in the correct tenant context

**Independent Test**: A new user can register, create a workspace, sign in, and retrieve the workspace context response without any pre-existing data

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T038 [P] [US1] Create registration integration tests in `tests/AIUsageGuard.IntegrationTests/Auth/RegisterWorkspaceTests.cs`
- [X] T039 [P] [US1] Create login integration tests in `tests/AIUsageGuard.IntegrationTests/Auth/LoginTests.cs`
- [X] T040 [P] [US1] Create workspace context integration tests in `tests/AIUsageGuard.IntegrationTests/Workspaces/WorkspaceContextTests.cs`
- [X] T041 [P] [US1] Create onboarding contract checks in `tests/AIUsageGuard.IntegrationTests/Auth/RegisterWorkspaceContractTests.cs`

### Implementation for User Story 1

- [X] T042 [P] [US1] Create the registration request contract in `src/AIUsageGuard.Api/Contracts/Auth/RegisterWorkspaceRequest.cs`
- [X] T043 [P] [US1] Create the workspace session response contract in `src/AIUsageGuard.Api/Contracts/Auth/WorkspaceSessionResponse.cs`
- [X] T044 [P] [US1] Create the login request contract in `src/AIUsageGuard.Api/Contracts/Auth/LoginRequest.cs`
- [X] T045 [US1] Create the workspace creation service in `src/AIUsageGuard.Application/Workspaces/CreateWorkspace/CreateWorkspaceService.cs`
- [X] T046 [US1] Create the workspace context query service in `src/AIUsageGuard.Application/Workspaces/GetWorkspaceContext/GetWorkspaceContextService.cs`
- [X] T047 [US1] Implement registration action in `src/AIUsageGuard.Api/Controllers/AuthController.cs`
- [X] T048 [US1] Implement login action in `src/AIUsageGuard.Api/Controllers/AuthController.cs`
- [X] T049 [US1] Implement logout action in `src/AIUsageGuard.Api/Controllers/AuthController.cs`
- [X] T050 [US1] Create the workspace context response contract in `src/AIUsageGuard.Api/Contracts/Workspaces/WorkspaceContextResponse.cs`
- [X] T051 [US1] Implement the workspace context endpoint in `src/AIUsageGuard.Api/Controllers/WorkspaceContextController.cs`
- [X] T052 [US1] Connect the workspace context endpoint to the query service in `src/AIUsageGuard.Api/Controllers/WorkspaceContextController.cs`
- [X] T053 [US1] Add workspace creation audit recording in `src/AIUsageGuard.Infrastructure/Auditing/AuditService.cs`
- [X] T054 [US1] Add sign-in and sign-out audit recording in `src/AIUsageGuard.Api/Controllers/AuthController.cs`

**Checkpoint**: User Story 1 is complete when a new customer can onboard into an isolated workspace and sign in successfully

---

## Phase 4: User Story 2 - Manage Workspace Members and Roles (Priority: P2)

**Goal**: Allow owners and admins to manage memberships and role assignments inside their own workspace

**Independent Test**: An owner or admin can add a user, change a role, and deactivate access without affecting any other workspace

### Tests for User Story 2 ⚠️

- [X] T055 [P] [US2] Create membership list integration tests in `tests/AIUsageGuard.IntegrationTests/Memberships/MembershipListTests.cs`
- [X] T056 [P] [US2] Create membership creation integration tests in `tests/AIUsageGuard.IntegrationTests/Memberships/MembershipCreateTests.cs`
- [X] T057 [P] [US2] Create role change and last-owner safeguard tests in `tests/AIUsageGuard.IntegrationTests/Memberships/MembershipRoleChangeTests.cs`
- [X] T058 [P] [US2] Create membership authorization tests in `tests/AIUsageGuard.IntegrationTests/Memberships/MembershipAuthorizationTests.cs`

### Implementation for User Story 2

- [X] T059 [P] [US2] Create the membership creation request model in `src/AIUsageGuard.Api/Contracts/Memberships/CreateMembershipRequest.cs`
- [X] T060 [P] [US2] Create the membership update request model in `src/AIUsageGuard.Api/Contracts/Memberships/UpdateMembershipRequest.cs`
- [X] T061 [P] [US2] Create the membership response model in `src/AIUsageGuard.Api/Contracts/Memberships/MembershipResponse.cs`
- [X] T062 [US2] Create the membership listing service in `src/AIUsageGuard.Application/Memberships/ListMemberships/ListMembershipsService.cs`
- [X] T063 [US2] Create the membership creation service in `src/AIUsageGuard.Application/Memberships/CreateMembership/CreateMembershipService.cs`
- [X] T064 [US2] Create the membership update service in `src/AIUsageGuard.Application/Memberships/UpdateMembership/UpdateMembershipService.cs`
- [X] T065 [US2] Add last-owner protection in `src/AIUsageGuard.Application/Memberships/UpdateMembership/UpdateMembershipService.cs`
- [X] T066 [US2] Add membership status transition rules in `src/AIUsageGuard.Application/Memberships/UpdateMembership/UpdateMembershipService.cs`
- [X] T067 [US2] Implement the membership list endpoint in `src/AIUsageGuard.Api/Controllers/MembershipsController.cs`
- [X] T068 [US2] Implement the membership creation endpoint in `src/AIUsageGuard.Api/Controllers/MembershipsController.cs`
- [X] T069 [US2] Implement the membership update endpoint in `src/AIUsageGuard.Api/Controllers/MembershipsController.cs`
- [X] T070 [US2] Add membership change audit recording in `src/AIUsageGuard.Infrastructure/Auditing/AuditService.cs`
- [X] T071 [US2] Wire membership audit calls from the controller in `src/AIUsageGuard.Api/Controllers/MembershipsController.cs`

**Checkpoint**: User Story 2 is complete when workspace admins can manage members safely inside their own tenant only

---

## Phase 5: User Story 3 - Sign In as a Workspace Member (Priority: P3)

**Goal**: Let a member sign in, resolve the correct workspace context, and be denied from unauthorized tenant or admin actions

**Independent Test**: A valid member can sign in and retrieve the workspace context response, while cross-workspace and insufficient-role attempts are denied

### Tests for User Story 3 ⚠️

- [X] T072 [P] [US3] Create member workspace context tests in `tests/AIUsageGuard.IntegrationTests/Workspaces/MemberWorkspaceContextTests.cs`
- [X] T073 [P] [US3] Create cross-workspace denial tests in `tests/AIUsageGuard.IntegrationTests/Security/CrossWorkspaceAccessTests.cs`
- [X] T074 [P] [US3] Create inactive membership access tests in `tests/AIUsageGuard.IntegrationTests/Auth/InactiveMembershipAccessTests.cs`
- [X] T075 [P] [US3] Create removed membership access tests in `tests/AIUsageGuard.IntegrationTests/Auth/RemovedMembershipAccessTests.cs`

### Implementation for User Story 3

- [X] T076 [US3] Create the current workspace resolver service in `src/AIUsageGuard.Application/Workspaces/ResolveCurrentWorkspace/ResolveCurrentWorkspaceService.cs`
- [X] T077 [US3] Create the sign-in guard service in `src/AIUsageGuard.Application/Identity/SignInGuardService.cs`
- [X] T078 [US3] Enforce membership status checks during sign-in in `src/AIUsageGuard.Api/Controllers/AuthController.cs`
- [X] T079 [US3] Apply membership validation rules in `src/AIUsageGuard.Application/Identity/SignInGuardService.cs`
- [X] T080 [US3] Resolve the active workspace in `src/AIUsageGuard.Infrastructure/Tenancy/WorkspaceContextAccessor.cs`
- [X] T081 [US3] Use the resolver service from tenant context code in `src/AIUsageGuard.Application/Workspaces/ResolveCurrentWorkspace/ResolveCurrentWorkspaceService.cs`
- [X] T082 [US3] Tighten member authorization behavior in `src/AIUsageGuard.Api/Policies/WorkspaceAuthorizationHandler.cs`
- [X] T083 [US3] Update the workspace context response contract for member-safe data in `src/AIUsageGuard.Api/Contracts/Workspaces/WorkspaceContextResponse.cs`
- [X] T084 [US3] Tighten the workspace context endpoint response rules in `src/AIUsageGuard.Api/Controllers/WorkspaceContextController.cs`

**Checkpoint**: User Story 3 is complete when member access is tenant-correct and least-privilege rules are consistently enforced

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish validation, tighten observability, and make the Phase 1 package easy to verify

- [X] T085 [P] Configure structured request logging in `src/AIUsageGuard.Api/Program.cs`
- [X] T086 [P] Configure correlation support in `src/AIUsageGuard.Api/Program.cs`
- [X] T087 [P] Add unit tests for last-owner rules in `tests/AIUsageGuard.UnitTests/Memberships/UpdateMembershipLastOwnerTests.cs`
- [X] T088 [P] Add unit tests for membership status rules in `tests/AIUsageGuard.UnitTests/Memberships/UpdateMembershipStatusTests.cs`
- [X] T089 [P] Add unit tests for sign-in guard behavior in `tests/AIUsageGuard.UnitTests/Identity/SignInGuardServiceTests.cs`
- [X] T090 [P] Refine success examples in `specs/002-core-saas-foundation/contracts/foundation-api.openapi.yaml`
- [X] T091 [P] Refine failure responses in `specs/002-core-saas-foundation/contracts/foundation-api.openapi.yaml`
- [X] T092 Run the validation scenarios and update wording in `specs/002-core-saas-foundation/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion and is the MVP
- **User Story 2 (Phase 4)**: Depends on Foundational completion and uses the authenticated tenant context established in User Story 1
- **User Story 3 (Phase 5)**: Depends on Foundational completion and is safest after User Story 1 establishes the sign-in flow
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - establishes the first working tenant flow
- **User Story 2 (P2)**: Can start after Foundational but will integrate more cleanly after User Story 1 authentication paths exist
- **User Story 3 (P3)**: Depends on the sign-in and workspace-context behavior introduced in User Story 1

### Within Each User Story

- Tests first, then request and response contracts, then application services, then controllers, then audit and authorization refinements
- Tasks that edit the same file stay sequential even if they are in the same phase
- Cheaper models should execute one unchecked task at a time and avoid bundling adjacent controller edits unless both depend on the same unfinished method

### Parallel Opportunities

- T003, T004, T005, T006, and T007 can run in parallel
- T016, T017, T022, T023, T024, T027, and T028 can run in parallel
- T038, T039, T040, and T041 can run in parallel
- T042, T043, and T044 can run in parallel
- T055, T056, T057, and T058 can run in parallel
- T059, T060, and T061 can run in parallel
- T072, T073, T074, and T075 can run in parallel
- T085, T086, T087, T088, T089, T090, and T091 can run in parallel

---

## Parallel Example: User Story 1

```text
Task: "Create registration integration tests in tests/AIUsageGuard.IntegrationTests/Auth/RegisterWorkspaceTests.cs"
Task: "Create login integration tests in tests/AIUsageGuard.IntegrationTests/Auth/LoginTests.cs"
Task: "Create workspace context integration tests in tests/AIUsageGuard.IntegrationTests/Workspaces/WorkspaceContextTests.cs"
Task: "Create onboarding contract checks in tests/AIUsageGuard.IntegrationTests/Auth/RegisterWorkspaceContractTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm a new customer can register, create a workspace, sign in, and retrieve the correct workspace context response

### Incremental Delivery

1. Build the solution and shared tenant/auth foundation
2. Deliver onboarding and first-owner access
3. Deliver membership and role management
4. Deliver member sign-in hardening and tenant-isolation verification
5. Finish with logging, unit tests, and contract/quickstart cleanup

### Parallel Team Strategy

With multiple developers:

1. One developer handles project scaffolding and host wiring
2. One developer handles domain and persistence foundations
3. One developer handles integration test harness setup
4. After foundational work, assign one developer per user story phase with controller/service ownership kept separate

---

## Notes

- The tasks are intentionally small and file-scoped so a cheaper LLM can implement them one at a time without large cross-file reasoning jumps
- Each user story includes explicit test files and implementation files so the completion path is unambiguous
- User Story 1 is the recommended MVP scope
- Avoid merging adjacent tasks unless the same model has already completed both files correctly
