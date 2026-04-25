# Quickstart: Phase 6 Usage Limits and Billing-Ready Design

## Goal

Verify that workspace owners and admins can review current plan status, observe
warning or restriction behavior at supported limit boundaries, and inspect
billing-ready monthly cycle history without breaking workspace isolation.

## Preconditions

1. The Phase 1 SaaS foundation is working with authenticated users and
   workspace membership.
2. The Phase 2 AI usage event ingestion endpoints are available and can store
   representative counted activity with estimated-cost values where applicable.
3. The Phase 4 reporting endpoints are available because plan status and cycle
   views reuse the same underlying period semantics for usage and spend.
4. The Phase 5 background-processing infrastructure is available so cycle
   rollover and reconciliation can run in non-production verification.
5. At least one plan catalog entry and one workspace plan assignment exist for
   the target workspace.
6. You have:
   - a signed-in workspace owner or admin session for the target workspace
   - a separate signed-in regular member session in that workspace
   - a separate signed-in owner or admin session for a different workspace

## Scenario 1: Review current workspace plan status

1. As a workspace owner or admin, call:
   `GET /workspaces/{workspaceId}/billing/plan-status`
2. Verify the request returns `200 OK`.
3. Verify the response identifies the active plan, current cycle dates, and the
   per-dimension status for active members, monthly AI activity, and estimated
   spend for the requested workspace only.
4. Verify each dimension clearly shows the included quantity, current quantity,
   remaining quantity or overage quantity, limit behavior, and current state.

## Scenario 2: Observe warning or restriction behavior

1. Configure or seed the target workspace on a plan with a low warning or hard
   limit for at least one supported dimension.
2. Trigger counted activity in the target workspace by either:
   - creating or reactivating workspace memberships to affect the active-member
     dimension
   - submitting accepted AI usage events to affect the activity-volume and
     estimated-spend dimensions
3. Verify that activity below the threshold remains allowed and updates the
   current-cycle totals.
4. Continue until the workspace reaches a warning threshold.
5. Call:
   `GET /workspaces/{workspaceId}/billing/plan-status`
6. Verify the response now shows the affected dimension in a warning state.
7. If the plan restricts that dimension, attempt one more protected write and
   verify the request is rejected with a clear `ProblemDetails` response that
   explains the workspace limit restriction.
8. If the plan allows overage for that dimension, continue once past the
   included allowance and verify the response shows accepted usage plus a
   non-zero overage quantity.

## Scenario 3: Review billing-ready cycle history

1. Call:
   `GET /workspaces/{workspaceId}/billing/cycles?pageNumber=1&pageSize=20`
2. Verify the request returns `200 OK`.
3. Verify the newest entry identifies the cycle dates, plan in effect,
   per-dimension totals, warning count, restriction count, and adjustment count
   for the target workspace only.
4. Open one cycle detail:
   `GET /workspaces/{workspaceId}/billing/cycles/{cycleId}`
5. Verify the detail shows the metric summaries, limit events, and any
   post-cycle adjustments without exposing raw prompts, raw files, or another
   workspace's data.

## Scenario 4: Confirm post-cycle adjustment handling

1. In a non-production environment, create or simulate accepted late activity
   that belongs to a previously closed monthly cycle for the target workspace.
2. Run or wait for the reconciliation pass that updates closed cycles.
3. Re-open:
   `GET /workspaces/{workspaceId}/billing/cycles/{cycleId}`
4. Verify the cycle status reflects an adjusted state when appropriate and that
   the adjustment list explains which dimension changed, by how much, and why.
5. Verify the revised totals match the adjustment and that the change is
   auditable.

## Scenario 5: Authorization boundaries hold

1. As the signed-in regular member session, call:
   `GET /workspaces/{workspaceId}/billing/plan-status`
2. Verify access is denied with `403 Forbidden`.
3. As the separate signed-in owner or admin from another workspace, call:
   `GET /workspaces/{workspaceId}/billing/cycles?pageNumber=1&pageSize=20`
4. Verify access is denied with `403 Forbidden`.
5. Verify both successful and denied billing reads produce audit evidence.
