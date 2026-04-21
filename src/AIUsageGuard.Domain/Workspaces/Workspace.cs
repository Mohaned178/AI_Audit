namespace AIUsageGuard.Domain.Workspaces;

public sealed class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public WorkspaceStatus Status { get; set; } = WorkspaceStatus.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid CreatedByUserId { get; set; }
}
