using AIUsageGuard.Application.AIUsageEvents.ListEvents;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;

namespace AIUsageGuard.UnitTests.AIUsageEvents;

public sealed class ListAIUsageEventsQueryValidationTests
{
    [Fact]
    public async Task Query_rejects_page_size_above_limit()
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

        var service = new ListAIUsageEventsService(dbContext, new AuditService(dbContext));

        var exception = await Assert.ThrowsAsync<RequestFailureException>(() =>
            service.ListAsync(new ListAIUsageEventsQuery(
                workspaceId,
                actorId,
                null,
                null,
                null,
                null,
                null,
                1,
                201)));

        Assert.Equal(400, exception.StatusCode);
    }
}
