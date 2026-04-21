using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Workspaces.ResolveCurrentWorkspace;

public sealed class ResolveCurrentWorkspaceService
{
    private readonly IPlatformStore _store;

    public ResolveCurrentWorkspaceService(IPlatformStore store)
    {
        _store = store;
    }

    public Task<(Workspace Workspace, WorkspaceMembership Membership)> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
        => ResolveAsync(userId, null, cancellationToken);

    public async Task<(Workspace Workspace, WorkspaceMembership Membership)> ResolveAsync(
        Guid userId,
        Guid? preferredWorkspaceId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await _store.ListMembershipsByUserAsync(userId, cancellationToken);
        var activeMemberships = memberships
            .Where(m => m.Status == MembershipStatus.Active)
            .OrderByDescending(m => m.Role)
            .ToList();

        var membership = activeMemberships
            .FirstOrDefault(membership => !preferredWorkspaceId.HasValue || membership.WorkspaceId == preferredWorkspaceId.Value)
            ?? activeMemberships.FirstOrDefault()
            ?? throw new RequestFailureException(403, "User is not assigned to any active workspace.");

        var workspace = await _store.FindWorkspaceByIdAsync(membership.WorkspaceId, cancellationToken)
            ?? throw new RequestFailureException(404, "Workspace not found.");

        if (workspace.Status != WorkspaceStatus.Active)
        {
            throw new RequestFailureException(403, "Workspace is not active.");
        }

        return (workspace, membership);
    }
}
