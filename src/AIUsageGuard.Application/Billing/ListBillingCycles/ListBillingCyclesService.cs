using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.Billing.ListBillingCycles;

public sealed class ListBillingCyclesService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ApplyWorkspacePlanAssignmentService _planAssignmentService;
    private readonly ILogger<ListBillingCyclesService> _logger;

    public ListBillingCyclesService(
        IPlatformStore store,
        IAuditService auditService,
        ApplyWorkspacePlanAssignmentService planAssignmentService,
        ILogger<ListBillingCyclesService> logger)
    {
        _store = store;
        _auditService = auditService;
        _planAssignmentService = planAssignmentService;
        _logger = logger;
    }

    public async Task<ListBillingCyclesResult> ListAsync(
        ListBillingCyclesQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            _ = await _store.FindWorkspaceByIdAsync(query.WorkspaceId, cancellationToken)
                ?? throw new RequestFailureException(404, "Workspace not found.");
            await _planAssignmentService.EnsureCurrentCycleAsync(query.WorkspaceId, query.RequestedByUserId, DateTimeOffset.UtcNow, cancellationToken);

            var cycles = await _store.ListUsageCyclesAsync(query.WorkspaceId, query.PageNumber, query.PageSize, cancellationToken);
            var summaries = new List<BillingCycleSummary>(cycles.Count);
            foreach (var cycle in cycles)
            {
                summaries.Add(await BuildSummaryAsync(cycle, cancellationToken));
            }

            var totalCount = await _store.CountUsageCyclesAsync(query.WorkspaceId, cancellationToken);
            await RecordAuditAsync(query, "success", $"Retrieved {summaries.Count} billing cycles.", cancellationToken);

            _logger.LogInformation(
                "Listed billing cycles for workspace {WorkspaceId} page {PageNumber} size {PageSize}.",
                query.WorkspaceId,
                query.PageNumber,
                query.PageSize);

            return new ListBillingCyclesResult(query.WorkspaceId, query.PageNumber, query.PageSize, totalCount, summaries);
        }
        catch (RequestFailureException exception)
        {
            await RecordAuditAsync(query, "failed", exception.Message, cancellationToken);
            _logger.LogWarning(exception, "Failed to list billing cycles for workspace {WorkspaceId}.", query.WorkspaceId);
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
        ListBillingCyclesQuery query,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = query.WorkspaceId,
            ActorUserId = query.RequestedByUserId,
            ActionType = "billing.cycle_history.read",
            TargetType = "billing_cycle_history",
            TargetId = query.WorkspaceId.ToString(),
            Result = result,
            Reason = reason
        }, cancellationToken);
    }

    private static void Validate(ListBillingCyclesQuery query)
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
            throw new RequestFailureException(400, "pageNumber must be at least 1.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new RequestFailureException(400, "pageSize must be between 1 and 100.");
        }
    }
}
