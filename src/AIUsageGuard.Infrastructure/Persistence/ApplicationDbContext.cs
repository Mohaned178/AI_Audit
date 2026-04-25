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

    public DbSet<PlanDefinition> PlanDefinitions => Set<PlanDefinition>();

    public DbSet<PlanLimitRule> PlanLimitRules => Set<PlanLimitRule>();

    public DbSet<WorkspacePlanAssignment> WorkspacePlanAssignments => Set<WorkspacePlanAssignment>();

    public DbSet<UsageCycle> UsageCycles => Set<UsageCycle>();

    public DbSet<UsageCycleMetric> UsageCycleMetrics => Set<UsageCycleMetric>();

    public DbSet<LimitEvent> LimitEvents => Set<LimitEvent>();

    public DbSet<CycleAdjustment> CycleAdjustments => Set<CycleAdjustment>();

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    public DbSet<AIUsageEvent> AIUsageEvents => Set<AIUsageEvent>();

    public DbSet<RiskFinding> RiskFindings => Set<RiskFinding>();

    public DbSet<RiskEvaluationOutcome> RiskEvaluationOutcomes => Set<RiskEvaluationOutcome>();

    public DbSet<WorkspaceRiskPolicy> WorkspaceRiskPolicies => Set<WorkspaceRiskPolicy>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<BackgroundJobRun> BackgroundJobRuns => Set<BackgroundJobRun>();

    public DbSet<NotificationMessage> Notifications => Set<NotificationMessage>();

    public DbSet<NotificationDeliveryOutcome> NotificationDeliveryOutcomes => Set<NotificationDeliveryOutcome>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new WorkspaceConfiguration());
        builder.ApplyConfiguration(new WorkspaceMembershipConfiguration());
        builder.ApplyConfiguration(new PlanDefinitionConfiguration());
        builder.ApplyConfiguration(new PlanLimitRuleConfiguration());
        builder.ApplyConfiguration(new WorkspacePlanAssignmentConfiguration());
        builder.ApplyConfiguration(new UsageCycleConfiguration());
        builder.ApplyConfiguration(new UsageCycleMetricConfiguration());
        builder.ApplyConfiguration(new LimitEventConfiguration());
        builder.ApplyConfiguration(new CycleAdjustmentConfiguration());
        builder.ApplyConfiguration(new AuditRecordConfiguration());
        builder.ApplyConfiguration(new AIUsageEventConfiguration());
        builder.ApplyConfiguration(new RiskFindingConfiguration());
        builder.ApplyConfiguration(new RiskEvaluationOutcomeConfiguration());
        builder.ApplyConfiguration(new WorkspaceRiskPolicyConfiguration());
        builder.ApplyConfiguration(new NotificationPreferenceConfiguration());
        builder.ApplyConfiguration(new BackgroundJobRunConfiguration());
        builder.ApplyConfiguration(new NotificationMessageConfiguration());
        builder.ApplyConfiguration(new NotificationDeliveryOutcomeConfiguration());

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

            user.Property(item => item.FailedSignInCount)
                .HasDefaultValue(0)
                .IsRequired();

            user.Property(item => item.LastFailedSignInAt)
                .HasColumnType("timestamp with time zone");

            user.Property(item => item.LockedUntilUtc)
                .HasColumnType("timestamp with time zone");

            user.HasIndex(item => item.LockedUntilUtc);
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
        UpdateUserEntity(entity, user);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserLockoutAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        var entity = await Users.FirstAsync(item => item.Id == user.Id, cancellationToken);
        UpdateUserEntity(entity, user);
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

    public Task<int> CountActiveMembershipsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => WorkspaceMemberships.CountAsync(
            membership => membership.WorkspaceId == workspaceId &&
                          membership.Status == MembershipStatus.Active,
            cancellationToken);

    public Task<PlanDefinition?> FindPlanDefinitionByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => PlanDefinitions.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<PlanDefinition?> FindPlanDefinitionByCodeAsync(string planCode, CancellationToken cancellationToken = default)
        => PlanDefinitions.FirstOrDefaultAsync(item => item.PlanCode == planCode, cancellationToken);

    public Task<PlanDefinition?> FindDefaultPlanDefinitionAsync(CancellationToken cancellationToken = default)
        => PlanDefinitions.FirstOrDefaultAsync(item => item.IsDefault && item.IsActive, cancellationToken);

    public async Task<IReadOnlyList<PlanDefinition>> ListActivePlanDefinitionsAsync(CancellationToken cancellationToken = default)
        => await PlanDefinitions
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.DisplayName)
            .ToListAsync(cancellationToken);

    public async Task AddPlanDefinitionAsync(PlanDefinition planDefinition, CancellationToken cancellationToken = default)
    {
        PlanDefinitions.Add(planDefinition);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePlanDefinitionAsync(PlanDefinition planDefinition, CancellationToken cancellationToken = default)
    {
        PlanDefinitions.Update(planDefinition);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<PlanLimitRule?> FindPlanLimitRuleAsync(Guid planDefinitionId, BillingDimension dimension, CancellationToken cancellationToken = default)
        => PlanLimitRules.FirstOrDefaultAsync(
            item => item.PlanDefinitionId == planDefinitionId && item.Dimension == dimension,
            cancellationToken);

    public async Task<IReadOnlyList<PlanLimitRule>> ListPlanLimitRulesAsync(Guid planDefinitionId, CancellationToken cancellationToken = default)
        => await PlanLimitRules
            .Where(item => item.PlanDefinitionId == planDefinitionId)
            .OrderBy(item => item.Dimension)
            .ToListAsync(cancellationToken);

    public async Task AddPlanLimitRuleAsync(PlanLimitRule rule, CancellationToken cancellationToken = default)
    {
        PlanLimitRules.Add(rule);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePlanLimitRuleAsync(PlanLimitRule rule, CancellationToken cancellationToken = default)
    {
        PlanLimitRules.Update(rule);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<WorkspacePlanAssignment?> FindPlanAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        => WorkspacePlanAssignments.FirstOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);

    public Task<WorkspacePlanAssignment?> FindActivePlanAssignmentAsync(
        Guid workspaceId,
        DateTimeOffset cycleStartUtc,
        CancellationToken cancellationToken = default)
        => FindActivePlanAssignmentCoreAsync(workspaceId, cycleStartUtc, cancellationToken);

    public Task<WorkspacePlanAssignment?> FindNextPlanAssignmentAsync(
        Guid workspaceId,
        DateTimeOffset cycleStartUtc,
        CancellationToken cancellationToken = default)
        => FindNextPlanAssignmentCoreAsync(workspaceId, cycleStartUtc, cancellationToken);

    public async Task<IReadOnlyList<WorkspacePlanAssignment>> ListPlanAssignmentsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => (await WorkspacePlanAssignments
                .Where(item => item.WorkspaceId == workspaceId)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.EffectiveFromCycleStartUtc)
            .ToList();

    private async Task<WorkspacePlanAssignment?> FindActivePlanAssignmentCoreAsync(
        Guid workspaceId,
        DateTimeOffset cycleStartUtc,
        CancellationToken cancellationToken)
        => (await WorkspacePlanAssignments
                .Where(item => item.WorkspaceId == workspaceId)
                .ToListAsync(cancellationToken))
            .Where(item =>
                item.EffectiveFromCycleStartUtc <= cycleStartUtc &&
                (!item.EffectiveToCycleStartUtc.HasValue || item.EffectiveToCycleStartUtc.Value > cycleStartUtc))
            .OrderByDescending(item => item.EffectiveFromCycleStartUtc)
            .FirstOrDefault();

    private async Task<WorkspacePlanAssignment?> FindNextPlanAssignmentCoreAsync(
        Guid workspaceId,
        DateTimeOffset cycleStartUtc,
        CancellationToken cancellationToken)
        => (await WorkspacePlanAssignments
                .Where(item => item.WorkspaceId == workspaceId)
                .ToListAsync(cancellationToken))
            .Where(item => item.EffectiveFromCycleStartUtc > cycleStartUtc)
            .OrderBy(item => item.EffectiveFromCycleStartUtc)
            .FirstOrDefault();

    public async Task AddPlanAssignmentAsync(WorkspacePlanAssignment assignment, CancellationToken cancellationToken = default)
    {
        WorkspacePlanAssignments.Add(assignment);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePlanAssignmentAsync(WorkspacePlanAssignment assignment, CancellationToken cancellationToken = default)
    {
        WorkspacePlanAssignments.Update(assignment);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<UsageCycle?> FindUsageCycleAsync(Guid workspaceId, Guid cycleId, CancellationToken cancellationToken = default)
        => UsageCycles.FirstOrDefaultAsync(
            item => item.WorkspaceId == workspaceId && item.Id == cycleId,
            cancellationToken);

    public Task<UsageCycle?> FindUsageCycleByStartAsync(Guid workspaceId, DateTimeOffset cycleStartUtc, CancellationToken cancellationToken = default)
        => UsageCycles.FirstOrDefaultAsync(
            item => item.WorkspaceId == workspaceId && item.CycleStartUtc == cycleStartUtc,
            cancellationToken);

    public Task<UsageCycle?> FindCurrentUsageCycleAsync(Guid workspaceId, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
        => UsageCycles.FirstOrDefaultAsync(
            item => item.WorkspaceId == workspaceId &&
                    item.CycleStartUtc <= asOfUtc &&
                    item.CycleEndExclusiveUtc > asOfUtc,
            cancellationToken);

    public async Task<IReadOnlyList<UsageCycle>> ListUsageCyclesAsync(
        Guid workspaceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        => (await UsageCycles
                .Where(item => item.WorkspaceId == workspaceId)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.CycleStartUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

    public async Task<IReadOnlyList<UsageCycle>> ListUsageCyclesDueForReconciliationAsync(
        DateTimeOffset dueBeforeUtc,
        int maxCount,
        CancellationToken cancellationToken = default)
        => (await UsageCycles
                .ToListAsync(cancellationToken))
            .Where(item => item.CycleEndExclusiveUtc <= dueBeforeUtc)
            .OrderBy(item => item.CycleEndExclusiveUtc)
            .Take(maxCount)
            .ToList();

    public Task<int> CountUsageCyclesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => UsageCycles.CountAsync(item => item.WorkspaceId == workspaceId, cancellationToken);

    public async Task AddUsageCycleAsync(UsageCycle cycle, CancellationToken cancellationToken = default)
    {
        UsageCycles.Add(cycle);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUsageCycleAsync(UsageCycle cycle, CancellationToken cancellationToken = default)
    {
        UsageCycles.Update(cycle);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<UsageCycleMetric?> FindUsageCycleMetricAsync(Guid usageCycleId, BillingDimension dimension, CancellationToken cancellationToken = default)
        => UsageCycleMetrics.FirstOrDefaultAsync(
            item => item.UsageCycleId == usageCycleId && item.Dimension == dimension,
            cancellationToken);

    public async Task<IReadOnlyList<UsageCycleMetric>> ListUsageCycleMetricsAsync(Guid usageCycleId, CancellationToken cancellationToken = default)
        => await UsageCycleMetrics
            .Where(item => item.UsageCycleId == usageCycleId)
            .OrderBy(item => item.Dimension)
            .ToListAsync(cancellationToken);

    public async Task AddUsageCycleMetricAsync(UsageCycleMetric metric, CancellationToken cancellationToken = default)
    {
        UsageCycleMetrics.Add(metric);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUsageCycleMetricAsync(UsageCycleMetric metric, CancellationToken cancellationToken = default)
    {
        UsageCycleMetrics.Update(metric);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<LimitEvent?> FindLatestLimitEventAsync(
        Guid workspaceId,
        Guid usageCycleId,
        BillingDimension dimension,
        CancellationToken cancellationToken = default)
        => FindLatestLimitEventCoreAsync(workspaceId, usageCycleId, dimension, cancellationToken);

    public async Task<IReadOnlyList<LimitEvent>> ListLimitEventsAsync(Guid usageCycleId, CancellationToken cancellationToken = default)
        => (await LimitEvents
                .Where(item => item.UsageCycleId == usageCycleId)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.OccurredAtUtc)
            .ToList();

    public async Task AddLimitEventAsync(LimitEvent limitEvent, CancellationToken cancellationToken = default)
    {
        LimitEvents.Add(limitEvent);
        await SaveChangesAsync(cancellationToken);
    }

    private async Task<LimitEvent?> FindLatestLimitEventCoreAsync(
        Guid workspaceId,
        Guid usageCycleId,
        BillingDimension dimension,
        CancellationToken cancellationToken)
        => (await LimitEvents
                .Where(item =>
                    item.WorkspaceId == workspaceId &&
                    item.UsageCycleId == usageCycleId &&
                    item.Dimension == dimension)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.OccurredAtUtc)
            .FirstOrDefault();

    public async Task<IReadOnlyList<CycleAdjustment>> ListCycleAdjustmentsAsync(Guid usageCycleId, CancellationToken cancellationToken = default)
        => (await CycleAdjustments
                .Where(item => item.UsageCycleId == usageCycleId)
                .ToListAsync(cancellationToken))
            .OrderByDescending(item => item.RecordedAtUtc)
            .ToList();

    public async Task AddCycleAdjustmentAsync(CycleAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        CycleAdjustments.Add(adjustment);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<AuditRecord?> FindAuditRecordAsync(Guid workspaceId, Guid auditLogId, CancellationToken cancellationToken = default)
        => AuditRecords.FirstOrDefaultAsync(
            item => item.WorkspaceId == workspaceId && item.Id == auditLogId,
            cancellationToken);

    public async Task<IReadOnlyList<AuditRecord>> ListAuditRecordsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = BuildAuditRecordQuery(filter);
        return await query
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .Skip((Math.Max(filter.PageNumber, 1) - 1) * Math.Max(filter.PageSize, 1))
            .Take(Math.Max(filter.PageSize, 1))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAuditRecordsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
        => BuildAuditRecordQuery(filter).CountAsync(cancellationToken);

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

    public Task<AIUsageEvent?> FindAIUsageEventByIdAsync(Guid workspaceId, Guid eventId, CancellationToken cancellationToken = default)
        => AIUsageEvents.FirstOrDefaultAsync(
            item => item.WorkspaceId == workspaceId && item.Id == eventId,
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

    public async Task<decimal> SumAIUsageEventCostsAsync(
        Guid workspaceId,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        CancellationToken cancellationToken = default)
    {
        var items = await BuildAIUsageEventQuery(workspaceId).ToListAsync(cancellationToken);
        return items
            .Where(item => !fromOccurredAt.HasValue || item.OccurredAt >= fromOccurredAt.Value)
            .Where(item => !toOccurredAt.HasValue || item.OccurredAt <= toOccurredAt.Value)
            .Sum(item => item.EstimatedCost ?? 0m);
    }

    public Task<int> CountAcceptedAIUsageEventsAsync(
        Guid workspaceId,
        DateTimeOffset fromOccurredAt,
        DateTimeOffset toOccurredAtExclusive,
        CancellationToken cancellationToken = default)
        => CountAcceptedAIUsageEventsCoreAsync(workspaceId, fromOccurredAt, toOccurredAtExclusive, cancellationToken);

    public async Task<DashboardTotals> GetDashboardTotalsAsync(
        Guid workspaceId,
        ReportingPeriod period,
        CancellationToken cancellationToken = default)
    {
        var events = await LoadReportingEventsAsync(workspaceId, period, cancellationToken);
        var findings = await LoadReportingFindingsAsync(workspaceId, period, cancellationToken);

        return new DashboardTotals
        {
            TotalEvents = events.Count,
            UniqueActorCount = events
                .Select(item => item.ActorUserId)
                .Distinct()
                .Count(),
            UniqueToolCount = events
                .Select(item => item.ToolName)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            FlaggedFindingCount = findings.Count
        };
    }

    private async Task<int> CountAcceptedAIUsageEventsCoreAsync(
        Guid workspaceId,
        DateTimeOffset fromOccurredAt,
        DateTimeOffset toOccurredAtExclusive,
        CancellationToken cancellationToken)
        => (await AIUsageEvents
                .Where(item => item.WorkspaceId == workspaceId)
                .ToListAsync(cancellationToken))
            .Count(item => item.OccurredAt >= fromOccurredAt && item.OccurredAt < toOccurredAtExclusive);

    public async Task<UsageSummaryPage> GetUsageSummaryByUserAsync(
        Guid workspaceId,
        ReportingPeriod period,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var events = await LoadReportingEventsAsync(workspaceId, period, cancellationToken);
        var findings = await LoadReportingFindingsAsync(workspaceId, period, cancellationToken);
        var flaggedByActor = findings
            .GroupBy(item => item.ActorUserId)
            .ToDictionary(group => group.Key, group => group.Count());
        var displayNames = await ResolveUserLabelsAsync(
            workspaceId,
            events.Select(item => item.ActorUserId),
            cancellationToken);

        var rows = events
            .GroupBy(item => item.ActorUserId)
            .Select(group => new UsageSummaryRow
            {
                Dimension = "user",
                ActorUserId = group.Key,
                DisplayLabel = ResolveUserLabel(group.Key, displayNames),
                TotalEvents = group.Count(),
                FlaggedFindingCount = flaggedByActor.GetValueOrDefault(group.Key),
                EstimatedCost = group.Sum(item => item.EstimatedCost ?? 0m),
                EventsWithEstimatedCost = group.Count(item => item.EstimatedCost.HasValue),
                EventsMissingEstimatedCost = group.Count(item => !item.EstimatedCost.HasValue),
                LastActivityAt = group.Max(item => item.OccurredAt)
            })
            .OrderByDescending(item => item.TotalEvents)
            .ThenByDescending(item => item.FlaggedFindingCount)
            .ThenByDescending(item => item.EstimatedCost)
            .ThenBy(item => item.DisplayLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return CreateUsageSummaryPage(rows, pageNumber, pageSize);
    }

    public async Task<UsageSummaryPage> GetUsageSummaryByToolAsync(
        Guid workspaceId,
        ReportingPeriod period,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var events = await LoadReportingEventsAsync(workspaceId, period, cancellationToken);
        var findings = await LoadReportingFindingsAsync(workspaceId, period, cancellationToken);
        var flaggedByTool = findings
            .GroupBy(item => NormalizeLabel(item.ToolName))
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var rows = events
            .GroupBy(item => NormalizeLabel(item.ToolName), StringComparer.OrdinalIgnoreCase)
            .Select(group => new UsageSummaryRow
            {
                Dimension = "tool",
                ActorUserId = null,
                DisplayLabel = group.First().ToolName.Trim(),
                TotalEvents = group.Count(),
                FlaggedFindingCount = flaggedByTool.GetValueOrDefault(group.Key, 0),
                EstimatedCost = group.Sum(item => item.EstimatedCost ?? 0m),
                EventsWithEstimatedCost = group.Count(item => item.EstimatedCost.HasValue),
                EventsMissingEstimatedCost = group.Count(item => !item.EstimatedCost.HasValue),
                LastActivityAt = group.Max(item => item.OccurredAt)
            })
            .OrderByDescending(item => item.TotalEvents)
            .ThenByDescending(item => item.FlaggedFindingCount)
            .ThenByDescending(item => item.EstimatedCost)
            .ThenBy(item => item.DisplayLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return CreateUsageSummaryPage(rows, pageNumber, pageSize);
    }

    public async Task<AlertsSummary> GetAlertsSummaryAsync(
        Guid workspaceId,
        ReportingPeriod period,
        CancellationToken cancellationToken = default)
    {
        var events = await LoadReportingEventsAsync(workspaceId, period, cancellationToken);
        var findings = await LoadReportingFindingsAsync(workspaceId, period, cancellationToken);

        return new AlertsSummary
        {
            WorkspaceId = workspaceId,
            Period = period,
            TotalFindings = findings.Count,
            HighSeverityCount = findings.Count(item => item.Severity == RiskSeverity.High),
            MediumSeverityCount = findings.Count(item => item.Severity == RiskSeverity.Medium),
            LowSeverityCount = findings.Count(item => item.Severity == RiskSeverity.Low),
            AffectedActorCount = findings
                .Select(item => item.ActorUserId)
                .Distinct()
                .Count(),
            AffectedToolCount = findings
                .Select(item => item.ToolName)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            DailyTrend = BuildDailyTrend(period, events, findings)
        };
    }

    public async Task<EstimatedCostSummary> GetEstimatedCostSummaryAsync(
        Guid workspaceId,
        ReportingPeriod period,
        CancellationToken cancellationToken = default)
    {
        var events = await LoadReportingEventsAsync(workspaceId, period, cancellationToken);
        var findings = await LoadReportingFindingsAsync(workspaceId, period, cancellationToken);
        var eventsWithCost = events.Count(item => item.EstimatedCost.HasValue);
        var eventsMissingCost = events.Count - eventsWithCost;

        return new EstimatedCostSummary
        {
            WorkspaceId = workspaceId,
            Period = period,
            EstimatedCostTotal = events.Sum(item => item.EstimatedCost ?? 0m),
            EventsWithEstimatedCost = eventsWithCost,
            EventsMissingEstimatedCost = eventsMissingCost,
            IsPartial = eventsMissingCost > 0,
            DailyTrend = BuildDailyTrend(period, events, findings)
        };
    }

    public Task<RiskFinding?> FindRiskFindingAsync(Guid workspaceId, Guid findingId, CancellationToken cancellationToken = default)
        => RiskFindings.FirstOrDefaultAsync(
            finding => finding.WorkspaceId == workspaceId && finding.Id == findingId,
            cancellationToken);

    public Task<RiskFinding?> FindRiskFindingByEventAndRuleAsync(Guid workspaceId, Guid eventId, RiskRuleType ruleType, CancellationToken cancellationToken = default)
        => RiskFindings.FirstOrDefaultAsync(
            finding => finding.WorkspaceId == workspaceId && finding.EventId == eventId && finding.RuleType == ruleType,
            cancellationToken);

    public async Task AddRiskFindingAsync(RiskFinding riskFinding, CancellationToken cancellationToken = default)
    {
        RiskFindings.Add(riskFinding);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RiskFinding>> ListRiskFindingsAsync(
        Guid workspaceId,
        RiskRuleType? ruleType,
        RiskSeverity? severity,
        RiskFindingStatus? status,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromDetectedAt,
        DateTimeOffset? toDetectedAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = await BuildRiskFindingQuery(workspaceId).ToListAsync(cancellationToken);
        return items
            .Where(item => !ruleType.HasValue || item.RuleType == ruleType.Value)
            .Where(item => !severity.HasValue || item.Severity == severity.Value)
            .Where(item => !status.HasValue || item.Status == status.Value)
            .Where(item => !actorUserId.HasValue || item.ActorUserId == actorUserId.Value)
            .Where(item => string.IsNullOrWhiteSpace(toolName) || item.ToolName.Equals(toolName.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(item => !fromDetectedAt.HasValue || item.DetectedAt >= fromDetectedAt.Value)
            .Where(item => !toDetectedAt.HasValue || item.DetectedAt <= toDetectedAt.Value)
            .OrderByDescending(item => item.DetectedAt)
            .ThenByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<int> CountRiskFindingsAsync(
        Guid workspaceId,
        RiskRuleType? ruleType,
        RiskSeverity? severity,
        RiskFindingStatus? status,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromDetectedAt,
        DateTimeOffset? toDetectedAt,
        CancellationToken cancellationToken = default)
    {
        var items = await BuildRiskFindingQuery(workspaceId).ToListAsync(cancellationToken);
        return items
            .Where(item => !ruleType.HasValue || item.RuleType == ruleType.Value)
            .Where(item => !severity.HasValue || item.Severity == severity.Value)
            .Where(item => !status.HasValue || item.Status == status.Value)
            .Where(item => !actorUserId.HasValue || item.ActorUserId == actorUserId.Value)
            .Where(item => string.IsNullOrWhiteSpace(toolName) || item.ToolName.Equals(toolName.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(item => !fromDetectedAt.HasValue || item.DetectedAt >= fromDetectedAt.Value)
            .Where(item => !toDetectedAt.HasValue || item.DetectedAt <= toDetectedAt.Value)
            .Count();
    }

    public Task<RiskEvaluationOutcome?> FindRiskEvaluationOutcomeByEventIdAsync(Guid workspaceId, Guid eventId, CancellationToken cancellationToken = default)
        => RiskEvaluationOutcomes.FirstOrDefaultAsync(
            outcome => outcome.WorkspaceId == workspaceId && outcome.EventId == eventId,
            cancellationToken);

    public async Task AddRiskEvaluationOutcomeAsync(RiskEvaluationOutcome outcome, CancellationToken cancellationToken = default)
    {
        RiskEvaluationOutcomes.Add(outcome);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<WorkspaceRiskPolicy?> FindWorkspaceRiskPolicyAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => WorkspaceRiskPolicies.FirstOrDefaultAsync(policy => policy.WorkspaceId == workspaceId, cancellationToken);

    public async Task AddWorkspaceRiskPolicyAsync(WorkspaceRiskPolicy policy, CancellationToken cancellationToken = default)
    {
        WorkspaceRiskPolicies.Add(policy);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateWorkspaceRiskPolicyAsync(WorkspaceRiskPolicy policy, CancellationToken cancellationToken = default)
    {
        WorkspaceRiskPolicies.Update(policy);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<NotificationPreference?> FindNotificationPreferenceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => NotificationPreferences.FirstOrDefaultAsync(item => item.WorkspaceId == workspaceId, cancellationToken);

    public async Task<IReadOnlyList<NotificationPreference>> ListNotificationPreferencesAsync(
        bool? urgentAlertsEnabled = null,
        bool? digestEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var query = NotificationPreferences.AsNoTracking().AsQueryable();
        if (urgentAlertsEnabled.HasValue)
        {
            query = query.Where(item => item.UrgentAlertsEnabled == urgentAlertsEnabled.Value);
        }

        if (digestEnabled.HasValue)
        {
            query = query.Where(item => item.DigestEnabled == digestEnabled.Value);
        }

        return await query
            .OrderBy(item => item.WorkspaceId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddNotificationPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        NotificationPreferences.Add(preference);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNotificationPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        NotificationPreferences.Update(preference);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task AddBackgroundJobRunAsync(BackgroundJobRun run, CancellationToken cancellationToken = default)
    {
        BackgroundJobRuns.Add(run);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBackgroundJobRunAsync(BackgroundJobRun run, CancellationToken cancellationToken = default)
    {
        BackgroundJobRuns.Update(run);
        await SaveChangesAsync(cancellationToken);
    }

    public Task<NotificationMessage?> FindNotificationAsync(Guid workspaceId, Guid notificationId, CancellationToken cancellationToken = default)
        => Notifications.FirstOrDefaultAsync(
            item => item.WorkspaceId == workspaceId && item.Id == notificationId,
            cancellationToken);

    public Task<NotificationMessage?> FindNotificationByFingerprintAsync(
        Guid workspaceId,
        NotificationType notificationType,
        string triggerFingerprint,
        CancellationToken cancellationToken = default)
        => Notifications.FirstOrDefaultAsync(
            item => item.WorkspaceId == workspaceId &&
                    item.NotificationType == notificationType &&
                    item.TriggerFingerprint == triggerFingerprint,
            cancellationToken);

    public async Task AddNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        Notifications.Add(notification);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        Notifications.Update(notification);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationMessage>> ListNotificationsAsync(
        Guid workspaceId,
        NotificationType? notificationType,
        NotificationStatus? status,
        DateTimeOffset? fromCreatedAt,
        DateTimeOffset? toCreatedAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var items = await BuildNotificationQuery(workspaceId).ToListAsync(cancellationToken);
        return items
            .Where(item => !notificationType.HasValue || item.NotificationType == notificationType.Value)
            .Where(item => !status.HasValue || item.Status == status.Value)
            .Where(item => !fromCreatedAt.HasValue || item.CreatedAtUtc >= fromCreatedAt.Value)
            .Where(item => !toCreatedAt.HasValue || item.CreatedAtUtc <= toCreatedAt.Value)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public Task<int> CountNotificationsAsync(
        Guid workspaceId,
        NotificationType? notificationType,
        NotificationStatus? status,
        DateTimeOffset? fromCreatedAt,
        DateTimeOffset? toCreatedAt,
        CancellationToken cancellationToken = default)
    {
        return CountNotificationsInMemoryAsync(
            workspaceId,
            notificationType,
            status,
            fromCreatedAt,
            toCreatedAt,
            cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationMessage>> ListNotificationsPendingDeliveryAsync(
        DateTimeOffset asOfUtc,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var notifications = await Notifications
            .Where(item =>
                item.Status == NotificationStatus.Pending ||
                item.Status == NotificationStatus.PartiallyDelivered)
            .ToListAsync(cancellationToken);

        var outcomes = await NotificationDeliveryOutcomes
            .Where(item => item.DeliveryStatus == DeliveryStatus.Pending)
            .ToListAsync(cancellationToken);

        return notifications
            .Where(item =>
            {
                var notificationOutcomes = outcomes
                    .Where(outcome => outcome.NotificationId == item.Id)
                    .ToList();

                return notificationOutcomes.Count == 0 ||
                       notificationOutcomes.Any(outcome =>
                           !outcome.NextAttemptAtUtc.HasValue ||
                           outcome.NextAttemptAtUtc <= asOfUtc);
            })
            .OrderBy(item => item.CreatedAtUtc)
            .Take(maxCount)
            .ToList();
    }

    public async Task AddNotificationDeliveryOutcomeAsync(NotificationDeliveryOutcome outcome, CancellationToken cancellationToken = default)
    {
        NotificationDeliveryOutcomes.Add(outcome);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNotificationDeliveryOutcomeAsync(NotificationDeliveryOutcome outcome, CancellationToken cancellationToken = default)
    {
        NotificationDeliveryOutcomes.Update(outcome);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDeliveryOutcome>> ListNotificationDeliveryOutcomesAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
        => await NotificationDeliveryOutcomes
            .Where(item => item.NotificationId == notificationId)
            .OrderBy(item => item.RecipientAddress)
            .ThenBy(item => item.RecipientUserId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NotificationDeliveryOutcome>> ListDueNotificationRetryOutcomesAsync(
        DateTimeOffset asOfUtc,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var outcomes = await NotificationDeliveryOutcomes
            .Where(item =>
                item.DeliveryStatus == DeliveryStatus.RetryScheduled)
            .ToListAsync(cancellationToken);
        return outcomes
            .Where(item => item.NextAttemptAtUtc.HasValue && item.NextAttemptAtUtc <= asOfUtc)
            .OrderBy(item => item.NextAttemptAtUtc)
            .Take(maxCount)
            .ToList();
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        NotificationDeliveryOutcomes.RemoveRange(NotificationDeliveryOutcomes);
        Notifications.RemoveRange(Notifications);
        BackgroundJobRuns.RemoveRange(BackgroundJobRuns);
        CycleAdjustments.RemoveRange(CycleAdjustments);
        LimitEvents.RemoveRange(LimitEvents);
        UsageCycleMetrics.RemoveRange(UsageCycleMetrics);
        UsageCycles.RemoveRange(UsageCycles);
        WorkspacePlanAssignments.RemoveRange(WorkspacePlanAssignments);
        PlanLimitRules.RemoveRange(PlanLimitRules);
        PlanDefinitions.RemoveRange(PlanDefinitions);
        NotificationPreferences.RemoveRange(NotificationPreferences);
        RiskFindings.RemoveRange(RiskFindings);
        RiskEvaluationOutcomes.RemoveRange(RiskEvaluationOutcomes);
        WorkspaceRiskPolicies.RemoveRange(WorkspaceRiskPolicies);
        AuditRecords.RemoveRange(AuditRecords);
        AIUsageEvents.RemoveRange(AIUsageEvents);
        WorkspaceMemberships.RemoveRange(WorkspaceMemberships);
        Workspaces.RemoveRange(Workspaces);
        Users.RemoveRange(Users);
        await SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AIUsageEvent> BuildAIUsageEventQuery(Guid workspaceId)
        => AIUsageEvents.Where(item => item.WorkspaceId == workspaceId);

    private IQueryable<AuditRecord> BuildAuditRecordQuery(AuditLogFilter filter)
    {
        var query = AuditRecords.AsNoTracking().AsQueryable();

        if (filter.WorkspaceId != Guid.Empty)
        {
            query = query.Where(item => item.WorkspaceId == filter.WorkspaceId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActionType))
        {
            var actionType = filter.ActionType.Trim();
            query = query.Where(item => item.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Result))
        {
            var result = filter.Result.Trim();
            query = query.Where(item => item.Result == result);
        }

        if (filter.ActorUserId.HasValue)
        {
            query = query.Where(item => item.ActorUserId == filter.ActorUserId.Value);
        }

        if (filter.IsSecurityRelevant.HasValue)
        {
            query = query.Where(item => item.IsSecurityRelevant == filter.IsSecurityRelevant.Value);
        }

        if (filter.FromOccurredAtUtc.HasValue)
        {
            query = query.Where(item => item.OccurredAt >= filter.FromOccurredAtUtc.Value);
        }

        if (filter.ToOccurredAtUtc.HasValue)
        {
            query = query.Where(item => item.OccurredAt <= filter.ToOccurredAtUtc.Value);
        }

        return query;
    }

    private IQueryable<RiskFinding> BuildRiskFindingQuery(Guid workspaceId)
        => RiskFindings.Where(item => item.WorkspaceId == workspaceId);

    private IQueryable<NotificationMessage> BuildNotificationQuery(Guid workspaceId)
        => Notifications
            .AsNoTracking()
            .Where(item => item.WorkspaceId == workspaceId);

    private async Task<List<AIUsageEvent>> LoadReportingEventsAsync(
        Guid workspaceId,
        ReportingPeriod period,
        CancellationToken cancellationToken)
    {
        var items = await BuildAIUsageEventQuery(workspaceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return items
            .Where(item => item.OccurredAt >= period.NormalizedFromUtc && item.OccurredAt <= period.NormalizedToUtc)
            .ToList();
    }

    private async Task<List<RiskFinding>> LoadReportingFindingsAsync(
        Guid workspaceId,
        ReportingPeriod period,
        CancellationToken cancellationToken)
    {
        var items = await BuildRiskFindingQuery(workspaceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return items
            .Where(item => item.DetectedAt >= period.NormalizedFromUtc && item.DetectedAt <= period.NormalizedToUtc)
            .ToList();
    }

    private async Task<Dictionary<Guid, string>> ResolveUserLabelsAsync(
        Guid workspaceId,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var actorIds = userIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();
        if (actorIds.Count == 0)
        {
            return [];
        }

        var memberIds = await WorkspaceMemberships
            .AsNoTracking()
            .Where(item => item.WorkspaceId == workspaceId && actorIds.Contains(item.UserId))
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await Users
            .AsNoTracking()
            .Where(item => memberIds.Contains(item.Id))
            .ToDictionaryAsync(
                item => item.Id,
                item => string.IsNullOrWhiteSpace(item.DisplayName) ? item.Email ?? item.Id.ToString() : item.DisplayName,
                cancellationToken);
    }

    private static string ResolveUserLabel(Guid actorUserId, IReadOnlyDictionary<Guid, string> displayNames)
    {
        if (displayNames.TryGetValue(actorUserId, out var displayName))
        {
            return displayName;
        }

        return actorUserId.ToString();
    }

    private static UsageSummaryPage CreateUsageSummaryPage(
        IReadOnlyList<UsageSummaryRow> rows,
        int pageNumber,
        int pageSize)
    {
        return new UsageSummaryPage
        {
            Items = rows
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = rows.Count
        };
    }

    private static IReadOnlyList<DailyTrendPoint> BuildDailyTrend(
        ReportingPeriod period,
        IReadOnlyList<AIUsageEvent> events,
        IReadOnlyList<RiskFinding> findings)
    {
        var eventCounts = events
            .GroupBy(item => DateOnly.FromDateTime(item.OccurredAt.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.Count());
        var findingCounts = findings
            .GroupBy(item => DateOnly.FromDateTime(item.DetectedAt.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.Count());
        var costTotals = events
            .GroupBy(item => DateOnly.FromDateTime(item.OccurredAt.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.Sum(item => item.EstimatedCost ?? 0m));
        var trend = new List<DailyTrendPoint>(period.DayCount);

        for (var date = period.FromDate; date <= period.ToDate; date = date.AddDays(1))
        {
            trend.Add(new DailyTrendPoint
            {
                Date = date,
                EventCount = eventCounts.GetValueOrDefault(date),
                FindingCount = findingCounts.GetValueOrDefault(date),
                EstimatedCost = costTotals.GetValueOrDefault(date)
            });
        }

        return trend;
    }

    private static string NormalizeLabel(string value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();

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

    private async Task<int> CountNotificationsInMemoryAsync(
        Guid workspaceId,
        NotificationType? notificationType,
        NotificationStatus? status,
        DateTimeOffset? fromCreatedAt,
        DateTimeOffset? toCreatedAt,
        CancellationToken cancellationToken)
    {
        var items = await BuildNotificationQuery(workspaceId).ToListAsync(cancellationToken);
        return items
            .Where(item => !notificationType.HasValue || item.NotificationType == notificationType.Value)
            .Where(item => !status.HasValue || item.Status == status.Value)
            .Where(item => !fromCreatedAt.HasValue || item.CreatedAtUtc >= fromCreatedAt.Value)
            .Where(item => !toCreatedAt.HasValue || item.CreatedAtUtc <= toCreatedAt.Value)
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
            LastSignInAt = entity.LastSignInAt,
            FailedSignInCount = entity.FailedSignInCount,
            LastFailedSignInAt = entity.LastFailedSignInAt,
            LockedUntilUtc = entity.LockedUntilUtc
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
            FailedSignInCount = model.FailedSignInCount,
            LastFailedSignInAt = model.LastFailedSignInAt,
            LockedUntilUtc = model.LockedUntilUtc,
            EmailConfirmed = true
        };
    }

    private static void UpdateUserEntity(ApplicationUser entity, UserAccount user)
    {
        entity.Email = user.Email;
        entity.NormalizedEmail = user.Email.ToUpperInvariant();
        entity.UserName = user.Email;
        entity.NormalizedUserName = user.Email.ToUpperInvariant();
        entity.DisplayName = user.DisplayName;
        entity.PasswordHash = user.PasswordHash;
        entity.Status = user.Status;
        entity.CreatedAt = user.CreatedAt;
        entity.LastSignInAt = user.LastSignInAt;
        entity.FailedSignInCount = user.FailedSignInCount;
        entity.LastFailedSignInAt = user.LastFailedSignInAt;
        entity.LockedUntilUtc = user.LockedUntilUtc;
    }
}
