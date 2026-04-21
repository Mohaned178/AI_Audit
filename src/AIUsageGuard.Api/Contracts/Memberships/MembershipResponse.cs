using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Api.Contracts.Memberships;

public sealed record MembershipResponse(
    Guid MembershipId,
    Guid WorkspaceId,
    Guid UserId,
    string UserEmail,
    WorkspaceRole Role,
    MembershipStatus Status);
