# Data Model: Phase 6 Usage Limits and Billing-Ready Design

## Overview

Phase 6 adds persisted commercial and accounting models for plan definitions,
workspace plan assignments, monthly usage cycles, per-dimension cycle metrics,
limit events, and post-cycle adjustments. These models reuse existing
workspace, membership, AI usage, background-processing, and audit data from
earlier phases so the platform can enforce supported limits and present
billing-ready history without introducing payment-provider integration.

## Existing Source Entities Reused

### AIUsageEvent

- **Purpose in Phase 6**: Supplies accepted AI usage activity and estimated
  cost totals for monthly activity-volume and estimated-spend dimensions.
- **Relevant fields**:
  - `WorkspaceId`
  - `OccurredAt`
  - `ReceivedAt`
  - `EstimatedCost`
  - `IdempotencyKey`

### WorkspaceMembership

- **Purpose in Phase 6**: Supplies the active-member count used for the
  workspace-member quota dimension and identifies which operators may view
  billing data.
- **Relevant fields**:
  - `WorkspaceId`
  - `UserId`
  - `Role`
  - `Status`
  - `LastUpdatedAt`

### BackgroundJobRun

- **Purpose in Phase 6**: Provides durable execution history for cycle
  rollovers and reconciliation passes that adjust closed cycles after late or
  corrected activity arrives.
- **Relevant fields**:
  - `Id`
  - `JobType`
  - `ScheduledForUtc`
  - `StartedAtUtc`
  - `CompletedAtUtc`
  - `Status`

## New Persisted Entities

### PlanDefinition

- **Purpose**: Represents one named commercial plan available to be assigned to
  workspaces.
- **Fields**:
  - `Id` (guid)
  - `PlanCode` (string)
  - `DisplayName` (string)
  - `IsDefault` (bool)
  - `IsActive` (bool)
  - `CreatedAtUtc` (datetimeoffset)
  - `RetiredAtUtc` (datetimeoffset, optional)
- **Validation**:
  - `PlanCode` must be unique among active plans.
  - Only one active default plan may exist at a time.
  - Retired plans cannot be assigned to new workspaces after retirement.

### PlanLimitRule

- **Purpose**: Defines one supported quota dimension and response strategy
  inside a plan.
- **Fields**:
  - `Id` (guid)
  - `PlanDefinitionId` (guid)
  - `Dimension` (enum: `ActiveMembers`, `AIActivityEvents`, `EstimatedCost`)
  - `IncludedQuantity` (decimal)
  - `WarningThresholdQuantity` (decimal, optional)
  - `HardLimitQuantity` (decimal, optional)
  - `LimitBehavior` (enum: `WarnOnly`, `Restrict`, `AllowOverage`)
  - `CreatedAtUtc` (datetimeoffset)
- **Validation**:
  - A plan may define at most one rule per supported dimension.
  - `IncludedQuantity` must be zero or greater.
  - `WarningThresholdQuantity`, when present, must be greater than zero and not
    greater than the included quantity.
  - `HardLimitQuantity` is required when `LimitBehavior` is `Restrict`.
  - `HardLimitQuantity`, when present, must be greater than or equal to the
    included quantity and any warning threshold.

### WorkspacePlanAssignment

- **Purpose**: Records which plan applies to a workspace for a specific
  effective period.
- **Fields**:
  - `Id` (guid)
  - `WorkspaceId` (guid)
  - `PlanDefinitionId` (guid)
  - `EffectiveFromCycleStartUtc` (datetimeoffset)
  - `EffectiveToCycleStartUtc` (datetimeoffset, optional)
  - `AssignedByUserId` (guid, optional)
  - `ChangeReason` (string, optional)
  - `CreatedAtUtc` (datetimeoffset)
- **Validation**:
  - Plan assignments for one workspace may not overlap.
  - A workspace must always resolve to exactly one active assignment for any
    cycle start.
  - Assignments become effective only at cycle boundaries.
  - At most one future-dated assignment may be pending for a workspace.

### UsageCycle

- **Purpose**: Captures one workspace's monthly accounting period and the plan
  snapshot applied to it.
- **Fields**:
  - `Id` (guid)
  - `WorkspaceId` (guid)
  - `CycleStartUtc` (datetimeoffset)
  - `CycleEndExclusiveUtc` (datetimeoffset)
  - `PlanAssignmentId` (guid)
  - `Status` (enum: `Open`, `Closed`, `Adjusted`)
  - `OpenedAtUtc` (datetimeoffset)
  - `ClosedAtUtc` (datetimeoffset, optional)
  - `LastCalculatedAtUtc` (datetimeoffset)
  - `AdjustmentCount` (int)
- **Validation**:
  - Only one usage cycle may exist per workspace and UTC month.
  - Cycle dates must align to contiguous UTC month boundaries.
  - A cycle cannot close without an effective plan assignment.
  - `AdjustmentCount` must be zero or greater.

### UsageCycleMetric

- **Purpose**: Stores the per-dimension totals and state for one usage cycle.
- **Fields**:
  - `Id` (guid)
  - `UsageCycleId` (guid)
  - `Dimension` (enum: `ActiveMembers`, `AIActivityEvents`, `EstimatedCost`)
  - `IncludedQuantity` (decimal)
  - `WarningThresholdQuantity` (decimal, optional)
  - `HardLimitQuantity` (decimal, optional)
  - `CurrentQuantity` (decimal)
  - `OverageQuantity` (decimal)
  - `LimitBehavior` (enum: `WarnOnly`, `Restrict`, `AllowOverage`)
  - `State` (enum: `WithinLimit`, `Warning`, `Overage`, `Restricted`)
  - `LastTransitionAtUtc` (datetimeoffset, optional)
- **Validation**:
  - Each cycle has at most one metric row per supported dimension.
  - `CurrentQuantity` and `OverageQuantity` must be zero or greater.
  - `OverageQuantity` may be greater than zero only when `LimitBehavior` is
    `AllowOverage`.
  - `State` must match the rule thresholds and current quantity.

### LimitEvent

- **Purpose**: Records a meaningful warning, overage, or restriction state
  transition for one workspace, cycle, and dimension.
- **Fields**:
  - `Id` (guid)
  - `WorkspaceId` (guid)
  - `UsageCycleId` (guid)
  - `Dimension` (enum: `ActiveMembers`, `AIActivityEvents`, `EstimatedCost`)
  - `EventType` (enum: `WarningRaised`, `OverageStarted`, `RestrictionApplied`, `StateReturnedToWithinLimit`)
  - `TriggeredBySourceType` (enum: `AIUsageEvent`, `MembershipChange`, `PlanAssignment`, `Reconciliation`)
  - `TriggeredBySourceId` (string)
  - `CurrentQuantity` (decimal)
  - `ThresholdQuantity` (decimal, optional)
  - `Reason` (string)
  - `OccurredAtUtc` (datetimeoffset)
- **Validation**:
  - Duplicate unchanged state transitions for the same workspace, cycle, and
    dimension are not allowed.
  - `Reason` must explain which dimension and threshold change occurred.
  - `CurrentQuantity` must be zero or greater.

### CycleAdjustment

- **Purpose**: Preserves how a previously summarized cycle changed after late or
  corrected source activity was applied.
- **Fields**:
  - `Id` (guid)
  - `UsageCycleId` (guid)
  - `Dimension` (enum: `ActiveMembers`, `AIActivityEvents`, `EstimatedCost`)
  - `AdjustmentType` (enum: `LateActivity`, `SourceCorrection`, `ReconciliationRepair`)
  - `DeltaQuantity` (decimal)
  - `Reason` (string)
  - `SourceReference` (string)
  - `RecordedAtUtc` (datetimeoffset)
  - `AppliedByJobRunId` (guid, optional)
- **Validation**:
  - `DeltaQuantity` must not be zero.
  - Adjustments to closed cycles must always create a traceable reason and
    source reference.
  - `AppliedByJobRunId` is required when the change comes from background
    reconciliation.

## Derived Operational Models

### PlanStatusSnapshot

- **Purpose**: The workspace-scoped read model returned to owners and admins
  for current plan status.
- **Fields**:
  - `WorkspaceId` (guid)
  - `PlanCode` (string)
  - `PlanDisplayName` (string)
  - `CurrentCycleId` (guid)
  - `CycleStartUtc` (datetimeoffset)
  - `CycleEndExclusiveUtc` (datetimeoffset)
  - `NextPlanCode` (string, optional)
  - `MetricStatuses` (collection of dimension summaries)
- **Validation**:
  - The snapshot must be built only from the workspace's active assignment and
    current open cycle.
  - Metrics must match the persisted cycle values for that workspace.

### BillingCycleSummary

- **Purpose**: The stable summary view of a current or past usage cycle.
- **Fields**:
  - `UsageCycleId` (guid)
  - `WorkspaceId` (guid)
  - `PlanCode` (string)
  - `CycleStartUtc` (datetimeoffset)
  - `CycleEndExclusiveUtc` (datetimeoffset)
  - `Status` (enum: `Open`, `Closed`, `Adjusted`)
  - `Metrics` (collection of per-dimension summaries)
  - `WarningEventCount` (int)
  - `RestrictionEventCount` (int)
  - `AdjustmentCount` (int)
- **Validation**:
  - The summary must identify the plan and cycle dates that were in effect for
    that workspace only.
  - Counts must match the associated metric, event, and adjustment records.

## Relationships

- One `PlanDefinition` has many `PlanLimitRule` records.
- One `WorkspacePlanAssignment` belongs to one workspace and one plan.
- One `UsageCycle` belongs to one workspace and one plan assignment.
- One `UsageCycle` has many `UsageCycleMetric` records.
- One `UsageCycle` has many `LimitEvent` records.
- One `UsageCycle` has many `CycleAdjustment` records.
- One `CycleAdjustment` may reference one `BackgroundJobRun` when created by
  reconciliation.

## State Transitions

### UsageCycle

- `Open` -> `Closed`
- `Closed` -> `Adjusted`
- `Open` -> `Adjusted`

### UsageCycleMetric

- `WithinLimit` -> `Warning`
- `Warning` -> `Overage`
- `Warning` -> `Restricted`
- `WithinLimit` -> `Restricted`
- `Overage` -> `WithinLimit`
- `Restricted` -> `WithinLimit`

## Query and Index Considerations

- Active plan lookup relies on:
  - `WorkspacePlanAssignment (WorkspaceId, EffectiveFromCycleStartUtc desc)`
- Current-cycle lookup relies on:
  - `UsageCycle (WorkspaceId, CycleStartUtc desc, Status)`
- Current metric and plan-status reads rely on:
  - `UsageCycleMetric (UsageCycleId, Dimension)`
- Cycle-history paging relies on:
  - `UsageCycle (WorkspaceId, CycleStartUtc desc)`
- Limit-event deduplication and review rely on:
  - `LimitEvent (WorkspaceId, UsageCycleId, Dimension, EventType, OccurredAtUtc desc)`
- Adjustment review relies on:
  - `CycleAdjustment (UsageCycleId, RecordedAtUtc desc)`

## Audit Touchpoints

- Each successful or denied plan-status read creates an `AuditRecord`.
- Each successful or denied billing-cycle history read creates an `AuditRecord`.
- Each workspace plan assignment change creates an `AuditRecord`.
- Each warning, overage, restriction, or return-to-within-limit transition
  creates an auditable `LimitEvent`.
- Each closed-cycle adjustment creates both a `CycleAdjustment` record and an
  `AuditRecord`.
