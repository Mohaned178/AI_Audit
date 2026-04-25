namespace AIUsageGuard.Application.Reporting;

public sealed record ReportingPeriodQuery(
    DateOnly FromDate,
    DateOnly ToDate);
