namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record CostSummaryResponse(
    Guid WorkspaceId,
    ReportingPeriodResponse Period,
    CostSummaryViewResponse Summary);
