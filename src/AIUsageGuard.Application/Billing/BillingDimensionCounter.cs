using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Billing;

public sealed class BillingDimensionCounter
{
    private readonly IPlatformStore _store;

    public BillingDimensionCounter(IPlatformStore store)
    {
        _store = store;
    }

    public async Task<decimal> CountAsync(
        Guid workspaceId,
        BillingDimension dimension,
        DateTimeOffset cycleStartUtc,
        DateTimeOffset cycleEndExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        return dimension switch
        {
            BillingDimension.ActiveMembers => await _store.CountActiveMembershipsAsync(workspaceId, cancellationToken),
            BillingDimension.AIActivityEvents => await _store.CountAcceptedAIUsageEventsAsync(workspaceId, cycleStartUtc, cycleEndExclusiveUtc, cancellationToken),
            BillingDimension.EstimatedCost => await _store.SumAIUsageEventCostsAsync(workspaceId, cycleStartUtc, cycleEndExclusiveUtc.AddTicks(-1), cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unsupported billing dimension.")
        };
    }
}
