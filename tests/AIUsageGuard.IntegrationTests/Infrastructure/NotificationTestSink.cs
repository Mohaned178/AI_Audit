using System.Collections.Concurrent;
using AIUsageGuard.Application.Abstractions;

namespace AIUsageGuard.IntegrationTests.Infrastructure;

public sealed class NotificationTestSink : INotificationSender
{
    private readonly ConcurrentDictionary<string, int> _plannedFailures = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<NotificationTestMessage> _messages = new();

    public IReadOnlyCollection<NotificationTestMessage> Messages => _messages.ToArray();

    public Task SendAsync(
        string recipientAddress,
        string subject,
        string summaryBody,
        CancellationToken cancellationToken = default)
    {
        if (_plannedFailures.TryGetValue(recipientAddress, out var remaining) && remaining > 0)
        {
            _plannedFailures[recipientAddress] = remaining - 1;
            throw new InvalidOperationException($"Planned notification failure for {recipientAddress}.");
        }

        _messages.Enqueue(new NotificationTestMessage(recipientAddress, subject, summaryBody));
        return Task.CompletedTask;
    }

    public void PlanFailures(string recipientAddress, int count)
    {
        _plannedFailures[recipientAddress] = count;
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }

        _plannedFailures.Clear();
    }
}

public sealed record NotificationTestMessage(string RecipientAddress, string Subject, string SummaryBody);
