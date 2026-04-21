using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Security;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Identity;

public sealed class SignInGuardService
{
    private readonly IPlatformStore _store;

    public SignInGuardService(IPlatformStore store)
    {
        _store = store;
    }

    public async Task<LoginResult> ValidateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _store.FindUserByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || user.Status != UserAccountStatus.Active)
        {
            throw new RequestFailureException(401, "Invalid credentials.");
        }

        if (!PasswordHashing.VerifyPassword(password, user.PasswordHash))
        {
            throw new RequestFailureException(401, "Invalid credentials.");
        }

        var memberships = await _store.ListMembershipsByUserAsync(user.Id, cancellationToken);
        var activeMemberships = memberships
            .Where(m => m.Status == MembershipStatus.Active)
            .OrderByDescending(m => m.Role)
            .ToList();

        if (activeMemberships.Count == 0)
        {
            throw new RequestFailureException(401, "User does not have an active workspace membership.");
        }

        var membership = activeMemberships[0];
        var workspace = await _store.FindWorkspaceByIdAsync(membership.WorkspaceId, cancellationToken)
            ?? throw new RequestFailureException(404, "Workspace not found.");

        if (workspace.Status != WorkspaceStatus.Active)
        {
            throw new RequestFailureException(403, "Workspace is not active.");
        }

        user.LastSignInAt = DateTimeOffset.UtcNow;
        await _store.UpdateUserAsync(user, cancellationToken);

        return new LoginResult(user, workspace, membership);
    }
}
