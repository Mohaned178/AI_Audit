# Tasks: Phase 0 Constitution and Project Rules

**Input**: Design documents from `/specs/001-constitution-rules/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: No automated test tasks are generated for this feature because Phase 0
is a documentation and workflow-alignment change with no runtime behavior. The
required validation mechanism is artifact review against
`specs/001-constitution-rules/quickstart.md`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Governance artifacts: `.specify/memory/`, `.specify/templates/`, `.specify/extensions/`
- Feature design artifacts: `specs/001-constitution-rules/`
- Project guidance: `AGENTS.md`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm scope, target artifacts, and implementation baseline before editing shared governance files

- [X] T001 Review `specs/001-constitution-rules/spec.md` and `specs/001-constitution-rules/plan.md` to confirm Phase 0 scope, user stories, and constraints
- [X] T002 [P] Review `specs/001-constitution-rules/research.md` and `specs/001-constitution-rules/data-model.md` to map decisions and governance entities to implementation work
- [X] T003 [P] Review `specs/001-constitution-rules/contracts/governance-artifact-contract.md` and `specs/001-constitution-rules/quickstart.md` to capture required outputs and validation steps

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared governance baseline and supporting metadata that every user story depends on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Replace placeholder content and add the Sync Impact Report in `.specify/memory/constitution.md`
- [X] T005 [P] Record the active feature path in `.specify/feature.json`
- [X] T006 [P] Update `AGENTS.md` with the `.NET 10`, ASP.NET Core, and modular monolith baseline for future phases
- [X] T007 Verify `.specify/memory/constitution.md`, `.specify/feature.json`, and `AGENTS.md` are internally consistent before story work begins

**Checkpoint**: Governance baseline and feature metadata are ready for story-specific implementation

---

## Phase 3: User Story 1 - Ratify Core Rules (Priority: P1) 🎯 MVP

**Goal**: Deliver the ratified constitution that governs all later project phases

**Independent Test**: Open `.specify/memory/constitution.md` and confirm it defines the mandatory principles, governance process, quality gates, semantic version, and ratification dates with no unresolved placeholders

### Implementation for User Story 1

- [X] T008 [US1] Draft the Core Principles section in `.specify/memory/constitution.md`
- [X] T009 [US1] Add Security & Governance Constraints and Delivery Workflow & Quality Gates in `.specify/memory/constitution.md`
- [X] T010 [US1] Finalize the Governance section, semantic version, ratified date, and last amended date in `.specify/memory/constitution.md`
- [X] T011 [US1] Validate `.specify/memory/constitution.md` against `specs/001-constitution-rules/spec.md` and `specs/001-constitution-rules/quickstart.md`, then correct any missing governance requirements

**Checkpoint**: The constitution is a complete, reviewable baseline for all later phases

---

## Phase 4: User Story 2 - Align Delivery Templates (Priority: P2)

**Goal**: Make the normal Spec Kit workflow enforce the constitution instead of bypassing it

**Independent Test**: Review the aligned templates and confirm they all require tenant boundaries, sensitive data handling, auditability, and proportionate validation before later features are planned or implemented

### Implementation for User Story 2

- [X] T012 [P] [US2] Align the technical context and constitution gate prompts in `.specify/templates/plan-template.md`
- [X] T013 [P] [US2] Align the security, governance, requirements, and assumptions prompts in `.specify/templates/spec-template.md`
- [X] T014 [P] [US2] Align the task-generation rules and mandatory validation expectations in `.specify/templates/tasks-template.md`
- [X] T015 [US2] Review `.specify/extensions/git/README.md`, `.specify/extensions/git/commands/speckit.git.initialize.md`, `.specify/extensions/git/commands/speckit.git.commit.md`, `.specify/extensions/git/commands/speckit.git.feature.md`, `.specify/extensions/git/commands/speckit.git.remote.md`, and `.specify/extensions/git/commands/speckit.git.validate.md` for governance conflicts and update `.specify/memory/constitution.md` Sync Impact Report if the review result changes

**Checkpoint**: New feature work cannot skip the constitution through outdated templates

---

## Phase 5: User Story 3 - Enforce Governance Reviews (Priority: P3)

**Goal**: Define the amendment workflow and validation package that makes governance changes reviewable

**Independent Test**: Review the contract, data model, quickstart, and AGENTS guidance and confirm they define amendment evidence, artifact relationships, and validation steps for later reviewers

### Implementation for User Story 3

- [X] T016 [US3] Document the governance artifact interface and amendment requirements in `specs/001-constitution-rules/contracts/governance-artifact-contract.md`
- [X] T017 [US3] Capture amendment, template-alignment, and sync-report entities in `specs/001-constitution-rules/data-model.md`
- [X] T018 [US3] Document the review and validation workflow in `specs/001-constitution-rules/quickstart.md`
- [X] T019 [US3] Update `AGENTS.md` so future planning work inherits the ratified platform and structure choices from `specs/001-constitution-rules/plan.md`

**Checkpoint**: Governance changes are explainable, reviewable, and reproducible

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency review across the constitution, templates, and planning artifacts

- [X] T020 [P] Reconcile the Sync Impact Report in `.specify/memory/constitution.md` with `.specify/templates/plan-template.md`, `.specify/templates/spec-template.md`, and `.specify/templates/tasks-template.md`
- [X] T021 [P] Run the validation checklist in `specs/001-constitution-rules/quickstart.md` against `.specify/memory/constitution.md`, `.specify/templates/plan-template.md`, `.specify/templates/spec-template.md`, `.specify/templates/tasks-template.md`, and `specs/001-constitution-rules/contracts/governance-artifact-contract.md`
- [X] T022 Perform a final review for placeholder removal, semantic version/date consistency, and artifact completeness across `.specify/memory/constitution.md` and `specs/001-constitution-rules/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; establishes scope and inputs
- **Foundational (Phase 2)**: Depends on Setup completion; blocks all story work
- **User Story 1 (Phase 3)**: Depends on Foundational completion; establishes the MVP governance artifact
- **User Story 2 (Phase 4)**: Depends on User Story 1 because template alignment must reflect the ratified constitution
- **User Story 3 (Phase 5)**: Depends on User Story 1 and should use the final template expectations from User Story 2
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2 and delivers the MVP
- **User Story 2 (P2)**: Starts after User Story 1 ratifies the governing rules
- **User Story 3 (P3)**: Starts after User Story 1 and can overlap late-stage review with User Story 2 once the governing text is stable

### Within Each User Story

- Ratify the governing document before updating dependent workflow templates
- Update dependent artifacts before running final validation
- Keep Sync Impact Report updates synchronized with actual template and guidance changes

### Parallel Opportunities

- T002 and T003 can run in parallel during setup
- T005 and T006 can run in parallel during foundational work
- T012, T013, and T014 can run in parallel because they edit different template files
- T020 and T021 can run in parallel during final validation

---

## Parallel Example: User Story 2

```text
Task: "Align the technical context and constitution gate prompts in .specify/templates/plan-template.md"
Task: "Align the security, governance, requirements, and assumptions prompts in .specify/templates/spec-template.md"
Task: "Align the task-generation rules and mandatory validation expectations in .specify/templates/tasks-template.md"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Confirm `.specify/memory/constitution.md` is ratified and reviewable

### Incremental Delivery

1. Ratify the constitution
2. Align the workflow templates
3. Add amendment/review artifacts
4. Run the quickstart validation pass

### Parallel Team Strategy

With multiple contributors:

1. One contributor finalizes `.specify/memory/constitution.md`
2. One contributor aligns the three template files in parallel after the constitution text stabilizes
3. One contributor prepares the governance contract, data model, quickstart, and AGENTS updates
4. Finish with a shared validation pass over all affected artifacts

---

## Notes

- All tasks follow the required checklist format with task ID, optional `[P]`, story label where required, and explicit file paths
- No automated test tasks are included because this feature is documentation and workflow alignment only
- User Story 1 is the recommended MVP scope
- Stop after any checkpoint if the affected artifact is fully reviewable on its own
