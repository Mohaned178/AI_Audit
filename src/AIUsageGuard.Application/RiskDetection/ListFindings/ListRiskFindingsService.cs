using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.ListFindings;

public sealed class ListRiskFindingsService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public ListRiskFindingsService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<ListRiskFindingsResult> ListAsync(ListRiskFindingsQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            var totalCount = await _store.CountRiskFindingsAsync(
                query.WorkspaceId,
                query.RuleType,
                query.Severity,
                query.Status,
                query.ActorUserId,
                query.ToolName,
                query.FromDetectedAt,
                query.ToDetectedAt,
                cancellationToken);

            var items = await _store.ListRiskFindingsAsync(
                query.WorkspaceId,
                query.RuleType,
                query.Severity,
                query.Status,
                query.ActorUserId,
                query.ToolName,
                query.FromDetectedAt,
                query.ToDetectedAt,
                query.PageNumber,
                query.PageSize,
                cancellationToken);

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "risk_finding.read",
                TargetType = "risk_finding_list",
                TargetId = null,
                Result = "success",
                Reason = "Risk findings retrieved."
            }, cancellationToken);

            return new ListRiskFindingsResult(items, query.PageNumber, query.PageSize, totalCount);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "risk_finding.read",
                TargetType = "risk_finding_list",
                TargetId = null,
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private static void Validate(ListRiskFindingsQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (query.PageNumber < 1)
        {
            throw new RequestFailureException(400, "Page number must be at least 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 200)
        {
            throw new RequestFailureException(400, "Page size must be between 1 and 200.");
        }

        if (query.FromDetectedAt.HasValue && query.ToDetectedAt.HasValue && query.FromDetectedAt > query.ToDetectedAt)
        {
            throw new RequestFailureException(400, "The start date must be earlier than or equal to the end date.");
        }
    }
}
