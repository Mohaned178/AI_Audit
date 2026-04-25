namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record ReportingPeriodResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    int DayCount);
