# Data Model: Phase 7 Audit Logs and Hardening

## Overview

Phase 7 turns the existing audit backbone into a workspace-visible
investigation surface and adds persisted state for selected hardening
behaviors. The phase primarily extends existing `AuditRecord` and `UserAccount`
models rather than introducing a large set of new tables.

## Existing Source Entities Reused

### AuditRecord

- **Purpose in Phase 7**: Remains the authoritative record of protected
  actions, denied access, and system-initiated security outcomes.
- **Relevant existing fields**:
  - `Id`
  - `WorkspaceId`
  - `ActorUserId`
  - `ActionType`
  - `TargetType`
  - `TargetId`
  - `Result`
  - `Reason`
  - `OccurredAt`

### UserAccount

- **Purpose in Phase 7**: Supplies the actor identity for audit history and
  carries login-hardening state for temporary lockouts.
- **Relevant existing fields**:
  - `Id`
  - `Email`
  - `DisplayName`
  - `Status`
  - `CreatedAt`
  - `LastSignInAt`

### WorkspaceMembership

- **Purpose in Phase 7**: Confirms which operators may access workspace audit
  history and supports role-based authorization for audit investigation.
- **Relevant existing fields**:
  - `WorkspaceId`
  - `UserId`
  - `Role`
  - `Status`

## Extended Persisted Entities

### AuditRecord

- **Purpose**: Stores append-only evidence for protected actions and attempted
  actions, enriched for investigation and hardening traceability.
- **Additional Phase 7 fields**:
  - `Category` (string: audit grouping such as `authentication`,
    `authorization`, `governance`, `billing`, `notification`, `reporting`)
  - `IsSecurityRelevant` (bool)
  - `CorrelationId` (string, optional)
  - `ClientIpAddressHash` (string, optional)
  - `UserAgent` (string, optional, minimized)
- **Validation**:
  - `Category` must come from a supported set of audit categories.
  - `CorrelationId`, when present, must be stable for one request chain.
  - Raw secrets, full tokens, raw prompts, and full client addresses must not
    be stored in these fields.
  - Records remain append-only through normal workspace-facing workflows.

### UserAccount

- **Purpose**: Stores bounded authentication-hardening state tied to repeated
  invalid sign-in attempts.
- **Additional Phase 7 fields**:
  - `FailedSignInCount` (int)
  - `LastFailedSignInAt` (datetimeoffset, optional)
  - `LockedUntilUtc` (datetimeoffset, optional)
- **Validation**:
  - `FailedSignInCount` must be zero or greater.
  - `LockedUntilUtc`, when present, must be later than `LastFailedSignInAt`.
  - Accounts in temporary lockout remain distinct from permanently disabled
    accounts.

## Derived Operational Models

### AuditLogFilter

- **Purpose**: Represents the supported filter set for workspace audit-history
  retrieval.
- **Fields**:
  - `WorkspaceId` (guid)
  - `RequestedByUserId` (guid)
  - `FromOccurredAtUtc` (datetimeoffset, optional)
  - `ToOccurredAtUtc` (datetimeoffset, optional)
  - `ActionType` (string, optional)
  - `Result` (string, optional)
  - `ActorUserId` (guid, optional)
  - `PageNumber` (int)
  - `PageSize` (int)
- **Validation**:
  - `WorkspaceId` and `RequestedByUserId` are required.
  - `PageNumber` must be at least 1.
  - `PageSize` must stay within the supported maximum.
  - `ToOccurredAtUtc` cannot be earlier than `FromOccurredAtUtc`.

### AuditLogPage

- **Purpose**: Stable paged result returned for workspace audit-history list
  reads.
- **Fields**:
  - `WorkspaceId` (guid)
  - `Items` (collection of audit summaries)
  - `PageNumber` (int)
  - `PageSize` (int)
  - `TotalCount` (int)
- **Validation**:
  - Every returned item belongs to the requested workspace.
  - Ordering is stable and investigation-friendly, newest first by default.

### HardeningDecision

- **Purpose**: Read model describing why a protected request was allowed,
  denied, limited, or locked out.
- **Fields**:
  - `DecisionType` (enum-like string: `Allowed`, `Denied`, `LockedOut`,
    `RejectedForIntegrity`)
  - `RuleName` (string)
  - `Reason` (string)
  - `OccurredAtUtc` (datetimeoffset)
- **Validation**:
  - Every denied or limited decision must map to one corresponding audit
    record.
  - Reasons must be concise enough for operator understanding and safe enough
    for minimized failure responses.

## Relationships

- One `AuditRecord` may belong to one workspace and one actor user.
- One `UserAccount` may appear as the actor for many `AuditRecord` records.
- One `WorkspaceMembership` links a user to the workspace whose audit history
  they may review.
- One `UserAccount` may have one active temporary lockout window at a time.
- One `HardeningDecision` corresponds to one protected request outcome and one
  audit record when the outcome is auditable.

## State Transitions

### UserAccount Authentication Guard State

- `Normal` -> `FailedAttemptObserved`
- `FailedAttemptObserved` -> `TemporarilyLocked`
- `TemporarilyLocked` -> `Normal`

### AuditRecord Investigation Semantics

- `Success` -> terminal recorded outcome
- `Failed` -> terminal recorded outcome
- `Denied` -> terminal recorded outcome
- `Duplicate` -> terminal recorded outcome when replay-safe suppression applies

## Query and Index Considerations

- Audit-history paging relies on:
  - `AuditRecord (WorkspaceId, OccurredAt desc)`
- Common investigation filters rely on:
  - `AuditRecord (WorkspaceId, ActionType, OccurredAt desc)`
  - `AuditRecord (WorkspaceId, Result, OccurredAt desc)`
  - `AuditRecord (WorkspaceId, ActorUserId, OccurredAt desc)`
- Security investigations benefit from:
  - `AuditRecord (WorkspaceId, IsSecurityRelevant, OccurredAt desc)`
- Authentication hardening lookups rely on:
  - `UserAccount (Email)`
  - `UserAccount (LockedUntilUtc)`

## Audit Touchpoints

- Every successful audit-history read creates an `AuditRecord`.
- Every denied audit-history access attempt creates an `AuditRecord`.
- Every temporary lockout decision creates an `AuditRecord`.
- Every rejected protected request that is blocked by a supported hardening rule
  creates an `AuditRecord`.
- Existing protected feature actions continue to create audit records that feed
  the Phase 7 investigation surface.
