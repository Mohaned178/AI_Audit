# Quickstart: Phase 7 Audit Logs and Hardening

## Goal

Verify that workspace owners and admins can investigate workspace audit
history, that unauthorized audit-history access remains blocked, and that
selected hardened protected workflows create trustworthy evidence when they
reject suspicious or invalid requests.

## Preconditions

1. The Phase 1 SaaS foundation is working with authenticated users and
   workspace membership.
2. Earlier phases already emit audit records for protected reads, writes,
   denied access, notifications, reporting, risk detection, and billing flows.
3. The target workspace has at least one owner or admin account and enough
   activity to produce audit evidence.
4. You have:
   - a signed-in workspace owner or admin session for the target workspace
   - a separate signed-in regular member session in that workspace
   - a separate signed-in owner or admin session for another workspace
   - one test account available for repeated invalid sign-in attempts

## Scenario 1: Review recent workspace audit history

1. As a workspace owner or admin, call:
   `GET /workspaces/{workspaceId}/audit-logs?pageNumber=1&pageSize=20`
2. Verify the request returns `200 OK`.
3. Verify each entry identifies the action type, outcome, target, time, and
   actor context when available for the requested workspace only.
4. Verify the newest entries appear first and the result set does not include
   another workspace's audit history.

## Scenario 2: Investigate denied or security-relevant activity

1. Trigger at least one denied protected action in the target workspace, such
   as a cross-workspace read attempt or member-level access to an admin-only
   endpoint.
2. As the authorized workspace owner or admin, call:
   `GET /workspaces/{workspaceId}/audit-logs?result=denied&pageNumber=1&pageSize=20`
3. Verify the denied action appears with the actor context when available, the
   denied result, the relevant target, and a concise reason.
4. Open one entry:
   `GET /workspaces/{workspaceId}/audit-logs/{auditLogId}`
5. Verify the detail remains workspace-scoped and includes investigation-safe
   context without exposing secrets or raw sensitive payloads.

## Scenario 3: Authorization boundaries hold for audit history

1. As the signed-in regular member session, call:
   `GET /workspaces/{workspaceId}/audit-logs?pageNumber=1&pageSize=20`
2. Verify access is denied with `403 Forbidden`.
3. As the separate owner or admin from another workspace, call:
   `GET /workspaces/{workspaceId}/audit-logs?pageNumber=1&pageSize=20`
4. Verify access is denied with `403 Forbidden`.
5. Verify both denied requests produce audit evidence without leaking the
   target workspace's audit entries.

## Scenario 4: Repeated invalid sign-in attempts trigger hardening

1. Using the test account, submit repeated invalid sign-in attempts until the
   supported temporary lockout threshold is reached.
2. Verify the system stops accepting additional sign-in attempts for the
   lockout window with a minimized failure response.
3. After the lockout is applied, inspect the relevant audit history from the
   affected workspace context.
4. Verify failed sign-ins and the lockout outcome are auditable and ordered in
   time.

## Scenario 5: Protected writes reject invalid integrity context safely

1. Attempt an authenticated protected write without the required
   `X-CSRF-TOKEN` request integrity header for this phase.
2. Verify the request is rejected with a clear `ProblemDetails` response that
   does not leak protected workspace details.
3. As an authorized workspace owner or admin, inspect recent audit history.
4. Verify the rejection is recorded as a security-relevant auditable outcome
   with the correct workspace, action type, and result.
