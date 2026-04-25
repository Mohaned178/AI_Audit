using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Billing.ListBillingCycles;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.Billing.GetBillingCycle;

public sealed class GetBillingCycleService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ILogger<GetBillingCycleService> _logger;

    public GetBillingCycleService(
        IPlatformStore store,
        IAuditService auditService,
        ILogger<GetBillingCycleService> logger)
    {
        _store = store;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetBillingCycleResult> GetAsync(
        GetBillingCycleQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            _ = await _store.FindWorkspaceByIdAsync(query.WorkspaceId, cancellationToken)
                ?? throw new RequestFailureException(404, "Workspace not found.");

            var cycle = await _store.FindUsageCycleAsync(query.WorkspaceId, query.UsageCycleId, cancellationToken)
                ?? throw new RequestFailureException(404, $"Billing cycle {query.UsageCycleId} was not found for the requested workspace.");

            var summary = await BuildSummaryAsync(cycle, cancellationToken);
            var limitEvents = await _store.ListLimitEventsAsync(cycle.Id, cancellationToken);
            var adjustments = await _store.ListCycleAdjustmentsAsync(cycle.Id, cancellationToken);

            await RecordAuditAsync(query, "success", $"Retrieved billing cycle {cycle.Id}.", cancellationToken);
            _logger.LogInformation("Retrieved billing cycle {UsageCycleId} for workspace {WorkspaceId}.", cycle.Id, query.WorkspaceId);

            return new GetBillingCycleResult(query.WorkspaceId, summary, limitEvents, adjustments);
        }
        catch (RequestFailureException exception)
        {
            await RecordAuditAsync(query, "failed", exception.Message, cancellationToken);
            _logger.LogWarning(exception, "Failed to retrieve billing cycle {UsageCycleId} for workspace {WorkspaceId}.", query.UsageCycleId, query.WorkspaceId);
            throw;
        }
    }

    private async Task<BillingCycleSummary> BuildSummaryAsync(UsageCycle cycle, CancellationToken cancellationToken)
    {
        var assignment = await _store.FindPlanAssignmentByIdAsync(cycle.PlanAssignmentId, cancellationToken)
            ?? throw new RequestFailureException(404, "Billing cycle plan assignment was not found.");
        var plan = await _store.FindPlanDefinitionByIdAsync(assignment.PlanDefinitionId, cancellationToken)
            ?? throw new RequestFailureException(404, "Billing cycle plan definition was not found.");
        var metrics = await _store.ListUsageCycleMetricsAsync(cycle.Id, cancellationToken);
        var limitEvents = await _store.ListLimitEventsAsync(cycle.Id, cancellationToken);

        return new BillingCycleSummary
        {
            UsageCycleId = cycle.Id,
            WorkspaceId = cycle.WorkspaceId,
            PlanCode = plan.PlanCode,
            PlanDisplayName = plan.DisplayName,
            CycleStartUtc = cycle.CycleStartUtc,
            CycleEndExclusiveUtc = cycle.CycleEndExclusiveUtc,
            Status = cycle.Status,
            Metrics = metrics,
            WarningEventCount = limitEvents.Count(item => item.EventType == LimitEventType.WarningRaised),
            RestrictionEventCount = limitEvents.Count(item => item.EventType == LimitEventType.RestrictionApplied),
            AdjustmentCount = cycle.AdjustmentCount
        };
    }

    private async Task RecordAuditAsync(
        GetBillingCycleQuery query,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = query.WorkspaceId,
            ActorUserId = query.RequestedByUserId,
            ActionType = "billing.cycle.read",
            TargetType = "billing_cycle",
            TargetId = query.UsageCycleId.ToString(),
            Result = result,
            Reason = reason
        }, cancellationToken);
    }

    private static void Validate(GetBillingCycleQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (query.UsageCycleId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Billing cycle is required.");
        }
    }
}
