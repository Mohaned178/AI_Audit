using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.Api.Contracts.Auth;
using AIUsageGuard.Api.Contracts.Memberships;
using AIUsageGuard.Api.Contracts.Workspaces;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.IntegrationTests.Infrastructure;

internal static class ApiTestClient
{
    public static async Task<WorkspaceSessionResponse> RegisterWorkspaceAsync(
        this HttpClient client,
        string email,
        string password,
        string displayName,
        string workspaceName)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterWorkspaceRequest(email, password, displayName, workspaceName));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Register failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<WorkspaceSessionResponse>())!;
    }

    public static async Task<HttpResponseMessage> RegisterWorkspaceResponseAsync(
        this HttpClient client,
        string email,
        string password,
        string displayName,
        string workspaceName)
    {
        return await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterWorkspaceRequest(email, password, displayName, workspaceName));
    }

    public static async Task<WorkspaceSessionResponse> LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Login failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<WorkspaceSessionResponse>())!;
    }

    public static async Task<WorkspaceContextResponse> GetWorkspaceContextAsync(this HttpClient client, Guid workspaceId)
    {
        var response = await client.GetAsync($"/workspaces/{workspaceId}/context");
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Workspace context failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<WorkspaceContextResponse>())!;
    }

    public static async Task<MembershipResponse> CreateMembershipAsync(
        this HttpClient client,
        Guid workspaceId,
        string userEmail,
        string role)
    {
        var response = await client.PostAsJsonAsync(
            $"/workspaces/{workspaceId}/memberships",
            new CreateMembershipRequest(userEmail, Enum.Parse<AIUsageGuard.Application.Models.WorkspaceRole>(role, true)));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Create membership failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<MembershipResponse>())!;
    }

    public static async Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
    }

    public static async Task LogoutAsync(this HttpClient client)
    {
        var response = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
