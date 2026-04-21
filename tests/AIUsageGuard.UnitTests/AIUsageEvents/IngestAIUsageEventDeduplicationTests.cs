using AIUsageGuard.Application.AIUsageEvents.IngestEvent;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;

namespace AIUsageGuard.UnitTests.AIUsageEvents;

public sealed class IngestAIUsageEventDeduplicationTests
{
    [Fact]
    public async Task Duplicate_key_returns_existing_event()
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

        var service = new IngestAIUsageEventService(dbContext, new AuditService(dbContext));

        var first = await service.IngestAsync(new IngestAIUsageEventCommand(
            workspaceId,
            actorId,
            "evt-dedup",
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
            "evt-dedup",
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
    }
}
