using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.UpdateRiskPolicy;

public sealed class UpdateWorkspaceRiskPolicyService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public UpdateWorkspaceRiskPolicyService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<UpdateWorkspaceRiskPolicyResult> UpdateAsync(UpdateWorkspaceRiskPolicyCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(command);

            var normalizedApprovedTools = (command.ApprovedTools ?? [])
                .Where(tool => !string.IsNullOrWhiteSpace(tool))
                .Select(tool => tool.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tool => tool, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (command.PerEventEstimatedCostThreshold is < 0 || command.DailyEstimatedCostThreshold is < 0)
            {
                throw new RequestFailureException(400, "Threshold values must be non-negative.");
            }

            var existing = await _store.FindWorkspaceRiskPolicyAsync(command.WorkspaceId, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            if (existing is null)
            {
                var created = new WorkspaceRiskPolicy
                {
                    WorkspaceId = command.WorkspaceId,
                    ApprovedTools = normalizedApprovedTools,
                    PerEventEstimatedCostThreshold = command.PerEventEstimatedCostThreshold,
                    DailyEstimatedCostThreshold = command.DailyEstimatedCostThreshold,
                    CreatedAt = now,
                    LastUpdatedAt = now,
                    LastUpdatedByUserId = command.RequestedByUserId
                };

                await _store.AddWorkspaceRiskPolicyAsync(created, cancellationToken);
                await RecordAuditAsync(command, created.Id, "success", "Workspace risk policy created.", cancellationToken);
                return new UpdateWorkspaceRiskPolicyResult(created, true);
            }

            existing.ApprovedTools = normalizedApprovedTools;
            existing.PerEventEstimatedCostThreshold = command.PerEventEstimatedCostThreshold;
            existing.DailyEstimatedCostThreshold = command.DailyEstimatedCostThreshold;
            existing.LastUpdatedAt = now;
            existing.LastUpdatedByUserId = command.RequestedByUserId;

            await _store.UpdateWorkspaceRiskPolicyAsync(existing, cancellationToken);
            await RecordAuditAsync(command, existing.Id, "success", "Workspace risk policy updated.", cancellationToken);
            return new UpdateWorkspaceRiskPolicyResult(existing, false);
        }
        catch (RequestFailureException exception)
        {
            await RecordAuditAsync(command, command.WorkspaceId, "failed", exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task RecordAuditAsync(UpdateWorkspaceRiskPolicyCommand command, Guid targetId, string result, string reason, CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = command.WorkspaceId,
            ActorUserId = command.RequestedByUserId,
            ActionType = "risk_policy.update",
            TargetType = "risk_policy",
            TargetId = targetId.ToString(),
            Result = result,
            Reason = reason
        }, cancellationToken);
    }

    private static void Validate(UpdateWorkspaceRiskPolicyCommand command)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (command.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }
    }
}
