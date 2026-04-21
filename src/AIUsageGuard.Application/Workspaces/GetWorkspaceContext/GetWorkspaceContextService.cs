using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Workspaces.GetWorkspaceContext;

public sealed class GetWorkspaceContextService
{
    private readonly IPlatformStore _store;

    public GetWorkspaceContextService(IPlatformStore store)
    {
        _store = store;
    }

    public async Task<WorkspaceContextResult> GetAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var workspace = await _store.FindWorkspaceByIdAsync(workspaceId, cancellationToken)
            ?? throw new RequestFailureException(404, "Workspace not found.");

        if (workspace.Status != WorkspaceStatus.Active)
        {
            throw new RequestFailureException(403, "Workspace is not active.");
        }

        var membership = await _store.FindActiveMembershipAsync(workspaceId, userId, cancellationToken)
            ?? throw new RequestFailureException(403, "User is not a member of the requested workspace.");

        if (membership.Status != MembershipStatus.Active)
        {
            throw new RequestFailureException(403, "Workspace membership is inactive.");
        }

        return new WorkspaceContextResult(workspace, membership);
    }
}
