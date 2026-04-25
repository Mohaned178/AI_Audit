namespace AIUsageGuard.Application.Auditing.GetAuditLog;

public sealed record GetAuditLogQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    Guid AuditLogId);
