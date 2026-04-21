# Data Model: Phase 1 Core SaaS Foundation

## Entities

### UserAccount

- **Purpose**: Represents an authenticated person who can access one or more
  workspaces.
- **Fields**:
  - `id` (unique identifier, required)
  - `email` (string, required, unique)
  - `displayName` (string, required)
  - `status` (enum: `active`, `locked`, `disabled`)
  - `createdAt` (timestamp, required)
  - `lastSignInAt` (timestamp, optional)
- **Validation**:
  - `email` must be unique and valid.
  - `status` must block sign-in when set to `disabled`.

### Workspace

- **Purpose**: Defines the tenant boundary for all protected data and actions.
- **Fields**:
  - `id` (unique identifier, required)
  - `name` (string, required)
  - `slug` (string, required, unique)
  - `status` (enum: `active`, `suspended`)
  - `createdAt` (timestamp, required)
  - `createdByUserId` (foreign key to `UserAccount`, required)
- **Validation**:
  - `slug` must be unique.
  - `status` must prevent normal access when `suspended`.

### WorkspaceMembership

- **Purpose**: Associates a user with a workspace and captures access status.
- **Fields**:
  - `id` (unique identifier, required)
  - `workspaceId` (foreign key, required)
  - `userId` (foreign key, required)
  - `roleId` (foreign key to `RoleAssignment`, required)
  - `status` (enum: `active`, `inactive`, `removed`)
  - `joinedAt` (timestamp, required)
  - `lastUpdatedAt` (timestamp, required)
- **Validation**:
  - Only one active membership per user/workspace pair may exist at a time.
  - `removed` memberships cannot authorize access.

### RoleAssignment

- **Purpose**: Defines the permission level granted inside a workspace.
- **Fields**:
  - `id` (unique identifier, required)
  - `name` (enum: `owner`, `admin`, `member`)
  - `canManageMembers` (boolean, required)
  - `canManageWorkspace` (boolean, required)
  - `canReadWorkspaceContext` (boolean, required)
- **Validation**:
  - `owner` must retain the highest workspace-level privileges.
  - The system must ensure at least one owner remains per workspace.

### AuditRecord

- **Purpose**: Stores sensitive action traces for governance and support review.
- **Fields**:
  - `id` (unique identifier, required)
  - `workspaceId` (foreign key, optional for pre-workspace onboarding events)
  - `actorUserId` (foreign key, optional for anonymous registration start)
  - `actionType` (string, required)
  - `targetType` (string, required)
  - `targetId` (string, optional)
  - `result` (enum: `success`, `denied`, `failed`)
  - `reason` (string, required)
  - `occurredAt` (timestamp, required)
- **Validation**:
  - `reason` must be present for denied or failed sensitive actions.
  - `workspaceId` must be present when the action occurs within an established
    workspace context.

## Relationships

- `UserAccount` has many `WorkspaceMembership`.
- `Workspace` has many `WorkspaceMembership`.
- `WorkspaceMembership` belongs to one `UserAccount`, one `Workspace`, and one
  `RoleAssignment`.
- `Workspace` has many `AuditRecord`.
- `UserAccount` may be the actor for many `AuditRecord` entries.

## State Transitions

### WorkspaceMembership

- `inactive` -> `active`: membership is enabled for access
- `active` -> `inactive`: access is temporarily disabled
- `active` -> `removed`: membership is removed and access ends

### Workspace

- `active` -> `suspended`: access is blocked except for explicitly authorized
  administrative recovery
- `suspended` -> `active`: normal tenant access resumes

### UserAccount

- `active` -> `locked`: access is temporarily blocked after account security
  conditions
- `active` -> `disabled`: access is administratively revoked
