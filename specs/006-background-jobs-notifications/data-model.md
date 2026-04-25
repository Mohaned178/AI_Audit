# Data Model: Phase 5 Background Jobs and Notifications

## Overview

Phase 5 adds persisted operational models for notification preferences,
background job runs, notification records, and delivery outcomes. These models
reuse the existing `AIUsageEvent`, `RiskFinding`, `WorkspaceMembership`,
`UserAccount`, `Workspace`, and `AuditRecord` data introduced by earlier phases
to decide what should be notified and who may receive or review it.

## Existing Source Entities Reused

### AIUsageEvent

- **Purpose in Phase 5**: Provides the activity data that can contribute to
  urgent governance alerts and digest summaries.
- **Relevant fields**:
  - `WorkspaceId`
  - `ActorUserId`
  - `ToolName`
  - `EstimatedCost`
  - `OccurredAt`

### RiskFinding

- **Purpose in Phase 5**: Provides the high-priority governance signals that
  can trigger urgent notifications and enrich digest summaries.
- **Relevant fields**:
  - `WorkspaceId`
  - `EventId`
  - `RuleType`
  - `Severity`
  - `DetectedAt`

### WorkspaceMembership and UserAccount

- **Purpose in Phase 5**: Determine which current workspace owners and admins
  are eligible recipients and which operators may manage notification settings
  or review outcomes.
- **Relevant fields**:
  - `WorkspaceMembership.WorkspaceId`
  - `WorkspaceMembership.UserId`
  - `WorkspaceMembership.Role`
  - `WorkspaceMembership.Status`
  - `UserAccount.Id`
  - `UserAccount.DisplayName`
  - `UserAccount.Email`

## New Persisted Entities

### NotificationPreference

- **Purpose**: Stores the workspace-scoped notification settings that control
  urgent alerts, digest delivery, cadence, and eligible recipients.
- **Fields**:
  - `WorkspaceId` (guid)
  - `UrgentAlertsEnabled` (bool)
  - `DigestEnabled` (bool)
  - `DigestCadence` (enum: `Daily`, `Weekly`)
  - `RecipientSelectionMode` (enum: `AllAdminsAndOwners`, `SelectedRecipients`)
  - `SelectedRecipientUserIds` (collection of guid)
  - `CreatedAt` (datetimeoffset)
  - `LastUpdatedAt` (datetimeoffset)
  - `LastUpdatedByUserId` (guid)
- **Validation**:
  - There is at most one notification preference record per workspace.
  - `SelectedRecipientUserIds` may contain only active workspace owners or
    admins.
  - Digest cadence is required when digest delivery is enabled.

### BackgroundJobRun

- **Purpose**: Tracks one execution of a scheduled or recurring background job
  so operators and support tooling can understand what ran and what happened.
- **Fields**:
  - `Id` (guid)
  - `JobType` (enum: `UrgentAlertScan`, `DigestGeneration`, `DeliveryRetry`)
  - `WorkspaceId` (guid, optional for global due-work scans)
  - `ScheduledForUtc` (datetimeoffset)
  - `StartedAtUtc` (datetimeoffset)
  - `CompletedAtUtc` (datetimeoffset, optional)
  - `Status` (enum: `Running`, `Completed`, `Failed`, `Skipped`)
  - `ProcessedItemCount` (int)
  - `FailureSummary` (string, optional)
- **Validation**:
  - `ScheduledForUtc` must be present for every job run.
  - `CompletedAtUtc` is required when the run reaches a final status.
  - `ProcessedItemCount` must be zero or greater.

### Notification

- **Purpose**: Represents one workspace-scoped urgent alert or digest message
  prepared for delivery.
- **Fields**:
  - `Id` (guid)
  - `WorkspaceId` (guid)
  - `NotificationType` (enum: `UrgentAlert`, `Digest`)
  - `Channel` (enum: `Email`)
  - `TriggerFingerprint` (string)
  - `Severity` (enum: `Low`, `Medium`, `High`, optional)
  - `Subject` (string)
  - `SummaryBody` (string)
  - `CoveredPeriodStartUtc` (datetimeoffset, optional)
  - `CoveredPeriodEndUtc` (datetimeoffset, optional)
  - `CreatedAtUtc` (datetimeoffset)
  - `CreatedByJobRunId` (guid)
  - `Status` (enum: `Pending`, `Delivered`, `PartiallyDelivered`, `Failed`, `Skipped`)
- **Validation**:
  - `TriggerFingerprint` must be unique per workspace for an unchanged
    notification source while the notification remains active.
  - Digest notifications require a covered period.
  - Notification content must exclude raw prompt or file payloads.

### NotificationDeliveryOutcome

- **Purpose**: Tracks delivery state for one notification-recipient pair.
- **Fields**:
  - `Id` (guid)
  - `NotificationId` (guid)
  - `RecipientUserId` (guid)
  - `RecipientAddress` (string)
  - `DeliveryStatus` (enum: `Pending`, `RetryScheduled`, `Delivered`, `Failed`, `Skipped`)
  - `AttemptCount` (int)
  - `LastAttemptedAtUtc` (datetimeoffset, optional)
  - `NextAttemptAtUtc` (datetimeoffset, optional)
  - `FinalReason` (string, optional)
- **Validation**:
  - Each notification-recipient pair appears at most once.
  - `AttemptCount` must be zero or greater.
  - `NextAttemptAtUtc` is required while delivery is scheduled for retry.
  - A final outcome requires either `Delivered`, `Failed`, or `Skipped`.

## Derived Operational Models

### DigestSummary

- **Purpose**: Captures the completed period totals that a digest notification
  presents to operators.
- **Fields**:
  - `WorkspaceId` (guid)
  - `PeriodStartUtc` (datetimeoffset)
  - `PeriodEndUtc` (datetimeoffset)
  - `TotalEvents` (int)
  - `FlaggedFindingCount` (int)
  - `EstimatedCostTotal` (decimal)
  - `IsPartialCost` (bool)
  - `TopUsers` (bounded collection)
  - `TopTools` (bounded collection)
- **Validation**:
  - The covered period must represent one completed digest window.
  - Totals must be derived only from data inside the selected workspace and
    covered period.

### UrgentAlertSnapshot

- **Purpose**: Describes the minimized workspace-scoped details included in an
  urgent notification.
- **Fields**:
  - `WorkspaceId` (guid)
  - `SourceType` (enum: `RiskFinding`, `ThresholdState`)
  - `SourceId` (guid or stable string)
  - `Severity` (enum: `Low`, `Medium`, `High`)
  - `Reason` (string)
  - `ActorDisplayLabel` (string, optional)
  - `ToolLabel` (string, optional)
  - `ObservedAtUtc` (datetimeoffset)
- **Validation**:
  - Snapshot content must stay within the sensitivity limits defined for Phase
    5 and avoid unnecessary evidence detail.

## Relationships

- One `NotificationPreference` belongs to one workspace.
- One `BackgroundJobRun` may create many `Notification` records.
- One `Notification` belongs to one workspace and has many
  `NotificationDeliveryOutcome` records.
- One `NotificationDeliveryOutcome` belongs to exactly one `Notification` and
  one recipient user.
- One digest notification references one completed digest period.
- One urgent notification references one triggering fingerprint and may be
  derived from one or more underlying workspace conditions.

## State Transitions

### BackgroundJobRun

- `Running` -> `Completed`
- `Running` -> `Failed`
- `Running` -> `Skipped`

### Notification

- `Pending` -> `Delivered`
- `Pending` -> `PartiallyDelivered`
- `Pending` -> `Failed`
- `Pending` -> `Skipped`
- `PartiallyDelivered` -> `Delivered`
- `PartiallyDelivered` -> `Failed`

### NotificationDeliveryOutcome

- `Pending` -> `Delivered`
- `Pending` -> `RetryScheduled`
- `Pending` -> `Skipped`
- `RetryScheduled` -> `Delivered`
- `RetryScheduled` -> `Failed`
- `RetryScheduled` -> `Skipped`

## Query and Index Considerations

- Notification preference lookup relies on:
  - `NotificationPreference (WorkspaceId)`
- Due-work scanning relies on:
  - `BackgroundJobRun (JobType, ScheduledForUtc desc)`
  - `NotificationDeliveryOutcome (DeliveryStatus, NextAttemptAtUtc asc)`
- Notification history lookup relies on:
  - `Notification (WorkspaceId, CreatedAtUtc desc)`
  - `Notification (WorkspaceId, NotificationType, CreatedAtUtc desc)`
- Delivery-outcome review relies on:
  - `NotificationDeliveryOutcome (NotificationId, DeliveryStatus)`
- Recipient validation must remain constrained to active owners and admins of
  the requested workspace.

## Audit Touchpoints

- Each successful or denied notification preference read or update creates an
  `AuditRecord`.
- Each background job run creates an auditable outcome with type, workspace
  scope when applicable, and result.
- Notification generation, skipped delivery, delivery retries, exhausted
  retries, and final delivery outcomes create auditable records.
