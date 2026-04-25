using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Infrastructure.Billing;

public sealed class DefaultPlanCatalogSeeder
{
    private readonly IPlatformStore _store;

    public DefaultPlanCatalogSeeder(IPlatformStore store)
    {
        _store = store;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingPlans = await _store.ListActivePlanDefinitionsAsync(cancellationToken);
        if (existingPlans.Count > 0)
        {
            return;
        }

        foreach (var definition in BuildDefaultPlans())
        {
            await _store.AddPlanDefinitionAsync(definition.Plan, cancellationToken);
            foreach (var rule in definition.Rules)
            {
                rule.PlanDefinitionId = definition.Plan.Id;
                await _store.AddPlanLimitRuleAsync(rule, cancellationToken);
            }
        }
    }

    private static IReadOnlyList<SeedPlanDefinition> BuildDefaultPlans()
    {
        return
        [
            new SeedPlanDefinition(
                new PlanDefinition
                {
                    PlanCode = "starter",
                    DisplayName = "Starter",
                    IsDefault = true,
                    IsActive = true
                },
                [
                    CreateRule(BillingDimension.ActiveMembers, 10m, 8m, 10m, BillingLimitBehavior.Restrict),
                    CreateRule(BillingDimension.AIActivityEvents, 5_000m, 4_500m, null, BillingLimitBehavior.AllowOverage),
                    CreateRule(BillingDimension.EstimatedCost, 150m, 120m, null, BillingLimitBehavior.AllowOverage)
                ]),
            new SeedPlanDefinition(
                new PlanDefinition
                {
                    PlanCode = "growth",
                    DisplayName = "Growth",
                    IsDefault = false,
                    IsActive = true
                },
                [
                    CreateRule(BillingDimension.ActiveMembers, 50m, 40m, 50m, BillingLimitBehavior.Restrict),
                    CreateRule(BillingDimension.AIActivityEvents, 25_000m, 22_500m, null, BillingLimitBehavior.AllowOverage),
                    CreateRule(BillingDimension.EstimatedCost, 1_000m, 850m, null, BillingLimitBehavior.AllowOverage)
                ])
        ];
    }

    private static PlanLimitRule CreateRule(
        BillingDimension dimension,
        decimal includedQuantity,
        decimal? warningThresholdQuantity,
        decimal? hardLimitQuantity,
        BillingLimitBehavior limitBehavior)
    {
        return new PlanLimitRule
        {
            Dimension = dimension,
            IncludedQuantity = includedQuantity,
            WarningThresholdQuantity = warningThresholdQuantity,
            HardLimitQuantity = hardLimitQuantity,
            LimitBehavior = limitBehavior
        };
    }

    private sealed record SeedPlanDefinition(PlanDefinition Plan, IReadOnlyList<PlanLimitRule> Rules);
}
