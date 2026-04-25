namespace AIUsageGuard.Application.Billing;

public sealed class BillingReconciliationOptions
{
    public const string SectionName = "Billing:Reconciliation";

    public TimeSpan WorkerInterval { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan LateActivityWindow { get; set; } = TimeSpan.FromDays(2);

    public int MaxWorkspaceBatchSize { get; set; } = 100;
}
