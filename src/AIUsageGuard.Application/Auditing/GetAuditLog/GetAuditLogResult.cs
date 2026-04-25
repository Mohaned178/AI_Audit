using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Auditing.GetAuditLog;

public sealed record GetAuditLogResult(
    Guid WorkspaceId,
    AuditLogDetail AuditLog);
