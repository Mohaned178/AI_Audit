using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Reporting;

public static class ReportingPeriodValidator
{
    public const int MaxSupportedDayCount = 93;

    public static ReportingPeriod ValidateAndNormalize(ReportingPeriodQuery query)
    {
        if (query.FromDate > query.ToDate)
        {
            throw new RequestFailureException(400, "fromDate must be earlier than or equal to toDate.");
        }

        var dayCount = query.ToDate.DayNumber - query.FromDate.DayNumber + 1;
        if (dayCount > MaxSupportedDayCount)
        {
            throw new RequestFailureException(400, $"Reporting periods longer than {MaxSupportedDayCount} days are not supported.");
        }

        return new ReportingPeriod
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            NormalizedFromUtc = new DateTimeOffset(query.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
            NormalizedToUtc = new DateTimeOffset(query.ToDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)),
            DayCount = dayCount
        };
    }

    public static void ValidatePage(int pageNumber, int pageSize, int maxPageSize = 100)
    {
        if (pageNumber < 1)
        {
            throw new RequestFailureException(400, "pageNumber must be at least 1.");
        }

        if (pageSize < 1 || pageSize > maxPageSize)
        {
            throw new RequestFailureException(400, $"pageSize must be between 1 and {maxPageSize}.");
        }
    }
}
