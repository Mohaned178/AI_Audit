using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.Api.Contracts.Auth;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Security;

public sealed class LoginHardeningTests
{
    [Fact]
    public async Task Repeated_invalid_signins_trigger_temporary_lockout()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.LogoutAsync();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("owner@example.com", "WrongPassword!"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var locked = await client.PostAsJsonAsync("/auth/login", new LoginRequest("owner@example.com", "WrongPassword!"));
        Assert.Equal((HttpStatusCode)423, locked.StatusCode);

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var user = await dbContext.Users.SingleAsync(item => item.Email == "owner@example.com");
            Assert.NotNull(user.LockedUntilUtc);
            Assert.Equal(0, user.FailedSignInCount);
            Assert.True(await dbContext.AuditRecords.AnyAsync(item => item.ActionType == "auth.login" && item.Result == "failed"));
        });
    }
}
