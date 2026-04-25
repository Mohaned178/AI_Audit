using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Security;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Application.Identity;

public sealed class SignInGuardService
{
    private static readonly Meter Meter = new("AIUsageGuard.Security");
    private static readonly Counter<long> ValidationAttemptCounter = Meter.CreateCounter<long>("ai_usage_guard.signin.attempts");
    private static readonly Counter<long> FailedAttemptCounter = Meter.CreateCounter<long>("ai_usage_guard.signin.failed_attempts");
    private static readonly Counter<long> LockoutCounter = Meter.CreateCounter<long>("ai_usage_guard.signin.lockouts");
    private static readonly Counter<long> SuccessCounter = Meter.CreateCounter<long>("ai_usage_guard.signin.successes");

    private readonly IPlatformStore _store;
    private readonly SignInHardeningOptions _options;
    private readonly ILogger<SignInGuardService> _logger;

    public SignInGuardService(IPlatformStore store)
        : this(store, Options.Create(new SignInHardeningOptions()), NullLogger<SignInGuardService>.Instance)
    {
    }

    public SignInGuardService(IPlatformStore store, IOptions<SignInHardeningOptions> options)
        : this(store, options, NullLogger<SignInGuardService>.Instance)
    {
    }

    public SignInGuardService(
        IPlatformStore store,
        IOptions<SignInHardeningOptions> options,
        ILogger<SignInGuardService> logger)
        : this(store, options.Value, logger)
    {
    }

    private SignInGuardService(IPlatformStore store, SignInHardeningOptions options, ILogger<SignInGuardService> logger)
    {
        _store = store;
        _options = options;
        _logger = logger;
    }

    public async Task<LoginResult> ValidateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        ValidationAttemptCounter.Add(1, new KeyValuePair<string, object?>("sign_in.email", normalizedEmail));
        var now = DateTimeOffset.UtcNow;
        var user = await _store.FindUserByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || user.Status != UserAccountStatus.Active)
        {
            FailedAttemptCounter.Add(1, new KeyValuePair<string, object?>("sign_in.email", normalizedEmail));
            _logger.LogWarning(
                "Rejected sign-in for {Email}: invalid credentials or inactive account.",
                normalizedEmail);
            throw new RequestFailureException(401, "Invalid credentials.");
        }

        var memberships = await _store.ListMembershipsByUserAsync(user.Id, cancellationToken);
        var activeMemberships = memberships
            .Where(m => m.Status == MembershipStatus.Active)
            .OrderByDescending(m => m.Role)
            .ToList();

        var workspaceId = activeMemberships.Count > 0 ? activeMemberships[0].WorkspaceId : (Guid?)null;

        if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc > now)
        {
            LockoutCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", workspaceId?.ToString()));
            _logger.LogWarning(
                "Rejected sign-in for user {UserId} in workspace {WorkspaceId}: account is temporarily locked.",
                user.Id,
                workspaceId);
            throw new SignInHardeningException(
                423,
                "Account is temporarily locked. Try again later.",
                workspaceId,
                user.Id,
                lockedOut: true);
        }

        if (!PasswordHashing.VerifyPassword(password, user.PasswordHash))
        {
            if (user.LastFailedSignInAt.HasValue && now - user.LastFailedSignInAt.Value > _options.FailureWindow)
            {
                user.FailedSignInCount = 0;
            }

            user.FailedSignInCount++;
            user.LastFailedSignInAt = now;

            var lockedOut = user.FailedSignInCount >= _options.MaxFailedAttempts;
            if (lockedOut)
            {
                user.LockedUntilUtc = now.Add(_options.LockoutDuration);
                user.FailedSignInCount = 0;
                LockoutCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", workspaceId?.ToString()));
            }

            await _store.UpdateUserLockoutAsync(user, cancellationToken);
            FailedAttemptCounter.Add(1, new KeyValuePair<string, object?>("sign_in.email", normalizedEmail));
            _logger.LogWarning(
                "Rejected sign-in for user {UserId} in workspace {WorkspaceId}: invalid credentials{LockoutSuffix}.",
                user.Id,
                workspaceId,
                lockedOut ? " and lockout was applied" : string.Empty);

            throw new SignInHardeningException(
                lockedOut ? 423 : 401,
                lockedOut ? "Account is temporarily locked. Try again later." : "Invalid credentials.",
                workspaceId,
                user.Id,
                lockedOut);
        }

        if (activeMemberships.Count == 0)
        {
            FailedAttemptCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", workspaceId?.ToString()));
            _logger.LogWarning(
                "Rejected sign-in for user {UserId}: no active workspace membership was found.",
                user.Id);
            throw new RequestFailureException(401, "User does not have an active workspace membership.");
        }

        var membership = activeMemberships[0];
        var workspace = await _store.FindWorkspaceByIdAsync(membership.WorkspaceId, cancellationToken)
            ?? throw new RequestFailureException(404, "Workspace not found.");

        if (workspace.Status != WorkspaceStatus.Active)
        {
            throw new RequestFailureException(403, "Workspace is not active.");
        }

        user.FailedSignInCount = 0;
        user.LastFailedSignInAt = null;
        user.LockedUntilUtc = null;
        user.LastSignInAt = DateTimeOffset.UtcNow;
        await _store.UpdateUserAsync(user, cancellationToken);
        SuccessCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", workspace.Id.ToString()));
        _logger.LogInformation(
            "Accepted sign-in for user {UserId} in workspace {WorkspaceId}.",
            user.Id,
            workspace.Id);

        return new LoginResult(user, workspace, membership);
    }
}
