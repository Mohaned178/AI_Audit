namespace AIUsageGuard.Api.Contracts.AIUsageEvents;

public sealed record AIUsageEventHistoryResponse(
    IReadOnlyList<AIUsageEventHistoryItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
