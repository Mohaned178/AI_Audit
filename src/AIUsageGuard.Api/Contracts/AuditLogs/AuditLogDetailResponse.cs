namespace AIUsageGuard.Api.Contracts.AuditLogs;

public sealed record AuditLogDetailResponse(
    Guid WorkspaceId,
    AuditLogDetailItemResponse AuditLog);
