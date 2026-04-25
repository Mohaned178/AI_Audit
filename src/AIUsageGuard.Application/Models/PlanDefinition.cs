namespace AIUsageGuard.Application.Models;

public sealed class PlanDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string PlanCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RetiredAtUtc { get; set; }
}
