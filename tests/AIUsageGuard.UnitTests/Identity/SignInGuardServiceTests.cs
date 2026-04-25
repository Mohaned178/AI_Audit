using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Identity;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Security;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.UnitTests.Identity;

public sealed class SignInGuardServiceTests
{
    [Fact]
    public async Task Validate_returns_highest_role_active_membership()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var user = new UserAccount
        {
            Email = "member@example.com",
            DisplayName = "Member",
            PasswordHash = PasswordHashing.HashPassword("Password123!")
        };

        var workspace = new Workspace
        {
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = user.Id
        };

        var membership = new WorkspaceMembership
        {
            WorkspaceId = workspace.Id,
            UserId = user.Id,
            Role = WorkspaceRole.Admin,
            Status = MembershipStatus.Active
        };

        await store.AddUserAsync(user);
        await store.AddWorkspaceAsync(workspace);
        await store.AddMembershipAsync(membership);

        var service = new SignInGuardService(store);
        var result = await service.ValidateAsync("member@example.com", "Password123!");

        Assert.Equal(WorkspaceRole.Admin, result.Membership.Role);
        Assert.Equal(workspace.Id, result.Workspace.Id);
    }

    [Fact]
    public async Task Validate_rejects_user_without_active_membership()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var user = new UserAccount
        {
            Email = "member@example.com",
            DisplayName = "Member",
            PasswordHash = PasswordHashing.HashPassword("Password123!")
        };

        await store.AddUserAsync(user);

        var service = new SignInGuardService(store);
        var exception = await Assert.ThrowsAsync<RequestFailureException>(() =>
            service.ValidateAsync("member@example.com", "Password123!"));

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task Validate_locks_account_after_repeated_failed_attempts()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var user = new UserAccount
        {
            Email = "member@example.com",
            DisplayName = "Member",
            PasswordHash = PasswordHashing.HashPassword("Password123!")
        };

        var workspace = new Workspace
        {
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = user.Id
        };

        await store.AddUserAsync(user);
        await store.AddWorkspaceAsync(workspace);
        await store.AddMembershipAsync(new WorkspaceMembership
        {
            WorkspaceId = workspace.Id,
            UserId = user.Id,
            Role = WorkspaceRole.Admin,
            Status = MembershipStatus.Active
        });

        var service = new SignInGuardService(
            store,
            Options.Create(new SignInHardeningOptions
            {
                MaxFailedAttempts = 2,
                FailureWindow = TimeSpan.FromMinutes(15),
                LockoutDuration = TimeSpan.FromMinutes(30)
            }));

        await Assert.ThrowsAsync<SignInHardeningException>(() => service.ValidateAsync("member@example.com", "WrongPassword!"));

        var lockout = await Assert.ThrowsAsync<SignInHardeningException>(() => service.ValidateAsync("member@example.com", "WrongPassword!"));

        Assert.True(lockout.LockedOut);
        Assert.Equal(423, lockout.StatusCode);

        var persisted = await store.FindUserByIdAsync(user.Id);
        Assert.NotNull(persisted?.LockedUntilUtc);
        Assert.Equal(0, persisted?.FailedSignInCount);

        var lockedRetry = await Assert.ThrowsAsync<SignInHardeningException>(() => service.ValidateAsync("member@example.com", "Password123!"));
        Assert.True(lockedRetry.LockedOut);
        Assert.Equal(423, lockedRetry.StatusCode);
    }
}
