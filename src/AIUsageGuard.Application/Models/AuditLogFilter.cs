namespace AIUsageGuard.Application.Models;

public sealed class AuditLogFilter
{
    public Guid WorkspaceId { get; init; }

    public Guid RequestedByUserId { get; init; }

    public DateTimeOffset? FromOccurredAtUtc { get; init; }

    public DateTimeOffset? ToOccurredAtUtc { get; init; }

    public string? ActionType { get; init; }

    public string? Result { get; init; }

    public Guid? ActorUserId { get; init; }

    public bool? IsSecurityRelevant { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
