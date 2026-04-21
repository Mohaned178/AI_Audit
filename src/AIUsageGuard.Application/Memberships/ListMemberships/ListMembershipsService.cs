using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Memberships.ListMemberships;

public sealed class ListMembershipsService
{
    private readonly IPlatformStore _store;

    public ListMembershipsService(IPlatformStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<WorkspaceMembership>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return _store.ListMembershipsAsync(workspaceId, cancellationToken);
    }
}
