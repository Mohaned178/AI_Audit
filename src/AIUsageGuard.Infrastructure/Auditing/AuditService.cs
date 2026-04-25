using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private static readonly Meter Meter = new("AIUsageGuard.Auditing");
    private static readonly Counter<long> RecordedCounter = Meter.CreateCounter<long>("ai_usage_guard.audit.records_recorded");
    private static readonly Counter<long> SecurityRelevantCounter = Meter.CreateCounter<long>("ai_usage_guard.audit.security_relevant_records");

    private readonly IPlatformStore _store;
    private readonly AuditCategoryResolver _categoryResolver;
    private readonly AuditRequestContextAccessor _requestContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IPlatformStore store,
        AuditCategoryResolver categoryResolver,
        AuditRequestContextAccessor requestContextAccessor)
        : this(store, categoryResolver, requestContextAccessor, NullLogger<AuditService>.Instance)
    {
    }

    public AuditService(
        IPlatformStore store,
        AuditCategoryResolver categoryResolver,
        AuditRequestContextAccessor requestContextAccessor,
        ILogger<AuditService> logger)
    {
        _store = store;
        _categoryResolver = categoryResolver;
        _requestContextAccessor = requestContextAccessor;
        _logger = logger;
    }

    public AuditService(IPlatformStore store)
        : this(
            store,
            new AuditCategoryResolver(),
            new AuditRequestContextAccessor(new HttpContextAccessor()),
            NullLogger<AuditService>.Instance)
    {
    }

    public Task RecordAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        record.Category = _categoryResolver.ResolveCategory(record);
        record.IsSecurityRelevant = _categoryResolver.IsSecurityRelevant(record);

        var requestContext = _requestContextAccessor.Capture();
        record.CorrelationId ??= requestContext.CorrelationId;
        record.ClientIpAddressHash ??= requestContext.ClientIpAddressHash;
        record.UserAgent ??= requestContext.UserAgent;

        RecordedCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", record.WorkspaceId?.ToString()));
        if (record.IsSecurityRelevant)
        {
            SecurityRelevantCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", record.WorkspaceId?.ToString()));
        }

        _logger.LogInformation(
            "Recorded audit event {ActionType} for workspace {WorkspaceId} with result {Result}.",
            record.ActionType,
            record.WorkspaceId,
            record.Result);

        return _store.AddAuditAsync(record, cancellationToken);
    }
}
