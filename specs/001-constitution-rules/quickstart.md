# Quickstart: Phase 0 Constitution and Project Rules

## Goal

Validate that Phase 0 has produced a usable governance baseline for the later
`.NET` implementation phases.

## Prerequisites

- Access to the repository working tree
- A Markdown viewer or editor
- Optional for later phases: `.NET 10` SDK installed locally

## Validate the Deliverables

1. Open `.specify/memory/constitution.md` and confirm the document has no
   placeholders, includes a sync impact report, and ends with a semantic version
   plus ratification dates.
2. Open `.specify/templates/plan-template.md`,
   `.specify/templates/spec-template.md`, and
   `.specify/templates/tasks-template.md` and verify they now require tenant
   boundaries, sensitive data handling, auditability, and testing.
3. Open `specs/001-constitution-rules/spec.md` and confirm the scope is limited
   to governance and workflow alignment rather than runtime feature delivery.
4. Open `specs/001-constitution-rules/plan.md` and `research.md` and confirm the
   future project baseline is `.NET 10`, ASP.NET Core, and a modular monolith.
5. Open `specs/001-constitution-rules/contracts/governance-artifact-contract.md`
   and verify the constitution, template alignment, and amendment workflow
   contracts are defined.

## Ready the Next Phase

1. Use `/speckit.tasks` for this feature if you want an implementation task list
   for the governance updates already planned.
2. Start Phase 1 on top of this baseline and carry forward:
   - `.NET 10` / ASP.NET Core 10
   - modular monolith structure
   - controller-based Web API defaults
   - tenant isolation, auditability, and security-first constraints

## Expected Outcome

After completing the checks above, the repository has a ratified governance
baseline that can be used to review every later feature plan before code is
written.
