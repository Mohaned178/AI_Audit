using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Api.Contracts.Memberships;

public sealed record UpdateMembershipRequest(
    WorkspaceRole? Role,
    MembershipStatus? Status);
