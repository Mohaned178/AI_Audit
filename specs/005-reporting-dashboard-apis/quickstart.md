# Quickstart: Phase 4 Reporting and Dashboard APIs

## Goal

Verify that workspace owners and admins can retrieve useful reporting summaries
for a selected period, that grouped reporting stays tenant-scoped, and that
cost completeness and empty states are communicated clearly.

## Preconditions

1. The Phase 1 SaaS foundation is working with authenticated users and
   workspace membership.
2. The Phase 2 AI usage event ingestion endpoints are available and have stored
   representative activity for at least one workspace.
3. The Phase 3 risk detection endpoints are available and have created findings
   for some of that activity.
4. You have a signed-in workspace owner or admin session for one workspace.
5. You have separate signed-in sessions available for:
   - a regular member added to the first workspace
   - an owner or admin of a different workspace with separate data

## Scenario 1: View the dashboard overview for a valid reporting period

1. As a workspace owner or admin, call:
   `GET /workspaces/{workspaceId}/dashboard?fromDate=2026-04-01&toDate=2026-04-21`
2. Verify the request returns `200 OK`.
3. Verify the response includes:
   - the normalized reporting period
   - total AI activity count
   - total flagged finding count
   - top user summary rows
   - top tool summary rows
   - alerts summary
   - estimated cost summary with completeness metadata
4. Verify the totals reflect only the selected workspace and period.
5. Verify the response does not include prompt previews, file names, or copied
   finding evidence.

## Scenario 2: Retrieve grouped user and tool usage reports

1. As a workspace owner or admin, call:
   `GET /workspaces/{workspaceId}/reports/usage-by-user?fromDate=2026-04-01&toDate=2026-04-21&pageNumber=1&pageSize=20`
2. Verify the request returns `200 OK`.
3. Verify each row includes:
   - actor identifier
   - display label
   - total event count
   - flagged finding count
   - estimated cost totals and completeness values
4. Call:
   `GET /workspaces/{workspaceId}/reports/usage-by-tool?fromDate=2026-04-01&toDate=2026-04-21&pageNumber=1&pageSize=20`
5. Verify the request returns `200 OK`.
6. Verify tool rows are ranked consistently and match the dashboard's top-tool
   subset for the same period.

## Scenario 3: Review alerts and cost summaries

1. As a workspace owner or admin, call:
   `GET /workspaces/{workspaceId}/reports/alerts-summary?fromDate=2026-04-01&toDate=2026-04-21`
2. Verify the request returns `200 OK`.
3. Verify the response includes total findings, severity distribution, affected
   actor and tool counts, and daily trend buckets for the selected period.
4. Call:
   `GET /workspaces/{workspaceId}/reports/cost-summary?fromDate=2026-04-01&toDate=2026-04-21`
5. Verify the request returns `200 OK`.
6. Verify the response includes total estimated cost, events with cost, events
   missing cost, and an `isPartial` indicator when any source event lacks cost
   data.

## Scenario 4: Confirm empty-state and invalid-range behavior

1. Request any dashboard or report endpoint for a date range with no matching
   data.
2. Verify the endpoint returns `200 OK` with a successful empty-state response with zero
   totals, full period-aligned trend buckets, and empty collections rather
   than an error.
3. Request:
   `GET /workspaces/{workspaceId}/dashboard?fromDate=2026-04-21&toDate=2026-04-01`
4. Verify the request is rejected with `400 Bad Request` and a clear validation error.
5. Request a date range larger than the supported maximum period.
6. Verify the request is rejected with a clear validation error explaining the
   supported 93-day range limit.

## Scenario 5: Authorization boundaries hold

1. As the separate signed-in regular member session, call:
   `GET /workspaces/{workspaceId}/dashboard?fromDate=2026-04-01&toDate=2026-04-21`
2. Verify access is denied with `403 Forbidden`.
3. As the separate signed-in owner or admin from a different workspace, call any reporting endpoint for the
   target workspace.
4. Verify access is denied with `403 Forbidden` and no totals from the target workspace are
   disclosed.
5. Verify successful and denied reporting requests both produce audit evidence.
