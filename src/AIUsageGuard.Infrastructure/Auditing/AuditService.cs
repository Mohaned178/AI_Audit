using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly IPlatformStore _store;

    public AuditService(IPlatformStore store)
    {
        _store = store;
    }

    public Task RecordAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        return _store.AddAuditAsync(record, cancellationToken);
    }
}
