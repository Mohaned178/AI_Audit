using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Identity;
using AIUsageGuard.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AIUsageGuard.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IPlatformStore
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<WorkspaceMembership> WorkspaceMemberships => Set<WorkspaceMembership>();

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    public DbSet<AIUsageEvent> AIUsageEvents => Set<AIUsageEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new WorkspaceConfiguration());
        builder.ApplyConfiguration(new WorkspaceMembershipConfiguration());
        builder.ApplyConfiguration(new AuditRecordConfiguration());
        builder.ApplyConfiguration(new AIUsageEventConfiguration());

        builder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("user_accounts");
            user.Property(item => item.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            user.Property(item => item.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            user.Property(item => item.CreatedAt)
                .IsRequired();
        });

        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
    }

    public async Task<UserAccount?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var entity = await Users.FirstOrDefaultAsync(
            user => user.NormalizedEmail == email.Trim().ToUpperInvariant(),
            cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<UserAccount?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task AddUserAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        Users.Add(ToEntity(user));
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        var entity = await Users.FirstAsync(item => item.Id == user.Id, cancellationToken);
        entity.Email = user.Email;
        entity.NormalizedEmail = user.Email.ToUpperInvariant();
        entity.UserName = user.Email;
        entity.NormalizedUserName = user.Email.ToUpperInvariant();
        entity.DisplayName = user.DisplayName;
        entity.PasswordHash = user.PasswordHash;
        entity.Status = user.Status;
        entity.CreatedAt = user.CreatedAt;
        entity.LastSignInAt = user.LastSignInAt;
        await SaveChangesAsync(cancellationToken);
    }

    public Task<Workspace?> FindWorkspaceByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Workspaces.FirstOrDefaultAsync(workspace => workspace.Id == id, cancellationToken);

    public Task<Workspace?> FindWorkspaceBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Workspaces.FirstOrDefaultAsync(workspace => workspace.Slug == slug, cancellationToken);

    public async Task AddWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        Workspaces.Add(workspace);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        Workspaces.Update(workspace);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<WorkspaceMembership?> FindMembershipByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => WorkspaceMemberships.FirstOrDefaultAsync(membership => membership.Id == id, cancellationToken);

    public Task<WorkspaceMembership?> FindActiveMembershipAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
        => WorkspaceMemberships.FirstOrDefaultAsync(
            membership => membership.WorkspaceId == workspaceId &&
                          membership.UserId == userId &&
                          membership.Status == MembershipStatus.Active,
            cancellationToken);

    public async Task<IReadOnlyList<WorkspaceMembership>> ListMembershipsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await WorkspaceMemberships
            .Where(membership => membership.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkspaceMembership>> ListMembershipsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => await WorkspaceMemberships
            .Where(membership => membership.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

    public async Task AddMembershipAsync(WorkspaceMembership membership, CancellationToken cancellationToken = default)
    {
        WorkspaceMemberships.Add(membership);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMembershipAsync(WorkspaceMembership membership, CancellationToken cancellationToken = default)
    {
        WorkspaceMemberships.Update(membership);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountOwnerMembershipsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => WorkspaceMemberships.CountAsync(
            membership => membership.WorkspaceId == workspaceId &&
                          membership.Role == WorkspaceRole.Owner &&
                          membership.Status == MembershipStatus.Active,
            cancellationToken);

    public async Task AddAuditAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        AuditRecords.Add(record);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRecord>> ListAuditsAsync(CancellationToken cancellationToken = default)
        => await AuditRecords
            .OrderBy(record => record.OccurredAt)
            .ToListAsync(cancellationToken);

    public Task<AIUsageEvent?> FindAIUsageEventByIdempotencyKeyAsync(Guid workspaceId, string idempotencyKey, CancellationToken cancellationToken = default)
        => AIUsageEvents.FirstOrDefaultAsync(
            item => item.WorkspaceId == workspaceId && item.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public async Task AddAIUsageEventAsync(AIUsageEvent aiUsageEvent, CancellationToken cancellationToken = default)
    {
        AIUsageEvents.Add(aiUsageEvent);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AIUsageEvent>> ListAIUsageEventsAsync(
        Guid workspaceId,
        AIUsageEventType? eventType,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = await BuildAIUsageEventQuery(workspaceId).ToListAsync(cancellationToken);
        return items
            .Where(item => !eventType.HasValue || item.EventType == eventType.Value)
            .Where(item => !actorUserId.HasValue || item.ActorUserId == actorUserId.Value)
            .Where(item => string.IsNullOrWhiteSpace(toolName) || item.ToolName.Equals(toolName.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(item => !fromOccurredAt.HasValue || item.OccurredAt >= fromOccurredAt.Value)
            .Where(item => !toOccurredAt.HasValue || item.OccurredAt <= toOccurredAt.Value)
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.ReceivedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public Task<int> CountAIUsageEventsAsync(
        Guid workspaceId,
        AIUsageEventType? eventType,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        CancellationToken cancellationToken = default)
    {
        return CountAIUsageEventsInMemoryAsync(
            workspaceId,
            eventType,
            actorUserId,
            toolName,
            fromOccurredAt,
            toOccurredAt,
            cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        AuditRecords.RemoveRange(AuditRecords);
        AIUsageEvents.RemoveRange(AIUsageEvents);
        WorkspaceMemberships.RemoveRange(WorkspaceMemberships);
        Workspaces.RemoveRange(Workspaces);
        Users.RemoveRange(Users);
        await SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AIUsageEvent> BuildAIUsageEventQuery(Guid workspaceId)
        => AIUsageEvents.Where(item => item.WorkspaceId == workspaceId);

    private async Task<int> CountAIUsageEventsInMemoryAsync(
        Guid workspaceId,
        AIUsageEventType? eventType,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        CancellationToken cancellationToken)
    {
        var items = await BuildAIUsageEventQuery(workspaceId).ToListAsync(cancellationToken);
        return items
            .Where(item => !eventType.HasValue || item.EventType == eventType.Value)
            .Where(item => !actorUserId.HasValue || item.ActorUserId == actorUserId.Value)
            .Where(item => string.IsNullOrWhiteSpace(toolName) || item.ToolName.Equals(toolName.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(item => !fromOccurredAt.HasValue || item.OccurredAt >= fromOccurredAt.Value)
            .Where(item => !toOccurredAt.HasValue || item.OccurredAt <= toOccurredAt.Value)
            .Count();
    }

    private static UserAccount ToModel(ApplicationUser entity)
    {
        return new UserAccount
        {
            Id = entity.Id,
            Email = entity.Email ?? string.Empty,
            DisplayName = entity.DisplayName,
            PasswordHash = entity.PasswordHash ?? string.Empty,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            LastSignInAt = entity.LastSignInAt
        };
    }

    private static ApplicationUser ToEntity(UserAccount model)
    {
        return new ApplicationUser
        {
            Id = model.Id,
            Email = model.Email,
            NormalizedEmail = model.Email.ToUpperInvariant(),
            UserName = model.Email,
            NormalizedUserName = model.Email.ToUpperInvariant(),
            DisplayName = model.DisplayName,
            PasswordHash = model.PasswordHash,
            Status = model.Status,
            CreatedAt = model.CreatedAt,
            LastSignInAt = model.LastSignInAt,
            EmailConfirmed = true
        };
    }
}
