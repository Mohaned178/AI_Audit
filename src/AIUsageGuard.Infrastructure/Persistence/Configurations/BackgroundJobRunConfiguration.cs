using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class BackgroundJobRunConfiguration : IEntityTypeConfiguration<BackgroundJobRun>
{
    public void Configure(EntityTypeBuilder<BackgroundJobRun> builder)
    {
        builder.ToTable("background_job_runs");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.JobType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.FailureSummary)
            .HasMaxLength(2000);

        builder.HasIndex(item => new { item.JobType, item.ScheduledForUtc });
        builder.HasIndex(item => new { item.WorkspaceId, item.JobType, item.StartedAtUtc });
    }
}
