# Data Model: Phase 4 Reporting and Dashboard APIs

## Overview

Phase 4 does not add new persisted domain entities. It introduces read-only
reporting models derived from the existing `AIUsageEvent`, `RiskFinding`,
`WorkspaceMembership`, `UserAccount`, and `AuditRecord` data already stored by
earlier phases.

## Existing Source Entities Reused

### AIUsageEvent

- **Purpose in Phase 4**: Provides activity volume, actor attribution, tool
  usage, event timing, token counts, and estimated cost inputs for dashboard
  and grouped usage reporting.
- **Relevant fields**:
  - `WorkspaceId`
  - `ActorUserId`
  - `EventType`
  - `ToolName`
  - `EstimatedCost`
  - `OccurredAt`

### RiskFinding

- **Purpose in Phase 4**: Provides flagged-activity volume, severity
  distribution, tool attribution, actor attribution, and daily alert trends.
- **Relevant fields**:
  - `WorkspaceId`
  - `EventId`
  - `RuleType`
  - `Severity`
  - `ActorUserId`
  - `ToolName`
  - `DetectedAt`

### UserAccount and WorkspaceMembership

- **Purpose in Phase 4**: Resolve grouped usage rows to stable actor identity
  values and display names while remaining inside the current workspace's
  membership boundary.
- **Relevant fields**:
  - `UserAccount.Id`
  - `UserAccount.DisplayName`
  - `UserAccount.Email`
  - `WorkspaceMembership.WorkspaceId`
  - `WorkspaceMembership.UserId`
  - `WorkspaceMembership.Role`
  - `WorkspaceMembership.Status`

## Derived Reporting Models

### ReportingPeriod

- **Purpose**: Defines the normalized date window shared across all dashboard
  and report responses.
- **Fields**:
  - `FromDate` (date, inclusive)
  - `ToDate` (date, inclusive)
  - `NormalizedFromUtc` (datetimeoffset)
  - `NormalizedToUtc` (datetimeoffset)
  - `DayCount` (int)
- **Validation**:
  - `FromDate` and `ToDate` are required.
  - `FromDate` must be less than or equal to `ToDate`.
  - Requested period must not exceed the supported maximum window for this
    phase.

### DashboardSummary

- **Purpose**: Provides the top-level workspace overview for a selected
  reporting period.
- **Fields**:
  - `WorkspaceId` (guid)
  - `Period` (`ReportingPeriod`)
  - `TotalEvents` (int)
  - `UniqueActorCount` (int)
  - `UniqueToolCount` (int)
  - `FlaggedFindingCount` (int)
  - `TopUsers` (collection of `UsageSummaryRow`)
  - `TopTools` (collection of `UsageSummaryRow`)
  - `Alerts` (`AlertsSummary`)
  - `Costs` (`EstimatedCostSummary`)
- **Validation**:
  - All values must be computed only from records inside one workspace and one
    normalized reporting period.
  - `TopUsers` and `TopTools` are limited to a bounded ranked subset.

### UsageSummaryRow

- **Purpose**: Represents one grouped usage result for a user or tool.
- **Fields**:
  - `Dimension` (enum: `User`, `Tool`)
  - `ActorUserId` (guid, optional for tool rows)
  - `DisplayLabel` (string)
  - `TotalEvents` (int)
  - `FlaggedFindingCount` (int)
  - `EstimatedCost` (decimal)
  - `EventsWithEstimatedCost` (int)
  - `EventsMissingEstimatedCost` (int)
  - `LastActivityAt` (datetimeoffset, optional)
- **Validation**:
  - `DisplayLabel` must be non-empty.
  - `TotalEvents` must be zero or greater.
  - `EstimatedCost` must be zero or greater.

### AlertsSummary

- **Purpose**: Describes flagged activity for a workspace and reporting period.
- **Fields**:
  - `WorkspaceId` (guid)
  - `Period` (`ReportingPeriod`)
  - `TotalFindings` (int)
  - `HighSeverityCount` (int)
  - `MediumSeverityCount` (int)
  - `LowSeverityCount` (int)
  - `AffectedActorCount` (int)
  - `AffectedToolCount` (int)
  - `DailyTrend` (collection of `DailyTrendPoint`)
- **Validation**:
  - Severity totals must sum to `TotalFindings`.
  - Trend points must stay inside the selected reporting period.

### EstimatedCostSummary

- **Purpose**: Reports period cost totals together with completeness signals so
  the consumer can determine whether the total is partial.
- **Fields**:
  - `WorkspaceId` (guid)
  - `Period` (`ReportingPeriod`)
  - `EstimatedCostTotal` (decimal)
  - `EventsWithEstimatedCost` (int)
  - `EventsMissingEstimatedCost` (int)
  - `IsPartial` (bool)
  - `DailyTrend` (collection of `DailyTrendPoint`)
- **Validation**:
  - `IsPartial` is `true` when `EventsMissingEstimatedCost` is greater than
    zero.
  - `EstimatedCostTotal` must equal the sum of the included estimated cost
    values for the reporting period.

### DailyTrendPoint

- **Purpose**: Represents one day of grouped dashboard trend output.
- **Fields**:
  - `Date` (date)
  - `EventCount` (int)
  - `FindingCount` (int)
  - `EstimatedCost` (decimal)
- **Validation**:
  - `Date` must fall within the reporting period.
  - Counts and totals must be zero or greater.

## Relationships

- One `ReportingPeriod` is attached to every dashboard and report response.
- One `DashboardSummary` contains many `UsageSummaryRow` items plus one
  `AlertsSummary` and one `EstimatedCostSummary`.
- One `AlertsSummary` contains many `DailyTrendPoint` items.
- One `EstimatedCostSummary` contains many `DailyTrendPoint` items.
- `UsageSummaryRow` values are derived from many `AIUsageEvent` rows and, where
  applicable, related `RiskFinding` rows for the same workspace and period.

## State Transitions

Phase 4 reporting models are generated on demand and are not persisted, so they
do not introduce lifecycle state transitions of their own.

## Query and Index Considerations

- Reporting queries rely on `AIUsageEvent` filtering by `WorkspaceId` and
  `OccurredAt`, with additional grouping by `ActorUserId` and `ToolName`.
- Alerts queries rely on `RiskFinding` filtering by `WorkspaceId` and
  `DetectedAt`, with grouping by `Severity`, `ActorUserId`, and `ToolName`.
- If current indexes are insufficient, the feature may need query indexes on:
  - `AIUsageEvent (WorkspaceId, OccurredAt desc)`
  - `AIUsageEvent (WorkspaceId, ActorUserId, OccurredAt desc)`
  - `AIUsageEvent (WorkspaceId, ToolName, OccurredAt desc)`
  - `RiskFinding (WorkspaceId, DetectedAt desc)`
  - `RiskFinding (WorkspaceId, Severity, DetectedAt desc)`
- User display-name lookup must remain constrained to actors who are valid
  members of the requested workspace.

## Audit Touchpoints

- Each successful dashboard or report read creates an `AuditRecord`.
- Denied reporting access attempts create an `AuditRecord`.
- Invalid reporting period requests create an auditable failure outcome when the
  request reaches a protected reporting endpoint.
