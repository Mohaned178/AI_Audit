# AI Usage Guard API Test Cases

This guide is intended for manual validation through Swagger UI or Postman.

## Testing Notes

- Sign in first through `POST /auth/register` or `POST /auth/login` so the browser session carries the authentication cookie.
- In Development, call `GET /dev/antiforgery-token` first to get a fresh antiforgery request token.
- Send the returned `RequestToken` value in the `X-CSRF-TOKEN` header on protected write requests.
- Workspace-scoped endpoints require the `workspaceId` that belongs to the authenticated user.
- Protected write endpoints use antiforgery protection and expect the `X-CSRF-TOKEN` header.
- Swagger UI is most useful for read operations; for protected writes, Postman may be easier unless you already have the CSRF token available.
- Owner and admin are both valid for admin-only endpoints because the role order is `Member < Admin < Owner`.

## Development Antiforgery Token

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Get antiforgery token for manual testing | `GET /dev/antiforgery-token` | None, Development only | None | `200` | Returns a request token and sets the antiforgery cookie used by protected write requests. |

## Authentication Flow

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Register a new workspace | `POST /auth/register` | None | `{"email":"owner@acme.test","password":"P@ssw0rd!","displayName":"Acme Owner","workspaceName":"Acme Security"}` | `201` | Returns a workspace session, creates the initial owner membership, and sets the auth cookie. |
| Log in with valid credentials | `POST /auth/login` | None | `{"email":"owner@acme.test","password":"P@ssw0rd!"}` | `200` | Returns the current workspace session and sets the auth cookie. |
| Log out | `POST /auth/logout` | Authenticated | None | `204` | Clears the current session cookie. |
| Register with an already-used email | `POST /auth/register` | None | Same email as an existing account | `400` | Request is rejected with a business-rule failure. |
| Log in with invalid credentials | `POST /auth/login` | None | Wrong password | `400` | Sign-in is rejected and no session is created. |

## Workspace Context Checks

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Read current workspace context | `GET /workspaces/{workspaceId}/context` | Member | None | `200` | Returns workspace id, workspace name, and the caller role. |
| Read context without a session | `GET /workspaces/{workspaceId}/context` | Unauthenticated | None | `401` | Access is denied. |
| Read context for a workspace the caller does not belong to | `GET /workspaces/{workspaceId}/context` | Member | None | `403` | Cross-workspace access is denied. |
| Read context with an unknown workspace id | `GET /workspaces/{workspaceId}/context` | Member | None | `404` | Workspace not found. |

## AI Usage Event Ingestion

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Ingest a prompt event | `POST /workspaces/{workspaceId}/events` | Member | `{"idempotencyKey":"evt-001","eventType":"prompt_submitted","occurredAt":"2026-04-24T10:00:00Z","toolName":"ChatGPT","modelName":"gpt-4.1","sourceLabel":"browser","promptPreview":"Summarize the policy","fileName":null,"fileSizeBytes":null,"inputTokenCount":120,"outputTokenCount":80,"estimatedCost":0.012,"details":{"category":"productivity"}}` | `201` | Returns an accepted ingestion response and stores the event. |
| Ingest a file upload event | `POST /workspaces/{workspaceId}/events` | Member | File-related payload with `eventType":"file_uploaded"` | `201` | Returns the stored event and flags the file metadata. |
| Repeat the same event with the same idempotency key | `POST /workspaces/{workspaceId}/events` | Member | Same payload as a prior successful request | `200` | Response indicates a duplicate and no second event is created. |
| Ingest without a CSRF header | `POST /workspaces/{workspaceId}/events` | Member | Valid payload but omit `X-CSRF-TOKEN` | `400` | Antiforgery protection rejects the request. |
| Ingest with an unsupported event type | `POST /workspaces/{workspaceId}/events` | Member | `{"eventType":"unsupported_event", ...}` | `400` | Request is rejected with the supported-event-type error. |
| Ingest with negative token counts | `POST /workspaces/{workspaceId}/events` | Member | `inputTokenCount:-1` or `outputTokenCount:-1` | `400` | Validation rejects the payload. |

## Risk Policy

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Read current risk policy | `GET /workspaces/{workspaceId}/risk-policy` | Admin | None | `200` | Returns approved tools and threshold values. |
| Update approved tools and cost thresholds | `PUT /workspaces/{workspaceId}/risk-policy` | Admin | `{"approvedTools":["ChatGPT","Claude"],"perEventEstimatedCostThreshold":0.05,"dailyEstimatedCostThreshold":10.0}` | `200` | Policy is updated and returned with the latest timestamps. |
| Update policy without a CSRF header | `PUT /workspaces/{workspaceId}/risk-policy` | Admin | Valid body but omit `X-CSRF-TOKEN` | `400` | Antiforgery protection rejects the request. |
| Update policy with negative threshold values | `PUT /workspaces/{workspaceId}/risk-policy` | Admin | `{"perEventEstimatedCostThreshold":-1}` | `400` | Negative thresholds are rejected. |

## Risk Findings

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| List findings | `GET /workspaces/{workspaceId}/risk-findings` | Admin | Query filters such as `severity=high&pageNumber=1&pageSize=20` | `200` | Returns a paged list of findings for the workspace. |
| Read finding detail | `GET /workspaces/{workspaceId}/risk-findings/{findingId}` | Admin | None | `200` | Returns the finding, the matched outcome, and the triggering event context. |
| Filter findings by rule type | `GET /workspaces/{workspaceId}/risk-findings?ruleType=sensitive_data_pattern` | Admin | None | `200` | Returns only findings that match the requested rule type. |
| Read a finding from another workspace | `GET /workspaces/{workspaceId}/risk-findings/{findingId}` | Admin | None | `403` or `404` | Access is denied or the finding is not visible in the requested workspace. |
| Read a missing finding id | `GET /workspaces/{workspaceId}/risk-findings/{findingId}` | Admin | None | `404` | Finding not found. |

## Dashboard and Reporting

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Read dashboard totals | `GET /workspaces/{workspaceId}/dashboard` | Admin | Query `fromDate=2026-04-01&toDate=2026-04-24` | `200` | Returns dashboard totals for the requested period. |
| Read usage by user | `GET /workspaces/{workspaceId}/reports/usage-by-user` | Admin | Query `fromDate=2026-04-01&toDate=2026-04-24&pageNumber=1&pageSize=20` | `200` | Returns a paged usage summary grouped by user. |
| Read usage by tool | `GET /workspaces/{workspaceId}/reports/usage-by-tool` | Admin | Query `fromDate=2026-04-01&toDate=2026-04-24&pageNumber=1&pageSize=20` | `200` | Returns a paged usage summary grouped by tool. |
| Read alerts summary | `GET /workspaces/{workspaceId}/reports/alerts-summary` | Admin | Query `fromDate=2026-04-01&toDate=2026-04-24` | `200` | Returns alert counts and severity breakdown for the selected period. |
| Read cost summary | `GET /workspaces/{workspaceId}/reports/cost-summary` | Admin | Query `fromDate=2026-04-01&toDate=2026-04-24` | `200` | Returns estimated cost and trend data for the selected period. |
| Use an invalid reporting period | Any reporting endpoint above | Admin | `fromDate=2026-04-24&toDate=2026-04-01` | `400` | The API rejects reversed date ranges. |
| Request a reporting period longer than supported | Any reporting endpoint above | Admin | More than 93 days in the query window | `400` | The API rejects unsupported period length. |

## Billing

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Read plan status | `GET /workspaces/{workspaceId}/billing/plan-status` | Admin | None | `200` | Returns the active plan, current cycle, and assignment metadata. |
| Read billing cycle history | `GET /workspaces/{workspaceId}/billing/cycles` | Admin | Query `pageNumber=1&pageSize=20` | `200` | Returns a paged billing cycle list. |
| Read billing cycle detail | `GET /workspaces/{workspaceId}/billing/cycles/{cycleId}` | Admin | None | `200` | Returns the billing cycle summary and related metrics. |
| Read billing cycle for a missing id | `GET /workspaces/{workspaceId}/billing/cycles/{cycleId}` | Admin | None | `404` | Billing cycle not found. |
| Request plan status for another workspace | `GET /workspaces/{workspaceId}/billing/plan-status` | Admin | None | `403` or `404` | Cross-workspace access is denied. |

## Notifications

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| List notifications | `GET /workspaces/{workspaceId}/notifications` | Admin | Query `type=digest&status=delivered&pageNumber=1&pageSize=20` | `200` | Returns a paged notification list. |
| Read notification detail | `GET /workspaces/{workspaceId}/notifications/{notificationId}` | Admin | None | `200` | Returns notification details and delivery outcomes. |
| Read notification preferences | `GET /workspaces/{workspaceId}/notification-preferences` | Admin | None | `200` | Returns the current delivery configuration. |
| Update notification preferences | `PUT /workspaces/{workspaceId}/notification-preferences` | Admin | `{"urgentAlertsEnabled":true,"digestEnabled":true,"digestCadence":"weekly","recipientSelectionMode":"all_admins_and_owners","selectedRecipientUserIds":[]}` | `200` | Preferences are updated and returned. |
| Update preferences with selected recipients | `PUT /workspaces/{workspaceId}/notification-preferences` | Admin | `{"urgentAlertsEnabled":true,"digestEnabled":true,"digestCadence":"daily","recipientSelectionMode":"selected_recipients","selectedRecipientUserIds":["<admin-user-id>"]}` | `200` | Preferences are saved for the selected recipients. |
| Update preferences without a CSRF header | `PUT /workspaces/{workspaceId}/notification-preferences` | Admin | Valid body but omit `X-CSRF-TOKEN` | `400` | Antiforgery protection rejects the request. |
| Use an invalid digest cadence | `PUT /workspaces/{workspaceId}/notification-preferences` | Admin | `{"digestCadence":"monthly"}` | `400` | Unsupported digest cadence is rejected. |

## Audit Logs

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| List audit logs | `GET /workspaces/{workspaceId}/audit-logs` | Admin | Query `fromOccurredAt=2026-04-01T00:00:00Z&toOccurredAt=2026-04-24T23:59:59Z&pageNumber=1&pageSize=20` | `200` | Returns workspace audit entries. |
| Read audit log detail | `GET /workspaces/{workspaceId}/audit-logs/{auditLogId}` | Admin | None | `200` | Returns the audit record with client context and reason fields. |
| Filter by security relevance | `GET /workspaces/{workspaceId}/audit-logs?securityRelevant=true` | Admin | None | `200` | Returns only security-relevant events. |
| Read a missing audit log | `GET /workspaces/{workspaceId}/audit-logs/{auditLogId}` | Admin | None | `404` | Audit log not found. |

## Memberships

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| List memberships | `GET /workspaces/{workspaceId}/memberships` | Member | None | `200` | Returns all memberships in the workspace. |
| Create a membership | `POST /workspaces/{workspaceId}/memberships` | Admin | `{"userEmail":"member@acme.test","role":"Member"}` | `201` | Adds the user to the workspace and returns the created membership. |
| Create a membership without a CSRF header | `POST /workspaces/{workspaceId}/memberships` | Admin | Valid body but omit `X-CSRF-TOKEN` | `400` | Antiforgery protection rejects the request. |
| Promote a member to admin | `PATCH /workspaces/{workspaceId}/memberships/{membershipId}` | Admin | `{"role":"Admin","status":"Active"}` | `200` | Membership role is updated. |
| Deactivate a membership | `PATCH /workspaces/{workspaceId}/memberships/{membershipId}` | Admin | `{"status":"Inactive"}` | `200` | Membership becomes inactive and loses access on the next authorization check. |
| Create a membership for a non-existent user | `POST /workspaces/{workspaceId}/memberships` | Admin | Unknown email address | `400` | Target user not found. |
| Update a missing membership | `PATCH /workspaces/{workspaceId}/memberships/{membershipId}` | Admin | Valid body | `404` | Membership not found. |

## Authorization Cases

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Member can read workspace context | `GET /workspaces/{workspaceId}/context` | Member | None | `200` | Member access is allowed. |
| Member cannot read admin-only reporting | `GET /workspaces/{workspaceId}/reports/usage-by-user` | Member | None | `403` | Member is denied. |
| Admin can read admin-only reporting | `GET /workspaces/{workspaceId}/reports/usage-by-user` | Admin | None | `200` | Admin access is allowed. |
| Owner can read admin-only reporting | `GET /workspaces/{workspaceId}/reports/usage-by-user` | Owner | None | `200` | Owner inherits admin-level access. |
| Unauthenticated user cannot read workspace data | Any workspace endpoint | Unauthenticated | None | `401` | Request is rejected before workspace authorization. |
| Authenticated user without workspace membership | Any workspace endpoint | Authenticated, no membership | None | `403` | Cross-workspace access is denied. |

## Validation Failure Cases

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Missing required auth fields | `POST /auth/register` | None | Missing email or password | `400` | Model binding or validation rejects the request. |
| Unsupported event type | `POST /workspaces/{workspaceId}/events` | Member | `eventType:"invalid"` | `400` | RequestFailureException for unsupported event type. |
| Invalid page number | `GET /workspaces/{workspaceId}/events?pageNumber=0` | Admin | None | `400` | Page number must be at least 1. |
| Invalid page size | `GET /workspaces/{workspaceId}/events?pageSize=500` | Admin | None | `400` | Page size outside the supported range is rejected. |
| Reversed date range | Reporting or audit list endpoint | Admin | `fromDate` after `toDate` | `400` | Date range validation fails. |
| Negative cost threshold | `PUT /workspaces/{workspaceId}/risk-policy` | Admin | `{"dailyEstimatedCostThreshold":-5}` | `400` | Threshold validation fails. |
| Invalid notification cadence | `PUT /workspaces/{workspaceId}/notification-preferences` | Admin | `{"digestCadence":"monthly"}` | `400` | Unsupported cadence is rejected. |

## Not Found Cases

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Unknown workspace | `GET /workspaces/{workspaceId}/context` | Member | None | `404` | Workspace not found. |
| Missing risk finding | `GET /workspaces/{workspaceId}/risk-findings/{findingId}` | Admin | None | `404` | Risk finding not found. |
| Missing notification | `GET /workspaces/{workspaceId}/notifications/{notificationId}` | Admin | None | `404` | Notification not found. |
| Missing billing cycle | `GET /workspaces/{workspaceId}/billing/cycles/{cycleId}` | Admin | None | `404` | Billing cycle not found. |
| Missing audit log | `GET /workspaces/{workspaceId}/audit-logs/{auditLogId}` | Admin | None | `404` | Audit log not found. |
| Missing membership | `PATCH /workspaces/{workspaceId}/memberships/{membershipId}` | Admin | Valid body | `404` | Membership not found. |

## Idempotency and Security Notes

| Scenario | HTTP Method and Endpoint | Required Role | Request Body | Expected Status Code | Expected Result / Assertion |
|---|---|---:|---|---:|---|
| Obtain a token, then ingest an event manually | `GET /dev/antiforgery-token` followed by `POST /workspaces/{workspaceId}/events` | Member | First request has no body, second request uses a valid event payload | `200` or `201` | The GET response provides the token, and the POST succeeds when the same session cookie plus `X-CSRF-TOKEN` are sent together. |
| Duplicate usage event submission | `POST /workspaces/{workspaceId}/events` | Member | Same `idempotencyKey` and payload as an earlier request | `200` | The second call is treated as a duplicate and does not create another event. |
| Protected write without CSRF token | Any `PUT`, `POST`, or `PATCH` endpoint marked with protected request integrity | Admin or Member | Valid body but no `X-CSRF-TOKEN` | `400` | Antiforgery protection blocks the request. |
| Protected write with CSRF token and valid cookie | Same protected endpoint | Admin or Member | Valid body plus `X-CSRF-TOKEN` | `200` or `201` | Request succeeds when the session cookie and CSRF token are both present. |
| Cross-workspace write attempt | Any workspace-scoped write endpoint | Authenticated but not a member | Valid body | `403` | The API denies access before business logic runs. |

## Suggested Manual Test Order

1. Register a workspace and capture the returned workspace id.
2. Add a member and an admin user.
3. Log in as the admin and verify workspace context.
4. Test event ingestion and repeat the same request to confirm idempotency.
5. Update risk policy, then query findings, dashboard, and reporting endpoints.
6. Verify billing, notifications, memberships, and audit logs.
7. Repeat a few requests as a non-member and as an unauthenticated client to confirm isolation.
