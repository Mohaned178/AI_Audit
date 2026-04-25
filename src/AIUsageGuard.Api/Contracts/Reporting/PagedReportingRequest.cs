namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record PagedReportingRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    int PageNumber = 1,
    int PageSize = 20);
