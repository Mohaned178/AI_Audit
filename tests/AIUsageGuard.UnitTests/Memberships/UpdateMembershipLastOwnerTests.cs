using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Memberships.UpdateMembership;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;

namespace AIUsageGuard.UnitTests.Memberships;

public sealed class UpdateMembershipLastOwnerTests
{
    [Fact]
    public async Task Update_rejects_demoting_last_owner()
    {
        var store = CreateStoreWithSingleOwner(out var workspace, out var ownerMembership);
        var service = CreateService(store);

        var exception = await Assert.ThrowsAsync<RequestFailureException>(() =>
            service.UpdateAsync(workspace.Id, ownerMembership.UserId, ownerMembership.Id, WorkspaceRole.Admin, null));

        Assert.Equal(409, exception.StatusCode);
    }

    private static UpdateMembershipService CreateService(IPlatformStore store)
    {
        IAuditService auditService = new AuditService(store);
        return new UpdateMembershipService(
            store,
            auditService,
            BillingTestFactory.CreatePlanAssignmentService(store),
            BillingTestFactory.CreateLimitEvaluator(store));
    }

    private static ApplicationDbContext CreateStoreWithSingleOwner(out Workspace workspace, out WorkspaceMembership ownerMembership)
    {
        var store = TestDbContextFactory.CreateContext();
        var owner = new UserAccount
        {
            Email = "owner@example.com",
            DisplayName = "Owner",
            PasswordHash = "hash"
        };

        workspace = new Workspace
        {
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = owner.Id
        };

        ownerMembership = new WorkspaceMembership
        {
            WorkspaceId = workspace.Id,
            UserId = owner.Id,
            Role = WorkspaceRole.Owner,
            Status = MembershipStatus.Active
        };

        store.AddUserAsync(owner).GetAwaiter().GetResult();
        store.AddWorkspaceAsync(workspace).GetAwaiter().GetResult();
        store.AddMembershipAsync(ownerMembership).GetAwaiter().GetResult();
        return store;
    }
}
