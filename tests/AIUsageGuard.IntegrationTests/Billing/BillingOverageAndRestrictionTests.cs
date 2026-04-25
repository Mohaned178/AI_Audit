using System.Net;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class BillingOverageAndRestrictionTests
{
    [Fact]
    public async Task Restriction_attempt_records_denied_audit()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            for (var index = 0; index < 10; index++)
            {
                await dbContext.AddUserAsync(new AIUsageGuard.Application.Models.UserAccount
                {
                    Email = $"blocked{index}@example.com",
                    DisplayName = $"Blocked {index}",
                    PasswordHash = "hash"
                });
            }
        });

        for (var index = 0; index < 9; index++)
        {
            await ownerClient.CreateMembershipAsync(owner.WorkspaceId, $"blocked{index}@example.com", "Member");
        }

        var response = await ownerClient.CreateMembershipResponseAsync(owner.WorkspaceId, "blocked9@example.com", "Member");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.AuditRecords.AnyAsync(item => item.ActionType == "membership.create" && item.Result == "denied")));
    }
}
