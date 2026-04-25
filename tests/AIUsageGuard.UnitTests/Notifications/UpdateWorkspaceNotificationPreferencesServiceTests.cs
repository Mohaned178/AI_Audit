using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications.ConfigureWorkspaceNotifications;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.Notifications;

public sealed class UpdateWorkspaceNotificationPreferencesServiceTests
{
    [Fact]
    public async Task Update_rejects_non_admin_selected_recipient()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var setup = await SeedWorkspaceAsync(dbContext);
        var service = new UpdateWorkspaceNotificationPreferencesService(dbContext, new AuditService(dbContext));

        var exception = await Assert.ThrowsAsync<RequestFailureException>(() => service.UpdateAsync(
            new UpdateWorkspaceNotificationPreferencesCommand(
                setup.WorkspaceId,
                setup.OwnerUserId,
                true,
                true,
                DigestCadence.Daily,
                RecipientSelectionMode.SelectedRecipients,
                [setup.MemberUserId])));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task Update_persists_valid_selected_admins()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var setup = await SeedWorkspaceAsync(dbContext);
        var service = new UpdateWorkspaceNotificationPreferencesService(dbContext, new AuditService(dbContext));

        var result = await service.UpdateAsync(new UpdateWorkspaceNotificationPreferencesCommand(
            setup.WorkspaceId,
            setup.OwnerUserId,
            true,
            true,
            DigestCadence.Weekly,
            RecipientSelectionMode.SelectedRecipients,
            [setup.OwnerUserId, setup.AdminUserId]));

        Assert.True(result.Preference.DigestEnabled);
        Assert.Equal(2, result.Preference.SelectedRecipientUserIds.Count);
        Assert.Equal(1, await dbContext.NotificationPreferences.CountAsync());
    }

    private static async Task<(Guid WorkspaceId, Guid OwnerUserId, Guid AdminUserId, Guid MemberUserId)> SeedWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var workspaceId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();

        await dbContext.AddUserAsync(new UserAccount
        {
            Id = ownerUserId,
            Email = "owner@example.com",
            DisplayName = "Owner",
            PasswordHash = "hash",
            Status = UserAccountStatus.Active
        });
        await dbContext.AddUserAsync(new UserAccount
        {
            Id = adminUserId,
            Email = "admin@example.com",
            DisplayName = "Admin",
            PasswordHash = "hash",
            Status = UserAccountStatus.Active
        });
        await dbContext.AddUserAsync(new UserAccount
        {
            Id = memberUserId,
            Email = "member@example.com",
            DisplayName = "Member",
            PasswordHash = "hash",
            Status = UserAccountStatus.Active
        });

        dbContext.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = ownerUserId
        });
        dbContext.WorkspaceMemberships.AddRange(
            new WorkspaceMembership
            {
                WorkspaceId = workspaceId,
                UserId = ownerUserId,
                Role = WorkspaceRole.Owner,
                Status = MembershipStatus.Active
            },
            new WorkspaceMembership
            {
                WorkspaceId = workspaceId,
                UserId = adminUserId,
                Role = WorkspaceRole.Admin,
                Status = MembershipStatus.Active
            },
            new WorkspaceMembership
            {
                WorkspaceId = workspaceId,
                UserId = memberUserId,
                Role = WorkspaceRole.Member,
                Status = MembershipStatus.Active
            });
        await dbContext.SaveChangesAsync();

        return (workspaceId, ownerUserId, adminUserId, memberUserId);
    }
}
