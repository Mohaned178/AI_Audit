namespace AIUsageGuard.Application.Models;

public sealed class UsageSummaryPage
{
    public IReadOnlyList<UsageSummaryRow> Items { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}
