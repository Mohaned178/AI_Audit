using Microsoft.Extensions.DependencyInjection;

namespace AIUsageGuard.Infrastructure.BackgroundProcessing;

public sealed class BackgroundJobScopeRunner
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BackgroundJobScopeRunner(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task RunAsync<TService>(
        Func<TService, CancellationToken, Task> action,
        CancellationToken cancellationToken)
        where TService : notnull
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await action(service, cancellationToken);
    }
}
