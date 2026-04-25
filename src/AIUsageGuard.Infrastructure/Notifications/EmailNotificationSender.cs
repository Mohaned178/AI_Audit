using AIUsageGuard.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Infrastructure.Notifications;

public sealed class EmailNotificationSender : INotificationSender
{
    private readonly ILogger<EmailNotificationSender> _logger;

    public EmailNotificationSender(ILogger<EmailNotificationSender> logger)
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
            "Sending email notification to {RecipientAddress} with subject {Subject}.",
            recipientAddress,
            subject);

        return Task.CompletedTask;
    }
}
