# Quickstart: Phase 3 Risk Detection Engine

## Goal

Verify that accepted AI usage events are evaluated against workspace-scoped risk
rules, that admins can review the resulting findings, and that workspace policy
inputs change future evaluation behavior.

## Preconditions

1. The Phase 1 SaaS foundation is working with authenticated users and
   workspace membership.
2. The Phase 2 AI usage event ingestion endpoints are available.
3. You have a signed-in workspace owner or admin session for one workspace.
4. You have at least one workspace member session available for event
   submission.

## Scenario 1: Detect a risky event and review the finding

1. As a workspace owner or admin, request the current workspace risk policy:
   `GET /workspaces/{workspaceId}/risk-policy`
2. Update the policy so only `ChatGPT` is approved and both cost thresholds are
   intentionally low for testing:
   `PUT /workspaces/{workspaceId}/risk-policy`
3. As a workspace member, submit an AI usage event that:
   - uses a non-approved tool name such as `Claude`
   - includes a prompt preview containing an email address
   - includes an estimated cost above the configured per-event threshold
4. Confirm the ingestion request is still accepted through the Phase 2 events
   endpoint.
5. As a workspace owner or admin, request:
   `GET /workspaces/{workspaceId}/risk-findings`
6. Verify that:
   - only findings for the current workspace are returned
   - the event created multiple findings if multiple rules matched
   - each finding includes `ruleType`, `severity`, `status`, and `reason`
   - rule and status values use the documented snake_case API strings such as
     `sensitive_data_pattern` and `open`
7. Request the specific finding detail:
   `GET /workspaces/{workspaceId}/risk-findings/{findingId}`
8. Verify the detail explains why the event was flagged and includes the
   relevant event context plus the evaluation result fields without exposing
   unnecessary copied payload data.

## Scenario 2: Clean event produces no finding

1. As a workspace member, submit an AI usage event using an approved tool with
   no sensitive text patterns, no file upload metadata, and a cost below the
   configured thresholds.
2. As a workspace owner or admin, request:
   `GET /workspaces/{workspaceId}/risk-findings`
3. Verify no new finding was created for that event.
4. Verify audit history or administrative diagnostics can still show that the
   event was evaluated without creating a finding.

## Scenario 3: Policy changes affect future evaluations only

1. As a workspace owner or admin, update the workspace risk policy to add the
   previously unapproved tool to the approved list and raise the cost
   thresholds.
2. As a workspace member, resubmit a comparable event after the policy change.
3. Verify the new event no longer creates unapproved-tool or threshold findings
   unless another supported rule still matches.
4. Verify older findings remain unchanged and reviewable.
5. Verify the policy response now reflects `createdAt`, `lastUpdatedAt`, and
   `lastUpdatedByUserId`.

## Scenario 4: Authorization boundaries hold

1. As a workspace member without admin rights, call:
   `GET /workspaces/{workspaceId}/risk-findings`
2. Verify access is denied.
3. As the same member, call:
   `PUT /workspaces/{workspaceId}/risk-policy`
4. Verify access is denied and an audit record is produced.
5. As an admin from a different workspace, attempt to read or update the target
   workspace's findings or policy.
6. Verify all cross-workspace attempts are denied.
