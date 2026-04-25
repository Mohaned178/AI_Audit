namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record ReportingPeriodRequest(
    DateOnly FromDate,
    DateOnly ToDate);
