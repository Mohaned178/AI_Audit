using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Billing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Memberships.UpdateMembership;

public sealed class UpdateMembershipService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ApplyWorkspacePlanAssignmentService _planAssignmentService;
    private readonly BillingLimitEvaluator _limitEvaluator;

    public UpdateMembershipService(
        IPlatformStore store,
        IAuditService auditService,
        ApplyWorkspacePlanAssignmentService planAssignmentService,
        BillingLimitEvaluator limitEvaluator)
    {
        _store = store;
        _auditService = auditService;
        _planAssignmentService = planAssignmentService;
        _limitEvaluator = limitEvaluator;
    }

    public async Task<WorkspaceMembership> UpdateAsync(
        Guid workspaceId,
        Guid actorUserId,
        Guid membershipId,
        WorkspaceRole? role,
        MembershipStatus? status,
        CancellationToken cancellationToken = default)
    {
        var actorMembership = await _store.FindActiveMembershipAsync(workspaceId, actorUserId, cancellationToken)
            ?? throw new RequestFailureException(403, "Actor is not a member of this workspace.");

        var workspace = await _store.FindWorkspaceByIdAsync(workspaceId, cancellationToken)
            ?? throw new RequestFailureException(404, "Workspace not found.");

        if (workspace.Status != WorkspaceStatus.Active)
        {
            throw new RequestFailureException(403, "Workspace is not active.");
        }

        if (actorMembership.Role is not (WorkspaceRole.Admin or WorkspaceRole.Owner))
        {
            throw new RequestFailureException(403, "Actor lacks permission to manage memberships.");
        }

        var membership = await _store.FindMembershipByIdAsync(membershipId, cancellationToken)
            ?? throw new RequestFailureException(404, "Membership not found.");

        if (membership.WorkspaceId != workspaceId)
        {
            throw new RequestFailureException(403, "Membership does not belong to the requested workspace.");
        }

        var previousRole = membership.Role;
        var previousStatus = membership.Status;
        var currentActiveOwner = membership.Role == WorkspaceRole.Owner && membership.Status == MembershipStatus.Active;
        var nextRole = role ?? membership.Role;
        var nextStatus = status ?? membership.Status;
        var nextActiveOwner = nextRole == WorkspaceRole.Owner && nextStatus == MembershipStatus.Active;
        var ownerCount = await _store.CountOwnerMembershipsAsync(workspaceId, cancellationToken);
        var activeMembershipCount = await _store.CountActiveMembershipsAsync(workspaceId, cancellationToken);
        var isCurrentlyActive = membership.Status == MembershipStatus.Active;
        var willBeActive = nextStatus == MembershipStatus.Active;
        var projectedActiveMembershipCount = activeMembershipCount + (willBeActive && !isCurrentlyActive ? 1 : 0) - (!willBeActive && isCurrentlyActive ? 1 : 0);

        if (currentActiveOwner && !nextActiveOwner && ownerCount <= 1)
        {
            throw new RequestFailureException(409, "A workspace must keep at least one owner.");
        }

        UsageCycleMetric? activeMemberMetric = null;
        if (projectedActiveMembershipCount != activeMembershipCount)
        {
            var currentCycle = await _planAssignmentService.EnsureCurrentCycleAsync(workspaceId, actorUserId, DateTimeOffset.UtcNow, cancellationToken);
            activeMemberMetric = await _store.FindUsageCycleMetricAsync(currentCycle.Id, BillingDimension.ActiveMembers, cancellationToken)
                ?? throw new RequestFailureException(409, "Active-member billing state is not configured for the workspace.");

            if (projectedActiveMembershipCount > activeMembershipCount &&
                _limitEvaluator.WouldExceedHardLimit(activeMemberMetric, projectedActiveMembershipCount))
            {
                await _limitEvaluator.RecordDeniedActionAsync(
                    workspaceId,
                    actorUserId,
                    BillingDimension.ActiveMembers,
                    projectedActiveMembershipCount,
                    activeMemberMetric.HardLimitQuantity ?? activeMemberMetric.IncludedQuantity,
                    "membership.update",
                    cancellationToken);
                throw new RequestFailureException(409, "Updating this membership would exceed the workspace active-member limit for the current plan.");
            }
        }

        if (role.HasValue)
        {
            membership.Role = role.Value;
        }

        if (status.HasValue)
        {
            if (status == MembershipStatus.Removed &&
                membership.Role == WorkspaceRole.Owner &&
                ownerCount <= 1)
            {
                throw new RequestFailureException(409, "A workspace must keep at least one owner.");
            }

            membership.Status = status.Value;
        }

        if (membership.Role == WorkspaceRole.Owner && membership.Status == MembershipStatus.Removed)
        {
            throw new RequestFailureException(409, "An owner membership cannot be removed.");
        }

        membership.LastUpdatedAt = DateTimeOffset.UtcNow;
        await _store.UpdateMembershipAsync(membership, cancellationToken);
        if (activeMemberMetric is not null)
        {
            await _limitEvaluator.ApplyAsync(
                workspaceId,
                activeMemberMetric,
                projectedActiveMembershipCount,
                DateTimeOffset.UtcNow,
                "MembershipChange",
                membership.Id.ToString(),
                cancellationToken);
        }

        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorUserId,
            ActionType = "membership.update",
            TargetType = "membership",
            TargetId = membership.Id.ToString(),
            Result = "success",
            Reason = $"Role {previousRole} -> {membership.Role}; status {previousStatus} -> {membership.Status}."
        }, cancellationToken);

        return membership;
    }
}
