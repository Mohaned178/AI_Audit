using AIUsageGuard.Application.BackgroundJobs.RunDeliveryRetry;
using AIUsageGuard.Application.BackgroundJobs.RunDigestGeneration;
using AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;
using AIUsageGuard.Application.Notifications;
using AIUsageGuard.Application.Notifications.DeliverPendingNotifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Infrastructure.BackgroundProcessing;

public sealed class NotificationBackgroundWorker : BackgroundService
{
    private readonly BackgroundProcessingClock _clock;
    private readonly BackgroundJobScopeRunner _scopeRunner;
    private readonly NotificationProcessingOptions _options;
    private readonly ILogger<NotificationBackgroundWorker> _logger;

    public NotificationBackgroundWorker(
        BackgroundProcessingClock clock,
        BackgroundJobScopeRunner scopeRunner,
        IOptions<NotificationProcessingOptions> options,
        ILogger<NotificationBackgroundWorker> logger)
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
                await _scopeRunner.RunAsync<RunUrgentAlertScanService>(
                    (service, cancellationToken) => service.RunAsync(new RunUrgentAlertScanCommand(scheduledForUtc), cancellationToken),
                    stoppingToken);
                await _scopeRunner.RunAsync<RunDigestGenerationService>(
                    (service, cancellationToken) => service.RunAsync(new RunDigestGenerationCommand(scheduledForUtc), cancellationToken),
                    stoppingToken);
                await _scopeRunner.RunAsync<RunDeliveryRetryService>(
                    (service, cancellationToken) => service.RunAsync(new RunDeliveryRetryCommand(scheduledForUtc), cancellationToken),
                    stoppingToken);
                await _scopeRunner.RunAsync<DeliverPendingNotificationsService>(
                    (service, cancellationToken) => service.RunAsync(new DeliverPendingNotificationsCommand(scheduledForUtc), cancellationToken),
                    stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Notification background worker iteration failed.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }
}
