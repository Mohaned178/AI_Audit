using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Auditing.ListAuditLogs;

public sealed class ListAuditLogsService
{
    private static readonly HashSet<string> AllowedResults = new(StringComparer.OrdinalIgnoreCase)
    {
        "success",
        "failed",
        "denied",
        "duplicate"
    };

    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public ListAuditLogsService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<ListAuditLogsResult> ListAsync(ListAuditLogsQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            _ = await _store.FindWorkspaceByIdAsync(query.WorkspaceId, cancellationToken)
                ?? throw new RequestFailureException(404, "Workspace not found.");

            var filter = new AuditLogFilter
            {
                WorkspaceId = query.WorkspaceId,
                RequestedByUserId = query.RequestedByUserId,
                FromOccurredAtUtc = query.FromOccurredAtUtc,
                ToOccurredAtUtc = query.ToOccurredAtUtc,
                ActionType = Normalize(query.ActionType),
                Result = NormalizeResult(query.Result),
                ActorUserId = query.ActorUserId,
                IsSecurityRelevant = query.SecurityRelevant,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            var totalCount = await _store.CountAuditRecordsAsync(filter, cancellationToken);
            var records = await _store.ListAuditRecordsAsync(filter, cancellationToken);
            var items = new List<AuditLogListItem>(records.Count);
            foreach (var record in records)
            {
                items.Add(new AuditLogListItem(
                    record.Id,
                    record.OccurredAt,
                    record.ActorUserId,
                    await ResolveActorDisplayNameAsync(record.ActorUserId, cancellationToken),
                    record.ActionType,
                    record.Category,
                    record.TargetType,
                    record.TargetId,
                    record.Result,
                    record.Reason,
                    record.IsSecurityRelevant,
                    record.CorrelationId));
            }

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "audit_log.list",
                TargetType = "audit_log_history",
                TargetId = query.WorkspaceId.ToString(),
                Result = "success",
                Reason = $"Retrieved {items.Count} audit log entries."
            }, cancellationToken);

            return new ListAuditLogsResult(new AuditLogPage(query.WorkspaceId, items, query.PageNumber, query.PageSize, totalCount));
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "audit_log.list",
                TargetType = "audit_log_history",
                TargetId = query.WorkspaceId.ToString(),
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private async Task<string?> ResolveActorDisplayNameAsync(Guid? actorUserId, CancellationToken cancellationToken)
    {
        if (!actorUserId.HasValue)
        {
            return null;
        }

        var user = await _store.FindUserByIdAsync(actorUserId.Value, cancellationToken);
        if (user is null)
        {
            return "Unknown";
        }

        return string.IsNullOrWhiteSpace(user.DisplayName) ? "Unknown" : user.DisplayName;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeResult(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!AllowedResults.Contains(normalized))
        {
            throw new RequestFailureException(400, "Result must be success, failed, denied, or duplicate.");
        }

        return normalized;
    }

    private static void Validate(ListAuditLogsQuery query)
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

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            throw new RequestFailureException(400, "Page size must be between 1 and 100.");
        }

        if (query.FromOccurredAtUtc.HasValue && query.ToOccurredAtUtc.HasValue && query.FromOccurredAtUtc > query.ToOccurredAtUtc)
        {
            throw new RequestFailureException(400, "The start date must be earlier than or equal to the end date.");
        }
    }
}
