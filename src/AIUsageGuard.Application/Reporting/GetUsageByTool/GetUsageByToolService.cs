using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Reporting;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.Reporting.GetUsageByTool;

public sealed class GetUsageByToolService
{
    private static readonly Meter ReportingMeter = new("AIUsageGuard.Reporting");
    private static readonly Counter<long> SuccessCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.usage_by_tool_reads");
    private static readonly Counter<long> EmptyCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.usage_by_tool_empty_results");
    private static readonly Counter<long> InvalidPeriodCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.usage_by_tool_invalid_requests");
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ILogger<GetUsageByToolService> _logger;

    public GetUsageByToolService(
        IPlatformStore store,
        IAuditService auditService,
        ILogger<GetUsageByToolService> logger)
    {
        _store = store;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetUsageByToolResult> GetAsync(GetUsageByToolQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            var period = ReportingPeriodValidator.ValidateAndNormalize(query.Period);
            ReportingPeriodValidator.ValidatePage(query.PageNumber, query.PageSize);
            var page = await _store.GetUsageSummaryByToolAsync(query.WorkspaceId, period, query.PageNumber, query.PageSize, cancellationToken);

            SuccessCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            if (page.TotalCount == 0)
            {
                EmptyCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            }

            _logger.LogInformation(
                "Retrieved usage-by-tool report for workspace {WorkspaceId} from {FromDate} to {ToDate} with {TotalCount} rows.",
                query.WorkspaceId,
                period.FromDate,
                period.ToDate,
                page.TotalCount);

            await RecordAuditAsync(query, "success", "Usage-by-tool report retrieved.", cancellationToken);
            return new GetUsageByToolResult(query.WorkspaceId, period, page);
        }
        catch (RequestFailureException exception)
        {
            if (exception.StatusCode == 400)
            {
                InvalidPeriodCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            }

            _logger.LogWarning(
                exception,
                "Usage-by-tool request failed for workspace {WorkspaceId} from {FromDate} to {ToDate}.",
                query.WorkspaceId,
                query.Period.FromDate,
                query.Period.ToDate);

            await RecordAuditAsync(query, "failed", exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task RecordAuditAsync(
        GetUsageByToolQuery query,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = query.WorkspaceId,
            ActorUserId = query.RequestedByUserId,
            ActionType = "report.usage_by_tool.read",
            TargetType = "usage_summary",
            TargetId = query.WorkspaceId.ToString(),
            Result = result,
            Reason = reason
        }, cancellationToken);
    }

    private static void Validate(GetUsageByToolQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }
    }
}
