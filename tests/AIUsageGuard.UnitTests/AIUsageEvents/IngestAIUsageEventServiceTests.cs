using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.AIUsageEvents.IngestEvent;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.AIUsageEvents;

public sealed class IngestAIUsageEventServiceTests
{
    [Fact]
    public async Task Ingest_accepts_valid_event_and_records_audit()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        dbContext.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = actorId
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.IngestAsync(new IngestAIUsageEventCommand(
            workspaceId,
            actorId,
            "evt-001",
            "prompt_submitted",
            DateTimeOffset.UtcNow,
            "ChatGPT",
            "gpt-5.4",
            null,
            "hello",
            null,
            null,
            null,
            null,
            null,
            null));

        Assert.False(result.IsDuplicate);
        Assert.Equal("ChatGPT", result.Event.ToolName);
        Assert.Equal(1, await dbContext.AIUsageEvents.CountAsync());
        Assert.Equal(1, await dbContext.AuditRecords.CountAsync());
    }

    [Fact]
    public async Task Ingest_returns_existing_event_for_duplicate_key()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        dbContext.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = actorId
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var first = await service.IngestAsync(new IngestAIUsageEventCommand(
            workspaceId,
            actorId,
            "evt-dup",
            "tool_used",
            DateTimeOffset.UtcNow,
            "Claude",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        var second = await service.IngestAsync(new IngestAIUsageEventCommand(
            workspaceId,
            actorId,
            "evt-dup",
            "tool_used",
            DateTimeOffset.UtcNow,
            "Claude",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        Assert.Equal(first.Event.Id, second.Event.Id);
        Assert.True(second.IsDuplicate);
        Assert.Equal(1, await dbContext.AIUsageEvents.CountAsync());
        Assert.Equal(2, await dbContext.AuditRecords.CountAsync());
    }

    private static IngestAIUsageEventService CreateService(IPlatformStore store)
        => new(store, new AuditService(store));
}
