using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Auditing;

public interface IAuditService
{
    Task RecordAsync(AuditRecord record, CancellationToken cancellationToken = default);
}
