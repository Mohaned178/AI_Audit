# Data Model: Phase 0 Constitution and Project Rules

## Overview

Phase 0 has no runtime tenant data model. Its primary data structures are
governance artifacts stored as Markdown and JSON files in the repository.

## Entities

### ConstitutionDocument

- **Purpose**: Represents the authoritative governance document for AI Usage
  Guard.
- **Fields**:
  - `projectName` (string, required)
  - `version` (semantic version string, required)
  - `ratifiedDate` (date, required)
  - `lastAmendedDate` (date, required)
  - `principles` (collection of `ConstitutionPrinciple`, required)
  - `governanceRules` (collection of `GovernanceRule`, required)
  - `syncImpactReport` (single `SyncImpactReport`, required)
- **Validation**:
  - `version` must follow semantic versioning.
  - `ratifiedDate` and `lastAmendedDate` must use `YYYY-MM-DD`.
  - No unresolved placeholder tokens may remain after ratification.

### ConstitutionPrinciple

- **Purpose**: Captures a non-negotiable rule applied to later project phases.
- **Fields**:
  - `id` (string, required)
  - `title` (string, required)
  - `mandate` (text, required)
  - `rationale` (text, required)
  - `appliesTo` (list of lifecycle stages, required)
- **Validation**:
  - `title` must be unique within the constitution.
  - `mandate` must be testable and use normative language.
  - `rationale` must explain why the rule exists.

### GovernanceRule

- **Purpose**: Defines amendment, compliance, review, and exception procedures.
- **Fields**:
  - `id` (string, required)
  - `category` (enum: `amendment`, `versioning`, `compliance`, `exception`)
  - `statement` (text, required)
  - `reviewStage` (enum: `plan`, `tasks`, `code-review`, `all`)
  - `evidenceRequired` (text, optional)
- **Validation**:
  - `category` must be one of the supported governance categories.
  - `reviewStage` must map to at least one project checkpoint.

### TemplateAlignment

- **Purpose**: Tracks how a workflow template reflects constitutional rules.
- **Fields**:
  - `templatePath` (path, required)
  - `requiredPrompts` (list of strings, required)
  - `alignmentStatus` (enum: `updated`, `reviewed`, `pending`)
  - `lastValidatedDate` (date, required)
- **Validation**:
  - `templatePath` must refer to an existing repository artifact.
  - `alignmentStatus` cannot be `pending` when the constitution change is
    marked complete without a corresponding follow-up item.

### SyncImpactReport

- **Purpose**: Records how a constitutional change affected related artifacts.
- **Fields**:
  - `versionChange` (string, required)
  - `modifiedPrinciples` (list of strings, required)
  - `addedSections` (list of strings, required)
  - `removedSections` (list of strings, required)
  - `templateUpdates` (collection of `TemplateAlignment`, required)
  - `followUpTodos` (list of strings, required)
- **Validation**:
  - `versionChange` must match the version shown in the constitution footer.
  - `templateUpdates` must include the core plan, spec, and tasks templates.

## Relationships

- `ConstitutionDocument` has many `ConstitutionPrinciple`.
- `ConstitutionDocument` has many `GovernanceRule`.
- `ConstitutionDocument` has one `SyncImpactReport`.
- `SyncImpactReport` references many `TemplateAlignment` records.

## State Transitions

### ConstitutionDocument

- `Draft` -> `Ratified`: initial placeholders are replaced and governance rules
  are approved.
- `Ratified` -> `Amended`: a later governance change updates principles,
  sections, or dependent templates.
- `Amended` -> `Ratified`: the updated document and sync impact changes are
  accepted as the new authoritative baseline.

### TemplateAlignment

- `pending` -> `reviewed`: the template has been inspected for compatibility.
- `reviewed` -> `updated`: the template has been changed to match the
  constitution.
- `reviewed` -> `pending`: a later constitutional change reopens the alignment
  work.
