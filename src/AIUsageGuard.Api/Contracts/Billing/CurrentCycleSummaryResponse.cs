namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record CurrentCycleSummaryResponse(
    Guid CycleId,
    DateTimeOffset CycleStartUtc,
    DateTimeOffset CycleEndExclusiveUtc,
    string Status);
