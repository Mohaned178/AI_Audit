using AIUsageGuard.Application.AIUsageEvents.IngestEvent;
using AIUsageGuard.Application.AIUsageEvents.ListEvents;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.AIUsageEvents;

public sealed class ListAIUsageEventsServiceTests
{
    [Fact]
    public async Task List_returns_events_in_descending_order_with_filters()
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
        var ingestService = new IngestAIUsageEventService(dbContext, new AuditService(dbContext));
        var listService = new ListAIUsageEventsService(dbContext, new AuditService(dbContext));

        await ingestService.IngestAsync(new IngestAIUsageEventCommand(
            workspaceId,
            actorId,
            "evt-a",
            "prompt_submitted",
            DateTimeOffset.UtcNow.AddMinutes(-10),
            "ChatGPT",
            null,
            null,
            "alpha",
            null,
            null,
            null,
            null,
            null,
            null));

        await ingestService.IngestAsync(new IngestAIUsageEventCommand(
            workspaceId,
            actorId,
            "evt-b",
            "tool_used",
            DateTimeOffset.UtcNow.AddMinutes(-5),
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

        var result = await listService.ListAsync(new ListAIUsageEventsQuery(
            workspaceId,
            actorId,
            null,
            null,
            "Claude",
            null,
            null,
            1,
            50));

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Claude", result.Items[0].ToolName);
    }

    [Fact]
    public async Task List_rejects_reversed_date_range()
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
        var listService = new ListAIUsageEventsService(dbContext, new AuditService(dbContext));

        var exception = await Assert.ThrowsAsync<AIUsageGuard.Application.Errors.RequestFailureException>(() =>
            listService.ListAsync(new ListAIUsageEventsQuery(
                workspaceId,
                actorId,
                null,
                null,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                1,
                50)));

        Assert.Equal(400, exception.StatusCode);
    }
}
