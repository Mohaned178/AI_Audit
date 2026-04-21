using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
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

    public static async Task<HttpResponseMessage> IngestAIUsageEventResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        IngestAIUsageEventRequest request)
    {
        return await client.PostAsJsonAsync($"/workspaces/{workspaceId}/events", request);
    }

    public static async Task<EventIngestionResponse> IngestAIUsageEventAsync(
        this HttpClient client,
        Guid workspaceId,
        IngestAIUsageEventRequest request)
    {
        var response = await client.IngestAIUsageEventResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Ingest AI usage event failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<EventIngestionResponse>())!;
    }

    public static async Task<HttpResponseMessage> ListAIUsageEventsResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        ListAIUsageEventsRequest request)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            query.Add($"eventType={Uri.EscapeDataString(request.EventType)}");
        }

        if (request.ActorUserId.HasValue)
        {
            query.Add($"actorUserId={request.ActorUserId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.ToolName))
        {
            query.Add($"toolName={Uri.EscapeDataString(request.ToolName)}");
        }

        if (request.FromOccurredAt.HasValue)
        {
            query.Add($"fromOccurredAt={Uri.EscapeDataString(request.FromOccurredAt.Value.ToString("O"))}");
        }

        if (request.ToOccurredAt.HasValue)
        {
            query.Add($"toOccurredAt={Uri.EscapeDataString(request.ToOccurredAt.Value.ToString("O"))}");
        }

        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");

        return await client.GetAsync($"/workspaces/{workspaceId}/events?{string.Join("&", query)}");
    }

    public static async Task<AIUsageEventHistoryResponse> ListAIUsageEventsAsync(
        this HttpClient client,
        Guid workspaceId,
        ListAIUsageEventsRequest request)
    {
        var response = await client.ListAIUsageEventsResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"List AI usage events failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<AIUsageEventHistoryResponse>())!;
    }

    public static async Task LogoutAsync(this HttpClient client)
    {
        var response = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
