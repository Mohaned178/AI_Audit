# Quickstart: Phase 2 AI Usage Event Ingestion

## Goal

Validate that the application can accept workspace-scoped AI usage events,
protect against duplicate submissions, and return filterable event history to
authorized administrators without leaking data across workspaces.

## Prerequisites

- `.NET 10` SDK installed
- PostgreSQL available for the application database
- Local configuration values for database connection and authentication secrets

## Setup

1. Run `dotnet build AIUsageGuard.slnx`.
2. Run `dotnet test AIUsageGuard.slnx`.
3. Start the API host with `dotnet run --project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj`.
4. Exercise the endpoints from `contracts/ai-usage-events.openapi.yaml`.

## Validation Scenario 1: Record a Valid AI Usage Event

1. Register or sign in as a workspace member.
2. Submit a `prompt_submitted` event to `POST /workspaces/{workspaceId}/events`
   with a new `idempotencyKey`.
3. Confirm the response reports an `accepted` outcome and returns the stored
   event metadata.

## Validation Scenario 2: Retrieve Filtered Event History

1. Sign in as a workspace owner or admin.
2. Request `GET /workspaces/{workspaceId}/events` with a date range or
   `eventType` filter.
3. Confirm the response returns only that workspace's matching events and
   includes page metadata.

## Validation Scenario 3: Prevent Duplicate or Replay Inflation

1. Submit an event with a specific `idempotencyKey`.
2. Submit the same request again with the same `idempotencyKey`.
3. Confirm the second response reports a `duplicate` outcome and does not create
   a second event record.

## Validation Scenario 4: Enforce Tenant and Role Boundaries

1. Sign in as a workspace member and try to read event history.
2. Confirm the history request is denied.
3. Sign in as a user from a different workspace and try to ingest or read using
   another workspace ID.
4. Confirm both requests are denied and no cross-workspace data is returned.

## Validation Scenario 5: Reject Invalid Event Payloads

1. Submit an event with an unsupported `eventType` or missing required fields.
2. Confirm the API returns a ProblemDetails failure and no event is stored.

## Expected Outcome

After these scenarios pass, the application can record trustworthy workspace AI
usage events, present basic filterable history to authorized admins, produce
auditable outcomes for accepted and rejected flows, and maintain the tenant
isolation guarantees established in Phase 1.
