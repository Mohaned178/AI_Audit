using AIUsageGuard.Application.Billing.GetBillingCycle;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Billing;

public sealed class GetBillingCycleServiceTests
{
    [Fact]
    public async Task Get_returns_cycle_detail_with_limit_events_and_adjustments()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var (workspace, owner, previousCycleId) = await BillingUnitTestData.SeedBillingHistoryAsync(store);
        var service = new GetBillingCycleService(
            store,
            new AuditService(store),
            NullLogger<GetBillingCycleService>.Instance);

        var result = await service.GetAsync(new GetBillingCycleQuery(workspace.Id, owner.Id, previousCycleId));

        Assert.Equal(previousCycleId, result.Cycle.UsageCycleId);
        Assert.Equal(2, result.LimitEvents.Count);
        Assert.Single(result.Adjustments);
    }
}
