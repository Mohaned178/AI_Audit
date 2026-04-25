using AIUsageGuard.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Infrastructure.Notifications;

public sealed class LoggedNotificationSender : INotificationSender
{
    private readonly ILogger<LoggedNotificationSender> _logger;

    public LoggedNotificationSender(ILogger<LoggedNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string recipientAddress,
        string subject,
        string summaryBody,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Captured notification for {RecipientAddress}. Subject: {Subject}. Body: {SummaryBody}",
            recipientAddress,
            subject,
            summaryBody);

        return Task.CompletedTask;
    }
}
