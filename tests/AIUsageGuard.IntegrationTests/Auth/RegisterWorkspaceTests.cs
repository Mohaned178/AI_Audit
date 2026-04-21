using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Auth;

public sealed class RegisterWorkspaceTests
{
    [Fact]
    public async Task Register_creates_workspace_owner_membership_and_audit_record()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var response = await client.RegisterWorkspaceResponseAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        var session = System.Text.Json.JsonSerializer.Deserialize<AIUsageGuard.Api.Contracts.Auth.WorkspaceSessionResponse>(body)!;
        Assert.Equal(1, await factory.ExecuteDbContextAsync(dbContext => dbContext.Users.CountAsync()));
        Assert.Equal(1, await factory.ExecuteDbContextAsync(dbContext => dbContext.Workspaces.CountAsync()));
        Assert.Equal(1, await factory.ExecuteDbContextAsync(dbContext => dbContext.WorkspaceMemberships.CountAsync()));
        Assert.True(await factory.ExecuteDbContextAsync(dbContext => dbContext.AuditRecords.AnyAsync(audit => audit.ActionType == "workspace.register")));
    }
}
