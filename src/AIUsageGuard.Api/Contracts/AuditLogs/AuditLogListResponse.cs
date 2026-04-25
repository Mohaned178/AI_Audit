namespace AIUsageGuard.Api.Contracts.AuditLogs;

public sealed record AuditLogListResponse(
    Guid WorkspaceId,
    AuditLogPageResponse Page);
