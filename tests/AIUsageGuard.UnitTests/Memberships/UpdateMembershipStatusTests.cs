using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Memberships.UpdateMembership;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;

namespace AIUsageGuard.UnitTests.Memberships;

public sealed class UpdateMembershipStatusTests
{
    [Fact]
    public async Task Update_allows_owner_to_deactivate_non_owner_member()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var owner = new UserAccount
        {
            Email = "owner@example.com",
            DisplayName = "Owner",
            PasswordHash = "hash"
        };

        var member = new UserAccount
        {
            Email = "member@example.com",
            DisplayName = "Member",
            PasswordHash = "hash"
        };

        var workspace = new Workspace
        {
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = owner.Id
        };

        var ownerMembership = new WorkspaceMembership
        {
            WorkspaceId = workspace.Id,
            UserId = owner.Id,
            Role = WorkspaceRole.Owner,
            Status = MembershipStatus.Active
        };

        var memberMembership = new WorkspaceMembership
        {
            WorkspaceId = workspace.Id,
            UserId = member.Id,
            Role = WorkspaceRole.Member,
            Status = MembershipStatus.Active
        };

        await store.AddUserAsync(owner);
        await store.AddUserAsync(member);
        await store.AddWorkspaceAsync(workspace);
        await store.AddMembershipAsync(ownerMembership);
        await store.AddMembershipAsync(memberMembership);

        IAuditService auditService = new AuditService(store);
        var service = new UpdateMembershipService(
            store,
            auditService,
            BillingTestFactory.CreatePlanAssignmentService(store),
            BillingTestFactory.CreateLimitEvaluator(store));

        var updated = await service.UpdateAsync(
            workspace.Id,
            owner.Id,
            memberMembership.Id,
            null,
            MembershipStatus.Inactive);

        Assert.Equal(MembershipStatus.Inactive, updated.Status);
    }
}
