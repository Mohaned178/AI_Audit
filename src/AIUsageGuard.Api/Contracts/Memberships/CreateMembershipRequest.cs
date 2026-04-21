using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Api.Contracts.Memberships;

public sealed record CreateMembershipRequest(
    string UserEmail,
    WorkspaceRole Role);
