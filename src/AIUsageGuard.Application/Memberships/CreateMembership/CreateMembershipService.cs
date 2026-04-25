using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Billing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Memberships.CreateMembership;

public sealed class CreateMembershipService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ApplyWorkspacePlanAssignmentService _planAssignmentService;
    private readonly BillingLimitEvaluator _limitEvaluator;

    public CreateMembershipService(
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

    public async Task<WorkspaceMembership> CreateAsync(
        Guid workspaceId,
        Guid actorUserId,
        string userEmail,
        WorkspaceRole role,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _store.FindWorkspaceByIdAsync(workspaceId, cancellationToken)
            ?? throw new RequestFailureException(404, "Workspace not found.");

        if (workspace.Status != WorkspaceStatus.Active)
        {
            throw new RequestFailureException(403, "Workspace is not active.");
        }

        var actorMembership = await _store.FindActiveMembershipAsync(workspaceId, actorUserId, cancellationToken)
            ?? throw new RequestFailureException(403, "Actor is not a member of this workspace.");

        if (actorMembership.Role is not (WorkspaceRole.Admin or WorkspaceRole.Owner))
        {
            throw new RequestFailureException(403, "Actor lacks permission to manage memberships.");
        }

        var targetUser = await _store.FindUserByEmailAsync(userEmail.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new RequestFailureException(400, "Target user not found.");

        var existingMembership = await _store.FindActiveMembershipAsync(workspaceId, targetUser.Id, cancellationToken);
        if (existingMembership is not null)
        {
            throw new RequestFailureException(400, "User already has an active membership in this workspace.");
        }

        var currentCycle = await _planAssignmentService.EnsureCurrentCycleAsync(workspaceId, actorUserId, DateTimeOffset.UtcNow, cancellationToken);
        var activeMemberMetric = await _store.FindUsageCycleMetricAsync(currentCycle.Id, BillingDimension.ActiveMembers, cancellationToken)
            ?? throw new RequestFailureException(409, "Active-member billing state is not configured for the workspace.");
        var projectedActiveMembers = await _store.CountActiveMembershipsAsync(workspaceId, cancellationToken) + 1m;
        if (_limitEvaluator.WouldExceedHardLimit(activeMemberMetric, projectedActiveMembers))
        {
            await _limitEvaluator.RecordDeniedActionAsync(
                workspaceId,
                actorUserId,
                BillingDimension.ActiveMembers,
                projectedActiveMembers,
                activeMemberMetric.HardLimitQuantity ?? activeMemberMetric.IncludedQuantity,
                "membership.create",
                cancellationToken);
            throw new RequestFailureException(409, "Creating this membership would exceed the workspace active-member limit for the current plan.");
        }

        var membership = new WorkspaceMembership
        {
            WorkspaceId = workspace.Id,
            UserId = targetUser.Id,
            Role = role,
            Status = MembershipStatus.Active
        };

        await _store.AddMembershipAsync(membership, cancellationToken);
        await _limitEvaluator.ApplyAsync(
            workspaceId,
            activeMemberMetric,
            projectedActiveMembers,
            DateTimeOffset.UtcNow,
            "MembershipChange",
            membership.Id.ToString(),
            cancellationToken);

        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = workspace.Id,
            ActorUserId = actorUserId,
            ActionType = "membership.create",
            TargetType = "membership",
            TargetId = membership.Id.ToString(),
            Result = "success",
            Reason = $"Membership created with role {role}."
        }, cancellationToken);

        return membership;
    }
}
