using System.Net;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class GetUsageReportsContractTests
{
    [Fact]
    public async Task Usage_by_user_rejects_invalid_page_size_with_problem_details()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.GetUsageByUserResponseAsync(
            session.WorkspaceId,
            new PagedReportingRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7), 1, 101));

        var problem = await response.ReadProblemAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem.Status);
        Assert.Contains("pageSize", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Usage_by_tool_returns_grouping_and_page_contract_fields()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();
        using var outsiderClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        var member = await memberClient.RegisterWorkspaceAsync("member@example.com", "Password123!", "Member", "Beta Workspace");
        var outsider = await outsiderClient.RegisterWorkspaceAsync("outsider@example.com", "Password123!", "Outsider", "Gamma Workspace");
        await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "member@example.com", "Member");
        await ReportingTestData.SeedScenarioAsync(factory, owner, member, outsider);

        var response = await ownerClient.GetUsageByToolAsync(
            owner.WorkspaceId,
            new PagedReportingRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7), 1, 20));

        Assert.Equal(owner.WorkspaceId, response.WorkspaceId);
        Assert.Equal("tool", response.GroupedBy);
        Assert.True(response.Page.PageNumber >= 1);
        Assert.NotEmpty(response.Page.Items);
    }
}
