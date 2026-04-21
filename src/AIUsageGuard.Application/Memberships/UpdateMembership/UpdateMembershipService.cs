using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Memberships.UpdateMembership;

public sealed class UpdateMembershipService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public UpdateMembershipService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
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

        if (currentActiveOwner && !nextActiveOwner && ownerCount <= 1)
        {
            throw new RequestFailureException(409, "A workspace must keep at least one owner.");
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
