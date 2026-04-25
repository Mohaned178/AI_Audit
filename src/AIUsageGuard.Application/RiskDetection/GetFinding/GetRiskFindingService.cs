using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.GetFinding;

public sealed class GetRiskFindingService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public GetRiskFindingService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<GetRiskFindingResult> GetAsync(GetRiskFindingQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            var finding = await _store.FindRiskFindingAsync(query.WorkspaceId, query.FindingId, cancellationToken)
                ?? throw new RequestFailureException(404, "Risk finding not found.");

            var eventRecord = await _store.FindAIUsageEventByIdAsync(query.WorkspaceId, finding.EventId, cancellationToken)
                ?? throw new RequestFailureException(404, "Triggering event not found.");

            var outcome = await _store.FindRiskEvaluationOutcomeByEventIdAsync(query.WorkspaceId, finding.EventId, cancellationToken)
                ?? throw new RequestFailureException(404, "Evaluation outcome not found.");

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "risk_finding.read",
                TargetType = "risk_finding",
                TargetId = finding.Id.ToString(),
                Result = "success",
                Reason = "Risk finding retrieved."
            }, cancellationToken);

            return new GetRiskFindingResult(finding, eventRecord, outcome);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "risk_finding.read",
                TargetType = "risk_finding",
                TargetId = query.FindingId.ToString(),
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private static void Validate(GetRiskFindingQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (query.FindingId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Finding is required.");
        }
    }
}
