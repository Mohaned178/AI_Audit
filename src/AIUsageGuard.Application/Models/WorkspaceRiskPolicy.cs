namespace AIUsageGuard.Application.Models;

public sealed class WorkspaceRiskPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public List<string> ApprovedTools { get; set; } = [];

    public decimal? PerEventEstimatedCostThreshold { get; set; }

    public decimal? DailyEstimatedCostThreshold { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid LastUpdatedByUserId { get; set; }
}
