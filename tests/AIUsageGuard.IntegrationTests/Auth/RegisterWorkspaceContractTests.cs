using System.Net;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Auth;

public sealed class RegisterWorkspaceContractTests
{
    [Fact]
    public async Task Register_rejects_duplicate_email_with_problem_details()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.LogoutAsync();

        var response = await client.RegisterWorkspaceResponseAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Beta Workspace");

        var problem = await response.ReadProblemAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem.Status);
    }
}
