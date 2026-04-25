namespace AIUsageGuard.Api.Contracts.AuditLogs;

public sealed record AuditLogPageResponse(
    IReadOnlyList<AuditLogSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
