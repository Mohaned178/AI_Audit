using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Reporting;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.Reporting.GetAlertsSummary;

public sealed class GetAlertsSummaryService
{
    private static readonly Meter ReportingMeter = new("AIUsageGuard.Reporting");
    private static readonly Counter<long> SuccessCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.alerts_summary_reads");
    private static readonly Counter<long> EmptyCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.alerts_summary_empty_results");
    private static readonly Counter<long> InvalidPeriodCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.alerts_summary_invalid_requests");
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ILogger<GetAlertsSummaryService> _logger;

    public GetAlertsSummaryService(
        IPlatformStore store,
        IAuditService auditService,
        ILogger<GetAlertsSummaryService> logger)
    {
        _store = store;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetAlertsSummaryResult> GetAsync(GetAlertsSummaryQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            var period = ReportingPeriodValidator.ValidateAndNormalize(query.Period);
            var summary = await _store.GetAlertsSummaryAsync(query.WorkspaceId, period, cancellationToken);

            SuccessCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            if (summary.TotalFindings == 0)
            {
                EmptyCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            }

            _logger.LogInformation(
                "Retrieved alerts summary for workspace {WorkspaceId} from {FromDate} to {ToDate} with {TotalFindings} findings.",
                query.WorkspaceId,
                period.FromDate,
                period.ToDate,
                summary.TotalFindings);

            await RecordAuditAsync(query, "success", "Alerts summary retrieved.", cancellationToken);
            return new GetAlertsSummaryResult(query.WorkspaceId, period, summary);
        }
        catch (RequestFailureException exception)
        {
            if (exception.StatusCode == 400)
            {
                InvalidPeriodCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            }

            _logger.LogWarning(
                exception,
                "Alerts summary request failed for workspace {WorkspaceId} from {FromDate} to {ToDate}.",
                query.WorkspaceId,
                query.Period.FromDate,
                query.Period.ToDate);

            await RecordAuditAsync(query, "failed", exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task RecordAuditAsync(
        GetAlertsSummaryQuery query,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = query.WorkspaceId,
            ActorUserId = query.RequestedByUserId,
            ActionType = "report.alerts_summary.read",
            TargetType = "alerts_summary",
            TargetId = query.WorkspaceId.ToString(),
            Result = result,
            Reason = reason
        }, cancellationToken);
    }

    private static void Validate(GetAlertsSummaryQuery query)
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
