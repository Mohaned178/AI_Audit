# Quickstart: Phase 1 Core SaaS Foundation

## Goal

Validate that the SaaS foundation implementation delivers tenant onboarding,
authentication, workspace-scoped access, and membership management.

## Prerequisites

- `.NET 10` SDK installed
- PostgreSQL available for the application database
- Local configuration values for database connection and authentication secrets

## Setup

1. Run `dotnet build AIUsageGuard.slnx`.
2. Run `dotnet test AIUsageGuard.slnx`.
3. Start the API host with `dotnet run --project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj`.
4. Exercise the endpoints from `contracts/foundation-api.openapi.yaml`.

## Validation Scenario 1: New Customer Onboarding

1. Register a new account and create a workspace.
2. Confirm the new account becomes the workspace owner.
3. Confirm the API response identifies the created workspace context.

## Validation Scenario 2: Workspace Member Administration

1. Sign in as the workspace owner or admin.
2. Add a new member to the workspace and assign the `member` role.
3. Change that member to `admin`.
4. Confirm the membership change is visible only inside the same workspace.

## Validation Scenario 3: Tenant Isolation and Authorization

1. Sign in as a member.
2. Attempt to access the workspace context endpoint for a different workspace.
3. Attempt to manage memberships without owner/admin privileges.
4. Confirm both actions are denied and no protected data is returned.

## Validation Scenario 4: Ownership Safeguard

1. Attempt to remove or deactivate the last remaining owner in a workspace.
2. Confirm the system rejects the change.

## Expected Outcome

After these scenarios pass, the application behaves like a multi-tenant SaaS
foundation with authenticated workspace access and role-based membership
controls, emits audit records for the covered flows, and returns consistent
ProblemDetails responses for contract failures.
