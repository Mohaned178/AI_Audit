namespace AIUsageGuard.Api.Contracts.Notifications;

public sealed record NotificationListResponse(
    IReadOnlyList<NotificationListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
