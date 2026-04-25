using System.Text.Json;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Billing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.RiskDetection.EvaluateEvent;

namespace AIUsageGuard.Application.AIUsageEvents.IngestEvent;

public sealed class IngestAIUsageEventService
{
    private const int MaxPromptPreviewLength = 4000;
    private const int MaxStringLength = 200;
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly EvaluateAIUsageEventRiskService _riskEvaluationService;
    private readonly ApplyWorkspacePlanAssignmentService _planAssignmentService;
    private readonly BillingLimitEvaluator _limitEvaluator;

    public IngestAIUsageEventService(
        IPlatformStore store,
        IAuditService auditService,
        EvaluateAIUsageEventRiskService riskEvaluationService,
        ApplyWorkspacePlanAssignmentService planAssignmentService,
        BillingLimitEvaluator limitEvaluator)
    {
        _store = store;
        _auditService = auditService;
        _riskEvaluationService = riskEvaluationService;
        _planAssignmentService = planAssignmentService;
        _limitEvaluator = limitEvaluator;
    }

    public async Task<IngestAIUsageEventResult> IngestAsync(IngestAIUsageEventCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventType = ParseEventType(command.EventType);
            ValidateCommand(command, eventType);

            var existing = await _store.FindAIUsageEventByIdempotencyKeyAsync(command.WorkspaceId, command.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                await _auditService.RecordAsync(CreateAudit(command, "duplicate", existing.Id.ToString(), "Duplicate event submission ignored."), cancellationToken);
                return new IngestAIUsageEventResult(existing, true);
            }

            var targetCycle = await ResolveCycleAsync(command, cancellationToken);
            UsageCycleMetric? activityMetric = null;
            UsageCycleMetric? estimatedCostMetric = null;
            decimal? projectedActivityCount = null;
            decimal? projectedEstimatedCost = null;
            var isCurrentOpenCycle = targetCycle is not null &&
                targetCycle.Status == UsageCycleStatus.Open &&
                command.OccurredAt >= targetCycle.CycleStartUtc &&
                command.OccurredAt < targetCycle.CycleEndExclusiveUtc;

            if (isCurrentOpenCycle)
            {
                activityMetric = await _store.FindUsageCycleMetricAsync(targetCycle!.Id, BillingDimension.AIActivityEvents, cancellationToken);
                estimatedCostMetric = await _store.FindUsageCycleMetricAsync(targetCycle.Id, BillingDimension.EstimatedCost, cancellationToken);
                projectedActivityCount = (activityMetric?.CurrentQuantity ?? 0m) + 1m;
                projectedEstimatedCost = (estimatedCostMetric?.CurrentQuantity ?? 0m) + (command.EstimatedCost ?? 0m);

                await EnsureWithinRestrictedLimitAsync(
                    command,
                    BillingDimension.AIActivityEvents,
                    activityMetric,
                    projectedActivityCount.Value,
                    cancellationToken);
                await EnsureWithinRestrictedLimitAsync(
                    command,
                    BillingDimension.EstimatedCost,
                    estimatedCostMetric,
                    projectedEstimatedCost.Value,
                    cancellationToken);
            }

            var record = new AIUsageEvent
            {
                WorkspaceId = command.WorkspaceId,
                ActorUserId = command.ActorUserId,
                EventType = eventType,
                IdempotencyKey = command.IdempotencyKey.Trim(),
                ToolName = command.ToolName.Trim(),
                ModelName = NormalizeNullable(command.ModelName),
                SourceLabel = NormalizeNullable(command.SourceLabel),
                OccurredAt = command.OccurredAt,
                ReceivedAt = DateTimeOffset.UtcNow,
                PromptPreview = NormalizeNullable(command.PromptPreview),
                FileName = NormalizeNullable(command.FileName),
                FileSizeBytes = command.FileSizeBytes,
                InputTokenCount = command.InputTokenCount,
                OutputTokenCount = command.OutputTokenCount,
                EstimatedCost = command.EstimatedCost,
                DetailsJson = command.DetailsJson
            };

            await _store.AddAIUsageEventAsync(record, cancellationToken);
            if (isCurrentOpenCycle)
            {
                if (activityMetric is not null)
                {
                    await _limitEvaluator.ApplyAsync(
                        command.WorkspaceId,
                        activityMetric,
                        projectedActivityCount!.Value,
                        record.OccurredAt,
                        "AIUsageEvent",
                        record.Id.ToString(),
                        cancellationToken);
                }

                if (estimatedCostMetric is not null)
                {
                    await _limitEvaluator.ApplyAsync(
                        command.WorkspaceId,
                        estimatedCostMetric,
                        projectedEstimatedCost!.Value,
                        record.OccurredAt,
                        "AIUsageEvent",
                        record.Id.ToString(),
                        cancellationToken);
                }
            }

            await _riskEvaluationService.EvaluateAsync(
                new EvaluateAIUsageEventRiskCommand(command.WorkspaceId, record.Id, command.ActorUserId),
                cancellationToken);
            await _auditService.RecordAsync(CreateAudit(command, "success", record.Id.ToString(), "AI usage event accepted."), cancellationToken);
            return new IngestAIUsageEventResult(record, false);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(CreateAudit(command, "failed", null, exception.Message), cancellationToken);
            throw;
        }
    }

    private static AIUsageEventType ParseEventType(string eventType)
    {
        if (Enum.TryParse<AIUsageEventType>(eventType, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        var normalized = eventType.Trim().ToLowerInvariant();
        return normalized switch
        {
            "prompt_submitted" => AIUsageEventType.PromptSubmitted,
            "file_uploaded" => AIUsageEventType.FileUploaded,
            "tool_used" => AIUsageEventType.ToolUsed,
            "usage_recorded" => AIUsageEventType.UsageRecorded,
            "model_called" => AIUsageEventType.ModelCalled,
            _ => throw new RequestFailureException(400, "Event type is not supported for Phase 2 ingestion.")
        };
    }

    private static void ValidateCommand(IngestAIUsageEventCommand command, AIUsageEventType eventType)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (command.ActorUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Actor is required.");
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            throw new RequestFailureException(400, "Idempotency key is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ToolName))
        {
            throw new RequestFailureException(400, "Tool name is required.");
        }

        if (command.ToolName.Length > MaxStringLength)
        {
            throw new RequestFailureException(400, "Tool name is too long.");
        }

        if (command.ModelName is not null && command.ModelName.Length > MaxStringLength)
        {
            throw new RequestFailureException(400, "Model name is too long.");
        }

        if (command.SourceLabel is not null && command.SourceLabel.Length > MaxStringLength)
        {
            throw new RequestFailureException(400, "Source label is too long.");
        }

        if (command.PromptPreview is not null && command.PromptPreview.Length > MaxPromptPreviewLength)
        {
            throw new RequestFailureException(400, "Prompt preview is too long.");
        }

        if (command.FileName is not null && command.FileName.Length > 512)
        {
            throw new RequestFailureException(400, "File name is too long.");
        }

        if (command.FileSizeBytes is < 0)
        {
            throw new RequestFailureException(400, "File size cannot be negative.");
        }

        if (command.InputTokenCount is < 0 || command.OutputTokenCount is < 0)
        {
            throw new RequestFailureException(400, "Token counts cannot be negative.");
        }

        if (command.EstimatedCost is < 0)
        {
            throw new RequestFailureException(400, "Estimated cost cannot be negative.");
        }

        if (command.OccurredAt > DateTimeOffset.UtcNow.Add(FutureTolerance))
        {
            throw new RequestFailureException(400, "Occurred time cannot be in the future.");
        }

        if (command.DetailsJson is not null)
        {
            try
            {
                _ = JsonDocument.Parse(command.DetailsJson);
            }
            catch (JsonException)
            {
                throw new RequestFailureException(400, "Details payload is invalid.");
            }
        }

        _ = eventType;
    }

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AuditRecord CreateAudit(IngestAIUsageEventCommand command, string result, string? targetId, string reason)
    {
        return new AuditRecord
        {
            WorkspaceId = command.WorkspaceId,
            ActorUserId = command.ActorUserId,
            ActionType = "ai_usage_event.ingest",
            TargetType = "ai_usage_event",
            TargetId = targetId,
            Result = result,
            Reason = reason
        };
    }

    private async Task<UsageCycle?> ResolveCycleAsync(
        IngestAIUsageEventCommand command,
        CancellationToken cancellationToken)
    {
        var currentCycle = await _planAssignmentService.EnsureCurrentCycleAsync(
            command.WorkspaceId,
            command.ActorUserId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (command.OccurredAt >= currentCycle.CycleStartUtc &&
            command.OccurredAt < currentCycle.CycleEndExclusiveUtc)
        {
            return currentCycle;
        }

        return await _store.FindCurrentUsageCycleAsync(command.WorkspaceId, command.OccurredAt, cancellationToken);
    }

    private async Task EnsureWithinRestrictedLimitAsync(
        IngestAIUsageEventCommand command,
        BillingDimension dimension,
        UsageCycleMetric? metric,
        decimal projectedQuantity,
        CancellationToken cancellationToken)
    {
        if (metric is null || !_limitEvaluator.WouldExceedHardLimit(metric, projectedQuantity))
        {
            return;
        }

        await _limitEvaluator.RecordDeniedActionAsync(
            command.WorkspaceId,
            command.ActorUserId,
            dimension,
            projectedQuantity,
            metric.HardLimitQuantity ?? metric.IncludedQuantity,
            "ai_usage_event.ingest",
            cancellationToken);
        throw new RequestFailureException(409, $"This event would exceed the workspace {dimension} limit for the current plan.");
    }
}
