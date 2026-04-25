using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.AuditLogs;
using AIUsageGuard.Api.Contracts.Auth;
using AIUsageGuard.Api.Contracts.Billing;
using AIUsageGuard.Api.Contracts.Memberships;
using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.Api.Contracts.Workspaces;
using AIUsageGuard.Application.Models;
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
        var response = await client.CreateMembershipResponseAsync(workspaceId, userEmail, role);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Create membership failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<MembershipResponse>())!;
    }

    public static Task<HttpResponseMessage> CreateMembershipResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        string userEmail,
        string role)
    {
        return client.PostAsJsonAsync(
            $"/workspaces/{workspaceId}/memberships",
            new CreateMembershipRequest(userEmail, Enum.Parse<AIUsageGuard.Application.Models.WorkspaceRole>(role, true)));
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

    public static Task<HttpResponseMessage> ListAuditLogsResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        string? actionType = null,
        string? result = null,
        Guid? actorUserId = null,
        bool? securityRelevant = null,
        DateTimeOffset? fromOccurredAt = null,
        DateTimeOffset? toOccurredAt = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query.Add($"actionType={Uri.EscapeDataString(actionType)}");
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            query.Add($"result={Uri.EscapeDataString(result)}");
        }

        if (actorUserId.HasValue)
        {
            query.Add($"actorUserId={actorUserId.Value}");
        }

        if (securityRelevant.HasValue)
        {
            query.Add($"securityRelevant={securityRelevant.Value.ToString().ToLowerInvariant()}");
        }

        if (fromOccurredAt.HasValue)
        {
            query.Add($"fromOccurredAt={Uri.EscapeDataString(fromOccurredAt.Value.ToString("O"))}");
        }

        if (toOccurredAt.HasValue)
        {
            query.Add($"toOccurredAt={Uri.EscapeDataString(toOccurredAt.Value.ToString("O"))}");
        }

        query.Add($"pageNumber={pageNumber}");
        query.Add($"pageSize={pageSize}");

        return client.GetAsync($"/workspaces/{workspaceId}/audit-logs?{string.Join("&", query)}");
    }

    public static async Task<AuditLogListResponse> ListAuditLogsAsync(
        this HttpClient client,
        Guid workspaceId,
        string? actionType = null,
        string? result = null,
        Guid? actorUserId = null,
        bool? securityRelevant = null,
        DateTimeOffset? fromOccurredAt = null,
        DateTimeOffset? toOccurredAt = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var response = await client.ListAuditLogsResponseAsync(
            workspaceId,
            actionType,
            result,
            actorUserId,
            securityRelevant,
            fromOccurredAt,
            toOccurredAt,
            pageNumber,
            pageSize);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"List audit logs failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<AuditLogListResponse>())!;
    }

    public static Task<HttpResponseMessage> GetAuditLogResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        Guid auditLogId)
    {
        return client.GetAsync($"/workspaces/{workspaceId}/audit-logs/{auditLogId}");
    }

    public static async Task<AuditLogDetailResponse> GetAuditLogAsync(
        this HttpClient client,
        Guid workspaceId,
        Guid auditLogId)
    {
        var response = await client.GetAuditLogResponseAsync(workspaceId, auditLogId);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get audit log failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<AuditLogDetailResponse>())!;
    }

    public static async Task<HttpResponseMessage> ListRiskFindingsResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        ListRiskFindingsRequest request)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.RuleType))
        {
            query.Add($"ruleType={Uri.EscapeDataString(request.RuleType)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Severity))
        {
            query.Add($"severity={Uri.EscapeDataString(request.Severity)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query.Add($"status={Uri.EscapeDataString(request.Status)}");
        }

        if (request.ActorUserId.HasValue)
        {
            query.Add($"actorUserId={request.ActorUserId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.ToolName))
        {
            query.Add($"toolName={Uri.EscapeDataString(request.ToolName)}");
        }

        if (request.FromDetectedAt.HasValue)
        {
            query.Add($"fromDetectedAt={Uri.EscapeDataString(request.FromDetectedAt.Value.ToString("O"))}");
        }

        if (request.ToDetectedAt.HasValue)
        {
            query.Add($"toDetectedAt={Uri.EscapeDataString(request.ToDetectedAt.Value.ToString("O"))}");
        }

        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");

        return await client.GetAsync($"/workspaces/{workspaceId}/risk-findings?{string.Join("&", query)}");
    }

    public static async Task<RiskFindingListResponse> ListRiskFindingsAsync(
        this HttpClient client,
        Guid workspaceId,
        ListRiskFindingsRequest request)
    {
        var response = await client.ListRiskFindingsResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"List risk findings failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<RiskFindingListResponse>())!;
    }

    public static async Task<HttpResponseMessage> GetRiskFindingResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        Guid findingId)
    {
        return await client.GetAsync($"/workspaces/{workspaceId}/risk-findings/{findingId}");
    }

    public static async Task<RiskFindingDetailResponse> GetRiskFindingAsync(
        this HttpClient client,
        Guid workspaceId,
        Guid findingId)
    {
        var response = await client.GetRiskFindingResponseAsync(workspaceId, findingId);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get risk finding failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<RiskFindingDetailResponse>())!;
    }

    public static async Task<HttpResponseMessage> GetRiskPolicyResponseAsync(
        this HttpClient client,
        Guid workspaceId)
    {
        return await client.GetAsync($"/workspaces/{workspaceId}/risk-policy");
    }

    public static async Task<WorkspaceRiskPolicyResponse> GetRiskPolicyAsync(
        this HttpClient client,
        Guid workspaceId)
    {
        var response = await client.GetRiskPolicyResponseAsync(workspaceId);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get risk policy failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<WorkspaceRiskPolicyResponse>())!;
    }

    public static async Task<HttpResponseMessage> UpdateRiskPolicyResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        UpdateWorkspaceRiskPolicyRequest request)
    {
        return await client.PutAsJsonAsync($"/workspaces/{workspaceId}/risk-policy", request);
    }

    public static async Task<WorkspaceRiskPolicyResponse> UpdateRiskPolicyAsync(
        this HttpClient client,
        Guid workspaceId,
        UpdateWorkspaceRiskPolicyRequest request)
    {
        var response = await client.UpdateRiskPolicyResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Update risk policy failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<WorkspaceRiskPolicyResponse>())!;
    }

    public static async Task LogoutAsync(this HttpClient client)
    {
        var response = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<HttpResponseMessage> GetDashboardResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        ReportingPeriodRequest request)
    {
        return await client.GetAsync(
            $"/workspaces/{workspaceId}/dashboard?fromDate={request.FromDate:yyyy-MM-dd}&toDate={request.ToDate:yyyy-MM-dd}");
    }

    public static async Task<DashboardResponse> GetDashboardAsync(
        this HttpClient client,
        Guid workspaceId,
        ReportingPeriodRequest request)
    {
        var response = await client.GetDashboardResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get dashboard failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<DashboardResponse>())!;
    }

    public static async Task<HttpResponseMessage> GetUsageByUserResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        PagedReportingRequest request)
    {
        return await client.GetAsync(
            $"/workspaces/{workspaceId}/reports/usage-by-user?fromDate={request.FromDate:yyyy-MM-dd}&toDate={request.ToDate:yyyy-MM-dd}&pageNumber={request.PageNumber}&pageSize={request.PageSize}");
    }

    public static async Task<UsageSummaryResponse> GetUsageByUserAsync(
        this HttpClient client,
        Guid workspaceId,
        PagedReportingRequest request)
    {
        var response = await client.GetUsageByUserResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get usage-by-user failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<UsageSummaryResponse>())!;
    }

    public static async Task<HttpResponseMessage> GetUsageByToolResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        PagedReportingRequest request)
    {
        return await client.GetAsync(
            $"/workspaces/{workspaceId}/reports/usage-by-tool?fromDate={request.FromDate:yyyy-MM-dd}&toDate={request.ToDate:yyyy-MM-dd}&pageNumber={request.PageNumber}&pageSize={request.PageSize}");
    }

    public static async Task<UsageSummaryResponse> GetUsageByToolAsync(
        this HttpClient client,
        Guid workspaceId,
        PagedReportingRequest request)
    {
        var response = await client.GetUsageByToolResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get usage-by-tool failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<UsageSummaryResponse>())!;
    }

    public static async Task<HttpResponseMessage> GetAlertsSummaryResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        ReportingPeriodRequest request)
    {
        return await client.GetAsync(
            $"/workspaces/{workspaceId}/reports/alerts-summary?fromDate={request.FromDate:yyyy-MM-dd}&toDate={request.ToDate:yyyy-MM-dd}");
    }

    public static async Task<AlertsSummaryResponse> GetAlertsSummaryAsync(
        this HttpClient client,
        Guid workspaceId,
        ReportingPeriodRequest request)
    {
        var response = await client.GetAlertsSummaryResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get alerts summary failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<AlertsSummaryResponse>())!;
    }

    public static async Task<HttpResponseMessage> GetCostSummaryResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        ReportingPeriodRequest request)
    {
        return await client.GetAsync(
            $"/workspaces/{workspaceId}/reports/cost-summary?fromDate={request.FromDate:yyyy-MM-dd}&toDate={request.ToDate:yyyy-MM-dd}");
    }

    public static async Task<CostSummaryResponse> GetCostSummaryAsync(
        this HttpClient client,
        Guid workspaceId,
        ReportingPeriodRequest request)
    {
        var response = await client.GetCostSummaryResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get cost summary failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<CostSummaryResponse>())!;
    }

    public static async Task<NotificationPreferenceResponse> GetNotificationPreferencesAsync(
        this HttpClient client,
        Guid workspaceId)
    {
        var response = await client.GetAsync($"/workspaces/{workspaceId}/notification-preferences");
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get notification preferences failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<NotificationPreferenceResponse>())!;
    }

    public static Task<HttpResponseMessage> UpdateNotificationPreferencesResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        UpdateNotificationPreferenceRequest request)
    {
        return client.PutAsJsonAsync($"/workspaces/{workspaceId}/notification-preferences", request);
    }

    public static async Task<NotificationPreferenceResponse> UpdateNotificationPreferencesAsync(
        this HttpClient client,
        Guid workspaceId,
        UpdateNotificationPreferenceRequest request)
    {
        var response = await client.UpdateNotificationPreferencesResponseAsync(workspaceId, request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Update notification preferences failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<NotificationPreferenceResponse>())!;
    }

    public static async Task<NotificationListResponse> ListNotificationsAsync(
        this HttpClient client,
        Guid workspaceId,
        string? type = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(type))
        {
            query.Add($"type={Uri.EscapeDataString(type)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var response = await client.GetAsync($"/workspaces/{workspaceId}/notifications?{string.Join("&", query)}");
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"List notifications failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<NotificationListResponse>())!;
    }

    public static async Task<NotificationDetailResponse> GetNotificationAsync(
        this HttpClient client,
        Guid workspaceId,
        Guid notificationId)
    {
        var response = await client.GetAsync($"/workspaces/{workspaceId}/notifications/{notificationId}");
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get notification failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<NotificationDetailResponse>())!;
    }

    public static Task<HttpResponseMessage> GetPlanStatusResponseAsync(
        this HttpClient client,
        Guid workspaceId)
    {
        return client.GetAsync($"/workspaces/{workspaceId}/billing/plan-status");
    }

    public static async Task<PlanStatusResponse> GetPlanStatusAsync(
        this HttpClient client,
        Guid workspaceId)
    {
        var response = await client.GetPlanStatusResponseAsync(workspaceId);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get plan status failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<PlanStatusResponse>())!;
    }

    public static Task<HttpResponseMessage> ListBillingCyclesResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        int pageNumber = 1,
        int pageSize = 20)
    {
        return client.GetAsync($"/workspaces/{workspaceId}/billing/cycles?pageNumber={pageNumber}&pageSize={pageSize}");
    }

    public static async Task<BillingCycleListResponse> ListBillingCyclesAsync(
        this HttpClient client,
        Guid workspaceId,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var response = await client.ListBillingCyclesResponseAsync(workspaceId, pageNumber, pageSize);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"List billing cycles failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<BillingCycleListResponse>())!;
    }

    public static Task<HttpResponseMessage> GetBillingCycleResponseAsync(
        this HttpClient client,
        Guid workspaceId,
        Guid cycleId)
    {
        return client.GetAsync($"/workspaces/{workspaceId}/billing/cycles/{cycleId}");
    }

    public static async Task<BillingCycleDetailResponse> GetBillingCycleAsync(
        this HttpClient client,
        Guid workspaceId,
        Guid cycleId)
    {
        var response = await client.GetBillingCycleResponseAsync(workspaceId, cycleId);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Get billing cycle failed with {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<BillingCycleDetailResponse>())!;
    }
}
