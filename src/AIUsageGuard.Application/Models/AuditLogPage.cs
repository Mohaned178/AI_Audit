namespace AIUsageGuard.Application.Models;

public sealed record AuditLogPage(
    Guid WorkspaceId,
    IReadOnlyList<AuditLogListItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
