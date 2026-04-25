using System.Text.Json;
using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceRiskPolicyConfiguration : IEntityTypeConfiguration<WorkspaceRiskPolicy>
{
    public void Configure(EntityTypeBuilder<WorkspaceRiskPolicy> builder)
    {
        builder.ToTable("workspace_risk_policies");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ApprovedTools)
            .HasColumnType("text")
            .HasConversion(
                value => JsonSerializer.Serialize(NormalizeTools(value)),
                value => DeserializeTools(value))
            .Metadata.SetValueComparer(CreateComparer());

        builder.Property(item => item.PerEventEstimatedCostThreshold)
            .HasPrecision(18, 4);

        builder.Property(item => item.DailyEstimatedCostThreshold)
            .HasPrecision(18, 4);

        builder.HasIndex(item => item.WorkspaceId)
            .IsUnique();

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(item => item.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static IReadOnlyList<string> NormalizeTools(IEnumerable<string> values)
        => values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<string> DeserializeTools(string json)
        => string.IsNullOrWhiteSpace(json)
            ? []
            : NormalizeTools(JsonSerializer.Deserialize<List<string>>(json) ?? []).ToList();

    private static ValueComparer<List<string>> CreateComparer()
    {
        return new ValueComparer<List<string>>(
            (left, right) => SequenceEqual(left, right),
            value => GetToolsHashCode(value),
            value => SnapshotTools(value));
    }

    private static bool SequenceEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        var normalizedLeft = NormalizeTools(left);
        var normalizedRight = NormalizeTools(right);
        return normalizedLeft.SequenceEqual(normalizedRight, StringComparer.OrdinalIgnoreCase);
    }

    private static int GetToolsHashCode(IReadOnlyList<string>? value)
    {
        if (value is null)
        {
            return 0;
        }

        return NormalizeTools(value)
            .Aggregate(0, (hash, item) => HashCode.Combine(hash, StringComparer.OrdinalIgnoreCase.GetHashCode(item)));
    }

    private static List<string> SnapshotTools(IReadOnlyList<string>? value)
        => value is null ? new List<string>() : value.ToList();
}
