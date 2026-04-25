using AIUsageGuard.Application.Billing.ListBillingCycles;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Billing;

public sealed class ListBillingCyclesServiceTests
{
    [Fact]
    public async Task List_returns_paged_billing_cycle_summaries()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var (workspace, owner, _) = await BillingUnitTestData.SeedBillingHistoryAsync(store);
        var service = new ListBillingCyclesService(
            store,
            new AuditService(store),
            BillingTestFactory.CreatePlanAssignmentService(store),
            NullLogger<ListBillingCyclesService>.Instance);

        var result = await service.ListAsync(new ListBillingCyclesQuery(workspace.Id, owner.Id, 1, 20));

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("starter", result.Items[0].PlanCode);
    }
}
