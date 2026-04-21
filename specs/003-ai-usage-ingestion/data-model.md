# Data Model: Phase 2 AI Usage Event Ingestion

## Entities

### AIUsageEvent

- **Purpose**: Represents a single accepted AI-related action recorded for a
  workspace.
- **Fields**:
  - `id` (unique identifier, required)
  - `workspaceId` (foreign key to `Workspace`, required)
  - `actorUserId` (foreign key to `UserAccount`, required)
  - `eventType` (enum: `prompt_submitted`, `file_uploaded`, `tool_used`,
    `usage_recorded`, `model_called`)
  - `idempotencyKey` (string, required)
  - `toolName` (string, required)
  - `modelName` (string, optional)
  - `sourceLabel` (string, optional)
  - `occurredAt` (timestamp, required)
  - `receivedAt` (timestamp, required)
  - `promptPreview` (string, optional, bounded length)
  - `fileName` (string, optional)
  - `fileSizeBytes` (integer, optional)
  - `inputTokenCount` (integer, optional)
  - `outputTokenCount` (integer, optional)
  - `estimatedCost` (decimal, optional)
  - `details` (structured detail payload for event-type-specific attributes,
    optional)
- **Validation**:
  - `idempotencyKey` must be unique per workspace.
  - `eventType` must be one of the supported Phase 2 types.
  - `toolName` must be present for every accepted event.
  - `occurredAt` may be historical but must not be unreasonably far in the
    future relative to receipt time.
  - `promptPreview` and other optional text fields must obey explicit length
    limits.
  - Raw file content and arbitrary unbounded payload blobs are not allowed.

### EventIngestionRequest

- **Purpose**: Describes the validated inbound command to record an AI usage
  event before it becomes a persisted `AIUsageEvent`.
- **Fields**:
  - `workspaceId` (route-scoped identifier, required)
  - `authenticatedActorUserId` (derived from the signed-in user, required)
  - `idempotencyKey` (string, required)
  - `eventType` (enum, required)
  - `occurredAt` (timestamp, required)
  - `toolName` (string, required)
  - `modelName` (string, optional)
  - `sourceLabel` (string, optional)
  - `promptPreview` (string, optional)
  - `fileName` (string, optional)
  - `fileSizeBytes` (integer, optional)
  - `inputTokenCount` (integer, optional)
  - `outputTokenCount` (integer, optional)
  - `estimatedCost` (decimal, optional)
  - `details` (structured detail payload, optional)
- **Validation**:
  - The authenticated caller must have an active membership in the target
    workspace.
  - Unsupported event types or missing required fields cause rejection.
  - Type-specific optional fields must be semantically valid when present, such
    as non-negative file sizes, token counts, and costs.

### EventHistoryQuery

- **Purpose**: Represents an authorized request to retrieve workspace event
  history.
- **Fields**:
  - `workspaceId` (route-scoped identifier, required)
  - `requestedByUserId` (authenticated caller, required)
  - `eventType` (enum filter, optional)
  - `actorUserId` (user filter, optional)
  - `toolName` (string filter, optional)
  - `fromOccurredAt` (timestamp filter, optional)
  - `toOccurredAt` (timestamp filter, optional)
  - `pageNumber` (integer, required)
  - `pageSize` (integer, required)
- **Validation**:
  - Only workspace owners and admins may execute the query.
  - `pageNumber` must be at least 1.
  - `pageSize` must stay within a bounded server-controlled range.
  - If both date filters are present, `fromOccurredAt` must be earlier than or
    equal to `toOccurredAt`.

### IngestionAuditOutcome

- **Purpose**: Captures the sensitive audit result of an ingestion or history
  access attempt using the platform's existing audit record mechanism.
- **Fields**:
  - `workspaceId` (foreign key to `Workspace`, required for workspace-scoped
    operations)
  - `actorUserId` (foreign key to `UserAccount`, optional when the request is
    unauthenticated)
  - `actionType` (enum-like string, required; examples: `event_ingest`,
    `event_history_read`)
  - `targetType` (string, required; examples: `ai_usage_event`, `event_history`)
  - `targetId` (event identifier or idempotency key, optional)
  - `result` (enum-like string: `success`, `denied`, `failed`, `duplicate`)
  - `reason` (string, required)
  - `occurredAt` (timestamp, required)
- **Validation**:
  - `reason` must explain all denied, failed, or duplicate results.
  - Workspace context must be captured for all authorized workspace operations.

## Relationships

- `Workspace` has many `AIUsageEvent`.
- `UserAccount` may be the actor for many `AIUsageEvent`.
- `Workspace` has many `IngestionAuditOutcome` records through the existing
  audit store.
- `AIUsageEvent` is created from one validated `EventIngestionRequest`.
- `EventHistoryQuery` reads many `AIUsageEvent` records but does not mutate
  them.

## State Transitions

### EventIngestionRequest

- `submitted` -> `accepted`: request passes validation and creates a new
  `AIUsageEvent`
- `submitted` -> `duplicate`: request matches an existing workspace-scoped
  idempotency key and returns the prior accepted event
- `submitted` -> `rejected`: request fails validation, authorization, or tenant
  checks

### AIUsageEvent

- `recorded`: accepted and persisted as part of append-only workspace history

### EventHistoryQuery

- `requested` -> `authorized`: caller is owner or admin and receives paged
  results
- `requested` -> `denied`: caller lacks tenant access or role permission
