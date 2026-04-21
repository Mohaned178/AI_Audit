# Governance Artifact Contract

## Purpose

Define the document-level interfaces produced by Phase 0 so future planning and
review steps can rely on stable governance artifacts.

## Contract 1: Constitution Artifact

- **Artifact Path**: `.specify/memory/constitution.md`
- **Required Sections**:
  - Core Principles
  - Security & Governance Constraints
  - Delivery Workflow & Quality Gates
  - Governance
- **Required Metadata**:
  - semantic version
  - ratified date
  - last amended date
  - prepended sync impact report comment
- **Invariants**:
  - no unresolved template placeholders
  - principles are normative and testable
  - governance defines amendment and compliance expectations

## Contract 2: Template Alignment

- **Artifact Paths**:
  - `.specify/templates/plan-template.md`
  - `.specify/templates/spec-template.md`
  - `.specify/templates/tasks-template.md`
- **Required Behaviors**:
  - prompt for tenant-boundary decisions
  - prompt for sensitive-data handling decisions
  - require auditability expectations
  - require proportionate testing expectations
- **Invariants**:
  - template guidance must not contradict the constitution
  - changes to governance-sensitive prompts must be updated in the same change
    set as the constitution when impacted

## Contract 3: Amendment Workflow

- **Trigger**: Any change to constitutional principles, governance rules, or
  required template behaviors
- **Required Outputs**:
  - updated constitution version
  - updated sync impact report
  - aligned dependent templates or explicit follow-up TODOs
- **Review Gate**:
  - the change is incomplete until versioning and dependency impact are visible
    in the same review

## Non-Goals

- This contract does not define runtime HTTP endpoints.
- This contract does not define tenant data schemas or event payloads.
- Those concerns begin in later phases and must conform to this governance
  contract.
