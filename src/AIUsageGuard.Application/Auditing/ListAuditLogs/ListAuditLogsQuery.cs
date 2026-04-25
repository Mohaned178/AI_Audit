namespace AIUsageGuard.Application.Auditing.ListAuditLogs;

public sealed record ListAuditLogsQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    DateTimeOffset? FromOccurredAtUtc,
    DateTimeOffset? ToOccurredAtUtc,
    string? ActionType,
    string? Result,
    Guid? ActorUserId,
    bool? SecurityRelevant,
    int PageNumber,
    int PageSize);
