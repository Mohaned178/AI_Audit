namespace AIUsageGuard.Domain.Memberships;

public sealed class WorkspaceMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;

    public MembershipStatus Status { get; set; } = MembershipStatus.Active;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
