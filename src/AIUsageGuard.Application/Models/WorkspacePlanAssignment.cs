namespace AIUsageGuard.Application.Models;

public sealed class WorkspacePlanAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public Guid PlanDefinitionId { get; set; }

    public DateTimeOffset EffectiveFromCycleStartUtc { get; set; }

    public DateTimeOffset? EffectiveToCycleStartUtc { get; set; }

    public Guid? AssignedByUserId { get; set; }

    public string? ChangeReason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
