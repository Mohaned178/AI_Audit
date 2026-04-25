namespace AIUsageGuard.Application.Models;

public sealed record AuditLogListItem(
    Guid AuditLogId,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string ActionType,
    string Category,
    string TargetType,
    string? TargetId,
    string Result,
    string Reason,
    bool IsSecurityRelevant,
    string? CorrelationId);
