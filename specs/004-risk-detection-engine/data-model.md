# Data Model: Phase 3 Risk Detection Engine

## Overview

Phase 3 builds on the existing `AIUsageEvent` and `AuditRecord` models from
Phase 2. It adds workspace-scoped risk policy configuration, evaluation
tracking, and reviewable findings without changing the tenant boundary model.

## Entities

### WorkspaceRiskPolicy

- **Purpose**: Stores the simple workspace-managed inputs required by supported
  risk rules.
- **Fields**:
  - `Id` (guid)
  - `WorkspaceId` (guid, unique)
  - `ApprovedTools` (collection of normalized tool identifiers)
  - `PerEventEstimatedCostThreshold` (decimal, optional)
  - `DailyEstimatedCostThreshold` (decimal, optional)
  - `CreatedAt` (datetimeoffset)
  - `LastUpdatedAt` (datetimeoffset)
  - `LastUpdatedByUserId` (guid)
- **Validation**:
  - `WorkspaceId` must reference exactly one existing workspace.
  - `ApprovedTools` entries must be unique after normalization and bounded in
    count and length.
  - Threshold values, when present, must be non-negative.
- **Relationships**:
  - One `WorkspaceRiskPolicy` belongs to one `Workspace`.
  - One `WorkspaceRiskPolicy` can influence many `RiskEvaluationOutcome` rows.

### RiskEvaluationOutcome

- **Purpose**: Captures the durable result of evaluating one accepted AI usage
  event against the built-in rule catalog for a workspace.
- **Fields**:
  - `Id` (guid)
  - `WorkspaceId` (guid)
  - `EventId` (guid, unique)
  - `EvaluatedAt` (datetimeoffset)
  - `AppliedRuleVersion` (string)
  - `MatchedRuleCount` (int)
  - `EvaluationResult` (enum: `NoMatch`, `Matched`, `Skipped`)
  - `SkippedReason` (string, optional)
  - `EvidenceSummary` (string, optional)
- **Validation**:
  - `EventId` must reference an accepted AI usage event in the same workspace.
  - `MatchedRuleCount` must be zero or greater.
  - `SkippedReason` is required only when `EvaluationResult` is `Skipped`.
- **Relationships**:
  - One `RiskEvaluationOutcome` belongs to one `AIUsageEvent`.
  - One `RiskEvaluationOutcome` can have zero or many `RiskFinding` rows.

### RiskFinding

- **Purpose**: Represents a single matched rule for one event and exposes the
  operator-facing governance signal that admins review.
- **Fields**:
  - `Id` (guid)
  - `WorkspaceId` (guid)
  - `EventId` (guid)
  - `EvaluationOutcomeId` (guid)
  - `RuleType` (enum: `SensitiveDataPattern`, `FileUpload`, `UnapprovedTool`,
    `CostThresholdExceeded`)
  - `Severity` (enum: `Low`, `Medium`, `High`)
  - `Status` (enum: `Open`)
  - `Reason` (string)
  - `EvidencePreview` (string, optional)
  - `ActorUserId` (guid)
  - `ToolName` (string)
  - `DetectedAt` (datetimeoffset)
- **Validation**:
  - Combination of `WorkspaceId`, `EventId`, and `RuleType` must be unique to
    prevent duplicate findings for the same replayed event.
  - `Reason` is required and bounded for operator readability.
  - `EvidencePreview` must remain bounded and exclude raw file contents.
- **Relationships**:
  - Many `RiskFinding` rows can reference one `AIUsageEvent`.
  - Many `RiskFinding` rows can reference one `RiskEvaluationOutcome`.
  - Many `RiskFinding` rows belong to one `Workspace`.

## Supporting Enums

### RiskRuleType

- `SensitiveDataPattern`
- `FileUpload`
- `UnapprovedTool`
- `CostThresholdExceeded`

### RiskSeverity

- `Low`
- `Medium`
- `High`

### RiskFindingStatus

- `Open`

### RiskEvaluationResult

- `NoMatch`
- `Matched`
- `Skipped`

## Derived Rule Inputs

The following values are read from the existing `AIUsageEvent` model and do not
require new event-schema changes in Phase 3:

- `PromptPreview`
- `DetailsJson` key-value pairs
- `FileName`
- `FileSizeBytes`
- `ToolName`
- `EstimatedCost`
- `OccurredAt`
- `ActorUserId`
- `WorkspaceId`

## State Transitions

### RiskEvaluationOutcome

1. `Pending` is implicit while the event ingestion workflow is executing.
2. Persisted as `Matched` when one or more built-in rules create findings.
3. Persisted as `NoMatch` when evaluation completes with zero findings.
4. Persisted as `Skipped` when evaluation cannot run deterministically for a
   documented reason such as missing eligible fields.

### RiskFinding

1. Created directly in `Open` status when a rule matches.
2. Remains `Open` throughout Phase 3.
3. Later phases may extend the lifecycle with review actions, but no mutation
   state is introduced here.

## Indexing & Query Considerations

- Unique index on `WorkspaceRiskPolicy.WorkspaceId`
- Unique index on `RiskEvaluationOutcome.EventId`
- Unique index on `RiskFinding (WorkspaceId, EventId, RuleType)`
- Query indexes on `RiskFinding (WorkspaceId, DetectedAt desc)`
- Query indexes on `RiskFinding (WorkspaceId, Severity, DetectedAt desc)`
- Query indexes on `RiskFinding (WorkspaceId, RuleType, DetectedAt desc)`
- Query indexes on `RiskFinding (WorkspaceId, ActorUserId, DetectedAt desc)`

## Audit Touchpoints

- Updating `WorkspaceRiskPolicy` creates an `AuditRecord`.
- Creating one or more `RiskFinding` rows creates `AuditRecord` entries or a
  summarized finding-creation audit outcome tied to the triggering event.
- Denied review and policy actions create `AuditRecord` entries.
