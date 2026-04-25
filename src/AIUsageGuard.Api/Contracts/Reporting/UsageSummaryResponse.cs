namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record UsageSummaryResponse(
    Guid WorkspaceId,
    ReportingPeriodResponse Period,
    string GroupedBy,
    UsageSummaryPageResponse Page);
