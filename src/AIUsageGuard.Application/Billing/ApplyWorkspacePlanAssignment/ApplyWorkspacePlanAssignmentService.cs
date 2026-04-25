using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;

public sealed class ApplyWorkspacePlanAssignmentService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly BillingDimensionCounter _dimensionCounter;
    private readonly BillingLimitEvaluator _limitEvaluator;
    private readonly ILogger<ApplyWorkspacePlanAssignmentService> _logger;

    public ApplyWorkspacePlanAssignmentService(
        IPlatformStore store,
        IAuditService auditService,
        BillingDimensionCounter dimensionCounter,
        BillingLimitEvaluator limitEvaluator,
        ILogger<ApplyWorkspacePlanAssignmentService> logger)
    {
        _store = store;
        _auditService = auditService;
        _dimensionCounter = dimensionCounter;
        _limitEvaluator = limitEvaluator;
        _logger = logger;
    }

    public async Task<ApplyWorkspacePlanAssignmentResult> ApplyAsync(
        ApplyWorkspacePlanAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateApply(command);

        _ = await _store.FindWorkspaceByIdAsync(command.WorkspaceId, cancellationToken)
            ?? throw new RequestFailureException(404, "Workspace not found.");

        var plan = await _store.FindPlanDefinitionByCodeAsync(command.PlanCode, cancellationToken)
            ?? throw new RequestFailureException(404, $"Plan '{command.PlanCode}' was not found.");

        if (!plan.IsActive)
        {
            throw new RequestFailureException(409, $"Plan '{command.PlanCode}' is not active.");
        }

        var assignments = await _store.ListPlanAssignmentsAsync(command.WorkspaceId, cancellationToken);
        if (assignments.Any(item => item.EffectiveFromCycleStartUtc == command.EffectiveFromCycleStartUtc))
        {
            throw new RequestFailureException(409, "A plan assignment already exists for the requested cycle start.");
        }

        var previousAssignment = assignments
            .Where(item => item.EffectiveFromCycleStartUtc < command.EffectiveFromCycleStartUtc)
            .OrderByDescending(item => item.EffectiveFromCycleStartUtc)
            .FirstOrDefault();

        if (previousAssignment is not null &&
            (!previousAssignment.EffectiveToCycleStartUtc.HasValue ||
             previousAssignment.EffectiveToCycleStartUtc > command.EffectiveFromCycleStartUtc))
        {
            previousAssignment.EffectiveToCycleStartUtc = command.EffectiveFromCycleStartUtc;
            await _store.UpdatePlanAssignmentAsync(previousAssignment, cancellationToken);
        }

        var assignment = new WorkspacePlanAssignment
        {
            WorkspaceId = command.WorkspaceId,
            PlanDefinitionId = plan.Id,
            EffectiveFromCycleStartUtc = command.EffectiveFromCycleStartUtc,
            AssignedByUserId = command.RequestedByUserId,
            ChangeReason = command.ChangeReason
        };

        await _store.AddPlanAssignmentAsync(assignment, cancellationToken);
        await RecordAssignmentAuditAsync(
            command.WorkspaceId,
            command.RequestedByUserId,
            assignment.Id,
            $"Plan '{plan.PlanCode}' assigned effective {assignment.EffectiveFromCycleStartUtc:O}.",
            cancellationToken);

        var currentCycleStart = GetCycleStart(DateTimeOffset.UtcNow);
        UsageCycle? openedCycle = null;
        if (assignment.EffectiveFromCycleStartUtc <= currentCycleStart)
        {
            openedCycle = await EnsureCycleForMonthAsync(
                command.WorkspaceId,
                command.RequestedByUserId,
                assignment.EffectiveFromCycleStartUtc,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        _logger.LogInformation(
            "Applied billing plan {PlanCode} to workspace {WorkspaceId} effective {CycleStartUtc}.",
            plan.PlanCode,
            command.WorkspaceId,
            assignment.EffectiveFromCycleStartUtc);

        return new ApplyWorkspacePlanAssignmentResult(
            assignment,
            openedCycle,
            assignment.EffectiveFromCycleStartUtc > currentCycleStart);
    }

    public Task<UsageCycle> EnsureCurrentCycleAsync(
        Guid workspaceId,
        Guid? requestedByUserId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
        => EnsureCycleForMonthAsync(workspaceId, requestedByUserId, GetCycleStart(asOfUtc), asOfUtc, cancellationToken);

    public async Task RecalculateMetricsAsync(
        Guid workspaceId,
        UsageCycle cycle,
        IReadOnlyList<UsageCycleMetric>? metrics,
        DateTimeOffset observedAtUtc,
        string sourceType,
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        metrics ??= await _store.ListUsageCycleMetricsAsync(cycle.Id, cancellationToken);

        foreach (var metric in metrics)
        {
            var quantity = await _dimensionCounter.CountAsync(
                workspaceId,
                metric.Dimension,
                cycle.CycleStartUtc,
                cycle.CycleEndExclusiveUtc,
                cancellationToken);

            await _limitEvaluator.ApplyAsync(
                workspaceId,
                metric,
                quantity,
                observedAtUtc,
                sourceType,
                sourceId,
                cancellationToken);
        }

        cycle.LastCalculatedAtUtc = observedAtUtc;
        await _store.UpdateUsageCycleAsync(cycle, cancellationToken);
    }

    private async Task<UsageCycle> EnsureCycleForMonthAsync(
        Guid workspaceId,
        Guid? requestedByUserId,
        DateTimeOffset cycleStartUtc,
        DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken)
    {
        _ = await _store.FindWorkspaceByIdAsync(workspaceId, cancellationToken)
            ?? throw new RequestFailureException(404, "Workspace not found.");

        var existingCycle = await _store.FindUsageCycleByStartAsync(workspaceId, cycleStartUtc, cancellationToken);
        if (existingCycle is not null)
        {
            await ClosePreviousCycleIfNeededAsync(workspaceId, existingCycle.CycleStartUtc, observedAtUtc, cancellationToken);
            return existingCycle;
        }

        var assignment = await _store.FindActivePlanAssignmentAsync(workspaceId, cycleStartUtc, cancellationToken);
        if (assignment is null)
        {
            var defaultPlan = await EnsureDefaultPlanAsync(cancellationToken);

            assignment = new WorkspacePlanAssignment
            {
                WorkspaceId = workspaceId,
                PlanDefinitionId = defaultPlan.Id,
                EffectiveFromCycleStartUtc = cycleStartUtc,
                AssignedByUserId = requestedByUserId,
                ChangeReason = "Default workspace plan initialized."
            };

            await _store.AddPlanAssignmentAsync(assignment, cancellationToken);
            await RecordAssignmentAuditAsync(
                workspaceId,
                requestedByUserId,
                assignment.Id,
                $"Default plan '{defaultPlan.PlanCode}' assigned effective {cycleStartUtc:O}.",
                cancellationToken);
        }

        var rules = await _store.ListPlanLimitRulesAsync(assignment.PlanDefinitionId, cancellationToken);
        var cycle = new UsageCycle
        {
            WorkspaceId = workspaceId,
            CycleStartUtc = cycleStartUtc,
            CycleEndExclusiveUtc = cycleStartUtc.AddMonths(1),
            PlanAssignmentId = assignment.Id,
            Status = UsageCycleStatus.Open,
            OpenedAtUtc = observedAtUtc,
            LastCalculatedAtUtc = observedAtUtc
        };

        var metrics = rules
            .Select(rule => new UsageCycleMetric
            {
                UsageCycleId = cycle.Id,
                Dimension = rule.Dimension,
                IncludedQuantity = rule.IncludedQuantity,
                WarningThresholdQuantity = rule.WarningThresholdQuantity,
                HardLimitQuantity = rule.HardLimitQuantity,
                CurrentQuantity = 0m,
                OverageQuantity = 0m,
                LimitBehavior = rule.LimitBehavior,
                State = UsageCycleMetricState.WithinLimit
            })
            .ToList();

        await _store.AddUsageCycleAsync(cycle, cancellationToken);
        foreach (var metric in metrics)
        {
            await _store.AddUsageCycleMetricAsync(metric, cancellationToken);
        }

        await RecalculateMetricsAsync(workspaceId, cycle, metrics, observedAtUtc, "PlanAssignment", assignment.Id.ToString(), cancellationToken);
        await ClosePreviousCycleIfNeededAsync(workspaceId, cycleStartUtc, observedAtUtc, cancellationToken);

        return cycle;
    }

    private async Task ClosePreviousCycleIfNeededAsync(
        Guid workspaceId,
        DateTimeOffset cycleStartUtc,
        DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken)
    {
        var previousCycle = await _store.FindUsageCycleByStartAsync(workspaceId, cycleStartUtc.AddMonths(-1), cancellationToken);
        if (previousCycle is null || previousCycle.Status != UsageCycleStatus.Open)
        {
            return;
        }

        previousCycle.Status = UsageCycleStatus.Closed;
        previousCycle.ClosedAtUtc = observedAtUtc;
        previousCycle.LastCalculatedAtUtc = observedAtUtc;
        await _store.UpdateUsageCycleAsync(previousCycle, cancellationToken);
    }

    private async Task RecordAssignmentAuditAsync(
        Guid workspaceId,
        Guid? actorUserId,
        Guid assignmentId,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorUserId,
            ActionType = "billing.plan_assignment.apply",
            TargetType = "plan_assignment",
            TargetId = assignmentId.ToString(),
            Result = "success",
            Reason = reason
        }, cancellationToken);
    }

    private async Task<PlanDefinition> EnsureDefaultPlanAsync(CancellationToken cancellationToken)
    {
        var defaultPlan = await _store.FindDefaultPlanDefinitionAsync(cancellationToken);
        if (defaultPlan is not null)
        {
            return defaultPlan;
        }

        defaultPlan = new PlanDefinition
        {
            PlanCode = "starter",
            DisplayName = "Starter",
            IsDefault = true,
            IsActive = true
        };

        await _store.AddPlanDefinitionAsync(defaultPlan, cancellationToken);
        foreach (var rule in BuildDefaultRules(defaultPlan.Id))
        {
            await _store.AddPlanLimitRuleAsync(rule, cancellationToken);
        }

        return defaultPlan;
    }

    private static IReadOnlyList<PlanLimitRule> BuildDefaultRules(Guid planDefinitionId)
    {
        return
        [
            new PlanLimitRule
            {
                PlanDefinitionId = planDefinitionId,
                Dimension = BillingDimension.ActiveMembers,
                IncludedQuantity = 10m,
                WarningThresholdQuantity = 8m,
                HardLimitQuantity = 10m,
                LimitBehavior = BillingLimitBehavior.Restrict
            },
            new PlanLimitRule
            {
                PlanDefinitionId = planDefinitionId,
                Dimension = BillingDimension.AIActivityEvents,
                IncludedQuantity = 5_000m,
                WarningThresholdQuantity = 4_500m,
                LimitBehavior = BillingLimitBehavior.AllowOverage
            },
            new PlanLimitRule
            {
                PlanDefinitionId = planDefinitionId,
                Dimension = BillingDimension.EstimatedCost,
                IncludedQuantity = 150m,
                WarningThresholdQuantity = 120m,
                LimitBehavior = BillingLimitBehavior.AllowOverage
            }
        ];
    }

    private static void ValidateApply(ApplyWorkspacePlanAssignmentCommand command)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (command.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (string.IsNullOrWhiteSpace(command.PlanCode))
        {
            throw new RequestFailureException(400, "Plan code is required.");
        }

        if (command.EffectiveFromCycleStartUtc != GetCycleStart(command.EffectiveFromCycleStartUtc))
        {
            throw new RequestFailureException(400, "Plan assignments must become effective at a UTC month boundary.");
        }
    }

    private static DateTimeOffset GetCycleStart(DateTimeOffset value)
        => new(value.Year, value.Month, 1, 0, 0, 0, TimeSpan.Zero);
}
