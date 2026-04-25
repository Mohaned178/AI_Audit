using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.Billing.GetPlanStatus;

public sealed class GetPlanStatusService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ApplyWorkspacePlanAssignmentService _planAssignmentService;
    private readonly ILogger<GetPlanStatusService> _logger;

    public GetPlanStatusService(
        IPlatformStore store,
        IAuditService auditService,
        ApplyWorkspacePlanAssignmentService planAssignmentService,
        ILogger<GetPlanStatusService> logger)
    {
        _store = store;
        _auditService = auditService;
        _planAssignmentService = planAssignmentService;
        _logger = logger;
    }

    public async Task<GetPlanStatusResult> GetAsync(
        GetPlanStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            _ = await _store.FindWorkspaceByIdAsync(query.WorkspaceId, cancellationToken)
                ?? throw new RequestFailureException(404, "Workspace not found.");

            var currentCycle = await _planAssignmentService.EnsureCurrentCycleAsync(
                query.WorkspaceId,
                query.RequestedByUserId,
                DateTimeOffset.UtcNow,
                cancellationToken);

            var assignment = await _store.FindPlanAssignmentByIdAsync(currentCycle.PlanAssignmentId, cancellationToken)
                ?? throw new RequestFailureException(404, "Active plan assignment was not found for the current billing cycle.");

            var plan = await _store.FindPlanDefinitionByIdAsync(assignment.PlanDefinitionId, cancellationToken)
                ?? throw new RequestFailureException(404, "Plan definition was not found for the active assignment.");

            var nextAssignment = await _store.FindNextPlanAssignmentAsync(query.WorkspaceId, currentCycle.CycleStartUtc, cancellationToken);
            PlanDefinition? nextPlan = null;
            if (nextAssignment is not null)
            {
                nextPlan = await _store.FindPlanDefinitionByIdAsync(nextAssignment.PlanDefinitionId, cancellationToken);
            }

            var metrics = await _store.ListUsageCycleMetricsAsync(currentCycle.Id, cancellationToken);
            var snapshot = new PlanStatusSnapshot
            {
                WorkspaceId = query.WorkspaceId,
                PlanCode = plan.PlanCode,
                PlanDisplayName = plan.DisplayName,
                CurrentCycleId = currentCycle.Id,
                CycleStartUtc = currentCycle.CycleStartUtc,
                CycleEndExclusiveUtc = currentCycle.CycleEndExclusiveUtc,
                NextPlanCode = nextPlan?.PlanCode,
                NextPlanStartsAtUtc = nextAssignment?.EffectiveFromCycleStartUtc,
                MetricStatuses = metrics
            };

            await RecordAuditAsync(query, "success", $"Retrieved plan status for plan {plan.PlanCode}.", cancellationToken);

            _logger.LogInformation(
                "Retrieved billing plan status for workspace {WorkspaceId} on plan {PlanCode} with {MetricCount} metrics.",
                query.WorkspaceId,
                plan.PlanCode,
                metrics.Count);

            return new GetPlanStatusResult(snapshot, currentCycle, assignment.EffectiveFromCycleStartUtc);
        }
        catch (RequestFailureException exception)
        {
            await RecordAuditAsync(query, "failed", exception.Message, cancellationToken);
            _logger.LogWarning(exception, "Failed to retrieve billing plan status for workspace {WorkspaceId}.", query.WorkspaceId);
            throw;
        }
    }

    private async Task RecordAuditAsync(
        GetPlanStatusQuery query,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = query.WorkspaceId,
            ActorUserId = query.RequestedByUserId,
            ActionType = "billing.plan_status.read",
            TargetType = "plan_status",
            TargetId = query.WorkspaceId.ToString(),
            Result = result,
            Reason = reason
        }, cancellationToken);
    }

    private static void Validate(GetPlanStatusQuery query)
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
