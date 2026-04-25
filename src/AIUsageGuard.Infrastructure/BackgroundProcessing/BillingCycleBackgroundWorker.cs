using AIUsageGuard.Application.Billing;
using AIUsageGuard.Application.Billing.ReconcileUsageCycles;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Infrastructure.BackgroundProcessing;

public sealed class BillingCycleBackgroundWorker : BackgroundService
{
    private readonly BackgroundProcessingClock _clock;
    private readonly BackgroundJobScopeRunner _scopeRunner;
    private readonly BillingReconciliationOptions _options;
    private readonly ILogger<BillingCycleBackgroundWorker> _logger;

    public BillingCycleBackgroundWorker(
        BackgroundProcessingClock clock,
        BackgroundJobScopeRunner scopeRunner,
        IOptions<BillingReconciliationOptions> options,
        ILogger<BillingCycleBackgroundWorker> logger)
    {
        _clock = clock;
        _scopeRunner = scopeRunner;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.WorkerInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            var scheduledForUtc = _clock.UtcNow;

            try
            {
                await _scopeRunner.RunAsync<ReconcileUsageCyclesService>(
                    (service, cancellationToken) => service.RunAsync(new ReconcileUsageCyclesCommand(scheduledForUtc), cancellationToken),
                    stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Billing cycle background worker iteration failed.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }
}
