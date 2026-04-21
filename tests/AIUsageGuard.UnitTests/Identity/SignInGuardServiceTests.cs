using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Identity;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Security;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;

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
}
