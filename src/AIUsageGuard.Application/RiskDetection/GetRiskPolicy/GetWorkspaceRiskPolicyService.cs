using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.GetRiskPolicy;

public sealed class GetWorkspaceRiskPolicyService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public GetWorkspaceRiskPolicyService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<GetWorkspaceRiskPolicyResult> GetAsync(GetWorkspaceRiskPolicyQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            var policy = await _store.FindWorkspaceRiskPolicyAsync(query.WorkspaceId, cancellationToken)
                ?? new WorkspaceRiskPolicy { WorkspaceId = query.WorkspaceId };

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "risk_policy.read",
                TargetType = "risk_policy",
                TargetId = policy.Id == Guid.Empty ? query.WorkspaceId.ToString() : policy.Id.ToString(),
                Result = "success",
                Reason = "Workspace risk policy retrieved."
            }, cancellationToken);

            return new GetWorkspaceRiskPolicyResult(policy);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "risk_policy.read",
                TargetType = "risk_policy",
                TargetId = query.WorkspaceId.ToString(),
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private static void Validate(GetWorkspaceRiskPolicyQuery query)
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
