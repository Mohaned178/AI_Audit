using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Auditing.GetAuditLog;

public sealed class GetAuditLogService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public GetAuditLogService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<GetAuditLogResult> GetAsync(GetAuditLogQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            _ = await _store.FindWorkspaceByIdAsync(query.WorkspaceId, cancellationToken)
                ?? throw new RequestFailureException(404, "Workspace not found.");

            var record = await _store.FindAuditRecordAsync(query.WorkspaceId, query.AuditLogId, cancellationToken)
                ?? throw new RequestFailureException(404, $"Audit log {query.AuditLogId} was not found for the requested workspace.");

            var auditLog = new AuditLogDetail(
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
                record.CorrelationId,
                record.ClientIpAddressHash,
                record.UserAgent);

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "audit_log.read",
                TargetType = "audit_log",
                TargetId = query.AuditLogId.ToString(),
                Result = "success",
                Reason = $"Retrieved audit log {query.AuditLogId}."
            }, cancellationToken);

            return new GetAuditLogResult(query.WorkspaceId, auditLog);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "audit_log.read",
                TargetType = "audit_log",
                TargetId = query.AuditLogId.ToString(),
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

    private static void Validate(GetAuditLogQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (query.AuditLogId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Audit log is required.");
        }
    }
}
