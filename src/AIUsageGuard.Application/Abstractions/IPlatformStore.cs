using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Abstractions;

public interface IPlatformStore
{
    Task<UserAccount?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserAccount?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddUserAsync(UserAccount user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(UserAccount user, CancellationToken cancellationToken = default);

    Task<Workspace?> FindWorkspaceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workspace?> FindWorkspaceBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);
    Task UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);

    Task<WorkspaceMembership?> FindMembershipByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkspaceMembership?> FindActiveMembershipAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspaceMembership>> ListMembershipsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspaceMembership>> ListMembershipsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task AddMembershipAsync(WorkspaceMembership membership, CancellationToken cancellationToken = default);
    Task UpdateMembershipAsync(WorkspaceMembership membership, CancellationToken cancellationToken = default);
    Task<int> CountOwnerMembershipsAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task AddAuditAsync(AuditRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditRecord>> ListAuditsAsync(CancellationToken cancellationToken = default);

    Task<AIUsageEvent?> FindAIUsageEventByIdempotencyKeyAsync(Guid workspaceId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAIUsageEventAsync(AIUsageEvent aiUsageEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AIUsageEvent>> ListAIUsageEventsAsync(
        Guid workspaceId,
        AIUsageEventType? eventType,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountAIUsageEventsAsync(
        Guid workspaceId,
        AIUsageEventType? eventType,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        CancellationToken cancellationToken = default);
}
