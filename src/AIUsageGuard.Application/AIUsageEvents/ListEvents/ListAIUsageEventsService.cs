using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.AIUsageEvents.ListEvents;

public sealed class ListAIUsageEventsService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public ListAIUsageEventsService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<ListAIUsageEventsResult> ListAsync(ListAIUsageEventsQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            var totalCount = await _store.CountAIUsageEventsAsync(
                query.WorkspaceId,
                query.EventType,
                query.ActorUserId,
                query.ToolName,
                query.FromOccurredAt,
                query.ToOccurredAt,
                cancellationToken);

            var items = await _store.ListAIUsageEventsAsync(
                query.WorkspaceId,
                query.EventType,
                query.ActorUserId,
                query.ToolName,
                query.FromOccurredAt,
                query.ToOccurredAt,
                query.PageNumber,
                query.PageSize,
                cancellationToken);

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "ai_usage_event.history.read",
                TargetType = "event_history",
                TargetId = null,
                Result = "success",
                Reason = "AI usage event history retrieved."
            }, cancellationToken);

            return new ListAIUsageEventsResult(items, query.PageNumber, query.PageSize, totalCount);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "ai_usage_event.history.read",
                TargetType = "event_history",
                TargetId = null,
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private static void Validate(ListAIUsageEventsQuery query)
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

        if (query.FromOccurredAt.HasValue && query.ToOccurredAt.HasValue && query.FromOccurredAt > query.ToOccurredAt)
        {
            throw new RequestFailureException(400, "The start date must be earlier than or equal to the end date.");
        }
    }
}
