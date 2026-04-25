namespace AIUsageGuard.Api.Contracts.AuditLogs;

public sealed record AuditLogSummaryResponse(
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
