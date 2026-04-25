namespace AIUsageGuard.Application.Models;

public sealed class BackgroundJobRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public BackgroundJobType JobType { get; set; }

    public Guid? WorkspaceId { get; set; }

    public DateTimeOffset ScheduledForUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public BackgroundJobRunStatus Status { get; set; } = BackgroundJobRunStatus.Running;

    public int ProcessedItemCount { get; set; }

    public string? FailureSummary { get; set; }
}
