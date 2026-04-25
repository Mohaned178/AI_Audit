using AIUsageGuard.Application.Auditing.GetAuditLog;
using AIUsageGuard.Application.Auditing.ListAuditLogs;

namespace AIUsageGuard.Api.Contracts.AuditLogs;

internal static class AuditLogResponseFactory
{
    public static AuditLogListResponse ToAuditLogListResponse(ListAuditLogsResult result)
    {
        return new AuditLogListResponse(
            result.Page.WorkspaceId,
            new AuditLogPageResponse(
                result.Page.Items.Select(ToAuditLogSummaryResponse).ToList(),
                result.Page.PageNumber,
                result.Page.PageSize,
                result.Page.TotalCount));
    }

    public static AuditLogDetailResponse ToAuditLogDetailResponse(GetAuditLogResult result)
    {
        return new AuditLogDetailResponse(
            result.WorkspaceId,
            ToAuditLogDetailItemResponse(result.AuditLog));
    }

    public static AuditLogSummaryResponse ToAuditLogSummaryResponse(AIUsageGuard.Application.Models.AuditLogListItem item)
    {
        return new AuditLogSummaryResponse(
            item.AuditLogId,
            item.OccurredAtUtc,
            item.ActorUserId,
            item.ActorDisplayName,
            item.ActionType,
            item.Category,
            item.TargetType,
            item.TargetId,
            item.Result,
            item.Reason,
            item.IsSecurityRelevant,
            item.CorrelationId);
    }

    public static AuditLogDetailItemResponse ToAuditLogDetailItemResponse(AIUsageGuard.Application.Models.AuditLogDetail item)
    {
        return new AuditLogDetailItemResponse(
            item.AuditLogId,
            item.OccurredAtUtc,
            item.ActorUserId,
            item.ActorDisplayName,
            item.ActionType,
            item.Category,
            item.TargetType,
            item.TargetId,
            item.Result,
            item.Reason,
            item.IsSecurityRelevant,
            item.CorrelationId,
            item.ClientIpAddressHash is null && item.UserAgent is null
                ? null
                : new AuditLogClientContextResponse(item.ClientIpAddressHash, item.UserAgent));
    }
}
