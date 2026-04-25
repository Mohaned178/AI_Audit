using AIUsageGuard.Application.Billing.ReconcileUsageCycles;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class BillingCycleReconciliationTests
{
    [Fact]
    public async Task Reconciliation_applies_late_activity_adjustment_to_closed_cycle()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        await BillingTestData.SeedLateActivityScenarioAsync(factory, owner);

        var result = await factory.ExecuteServiceAsync<ReconcileUsageCyclesService, ReconcileUsageCyclesResult>(
            service => service.RunAsync(new ReconcileUsageCyclesCommand(new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero))));

        Assert.True(result.CyclesAdjusted >= 1);
        Assert.True(await factory.ExecuteDbContextAsync(async dbContext =>
            (await dbContext.CycleAdjustments.ToListAsync())
                .Any(item => item.Reason.Contains("Late or corrected activity", StringComparison.Ordinal))));
    }
}
