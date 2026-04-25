namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record AlertsSummaryResponse(
    Guid WorkspaceId,
    ReportingPeriodResponse Period,
    AlertsSummaryViewResponse Summary);
