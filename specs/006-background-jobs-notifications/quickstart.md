# Quickstart: Phase 5 Background Jobs and Notifications

## Goal

Verify that workspace owners and admins can configure notification preferences,
receive urgent governance alerts, review scheduled digest results, and inspect
delivery outcomes without breaking workspace isolation.

## Preconditions

1. The Phase 1 SaaS foundation is working with authenticated users and
   workspace membership.
2. The Phase 2 AI usage event ingestion endpoints are available and can store
   representative activity.
3. The Phase 3 risk detection endpoints are available and can create supported
   findings or threshold conditions that qualify for urgent notifications.
4. The Phase 4 reporting endpoints are available because digest generation
   reuses reporting-period summaries.
5. Background processing is enabled in the running environment.
6. An outbound notification sink suitable for non-production verification is
   available so delivered messages can be inspected safely.
7. The background worker interval is short enough for manual verification, or
   you have an operator-safe way to invoke one notification scan and delivery
   pass in a non-production environment.
8. You have:
   - a signed-in workspace owner or admin session for the target workspace
   - a separate signed-in regular member session in that workspace
   - a separate signed-in owner or admin session for a different workspace

## Scenario 1: Configure workspace notification preferences

1. As a workspace owner or admin, call:
   `GET /workspaces/{workspaceId}/notification-preferences`
2. Verify the request returns `200 OK` with the current workspace-scoped
   notification settings.
3. Update the settings with:
   `PUT /workspaces/{workspaceId}/notification-preferences`
4. Verify the request returns `200 OK`.
5. Verify the response reflects the selected urgent-alert setting, digest
   setting, digest cadence, and eligible recipients for that workspace only.

## Scenario 2: Trigger and review an urgent governance alert

1. Configure the workspace so urgent notifications are enabled.
2. Submit or identify a supported high-priority governance condition such as a
   high-severity finding or supported threshold breach in the target workspace.
3. Wait for the urgent alert scan window to complete in the non-production
   environment.
4. Verify at least one eligible workspace recipient receives a notification in
   the configured outbound sink, or a final skipped or failed outcome is
   recorded with a clear reason.
5. Call:
   `GET /workspaces/{workspaceId}/notifications?type=urgent_alert&pageNumber=1&pageSize=20`
6. Verify the request returns `200 OK` and includes the new workspace-scoped
   urgent notification history entry.
7. Open the notification detail:
   `GET /workspaces/{workspaceId}/notifications/{notificationId}`
8. Verify the detail identifies the workspace, notification type, severity,
   reason, and delivery outcomes without exposing raw prompts or raw file
   contents.

## Scenario 3: Review a scheduled digest

1. Configure the workspace so digest delivery is enabled.
2. Ensure the workspace has recent activity and flagged findings during the
   current digest cadence window.
3. Wait for the next completed digest window in the non-production environment.
4. Verify a digest notification is generated once for that completed period.
5. Call:
   `GET /workspaces/{workspaceId}/notifications?type=digest&pageNumber=1&pageSize=20`
6. Verify the request returns `200 OK`.
7. Verify the newest digest entry clearly identifies the covered period and
   summarizes recent activity, flagged activity, and estimated cost for the
   target workspace only.

## Scenario 4: Confirm retry and final-outcome behavior

1. In a non-production environment, cause one notification delivery attempt to
   fail temporarily.
2. Verify the failed attempt is retried automatically during a later retry
   scan.
3. Call the notification detail endpoint for the affected notification.
4. Verify the delivery history shows the retry progression and ends in a clear
   final outcome such as delivered, failed, or skipped.
5. Verify repeated scans do not create duplicate urgent notifications for the
   same unchanged issue.

## Scenario 5: Authorization boundaries hold

1. As the separate signed-in regular member session, call:
   `GET /workspaces/{workspaceId}/notification-preferences`
2. Verify access is denied with `403 Forbidden`.
3. As the separate signed-in owner or admin from another workspace, call:
   `GET /workspaces/{workspaceId}/notifications?pageNumber=1&pageSize=20`
4. Verify access is denied with `403 Forbidden`.
5. Verify successful and denied notification-management actions both produce
   audit evidence.
