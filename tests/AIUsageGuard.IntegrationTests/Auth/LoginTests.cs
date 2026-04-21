using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Auth;

public sealed class LoginTests
{
    [Fact]
    public async Task Login_returns_workspace_session_for_existing_user()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var registered = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.LogoutAsync();

        var session = await client.LoginAsync("owner@example.com", "Password123!");

        Assert.Equal(registered.WorkspaceId, session.WorkspaceId);
        Assert.True(await factory.ExecuteDbContextAsync(dbContext => dbContext.AuditRecords.AnyAsync(audit => audit.ActionType == "auth.login")));
        Assert.True(await factory.ExecuteDbContextAsync(dbContext => dbContext.AuditRecords.AnyAsync(audit => audit.ActionType == "auth.logout")));
    }

    [Fact]
    public async Task Login_rejects_invalid_password()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.LogoutAsync();

        var response = await client.PostAsJsonAsync("/auth/login", new AIUsageGuard.Api.Contracts.Auth.LoginRequest("owner@example.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
