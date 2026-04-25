namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record UsageSummaryRowResponse(
    Guid? ActorUserId,
    string DisplayLabel,
    int TotalEvents,
    int FlaggedFindingCount,
    decimal EstimatedCost,
    int EventsWithEstimatedCost,
    int EventsMissingEstimatedCost,
    DateTimeOffset? LastActivityAt);
