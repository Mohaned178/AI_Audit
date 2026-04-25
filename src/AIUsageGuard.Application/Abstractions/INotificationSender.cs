namespace AIUsageGuard.Application.Abstractions;

public interface INotificationSender
{
    Task SendAsync(
        string recipientAddress,
        string subject,
        string summaryBody,
        CancellationToken cancellationToken = default);
}
