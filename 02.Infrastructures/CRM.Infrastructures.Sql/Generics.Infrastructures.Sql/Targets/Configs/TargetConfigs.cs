using CRM.Core.Domain.Targets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructures.Sql.Targets.Configs;

public sealed class TargetMetricConfig : IEntityTypeConfiguration<TargetMetric>
{
    public void Configure(EntityTypeBuilder<TargetMetric> b)
    {
        b.ToTable("TargetMetrics");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.Title).HasMaxLength(120).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(30).IsRequired();
        b.Property(x => x.Kind).HasConversion<byte>();
        b.HasIndex(x => x.SortOrder);
    }
}

public sealed class TargetPlanConfig : IEntityTypeConfiguration<TargetPlan>
{
    public void Configure(EntityTypeBuilder<TargetPlan> b)
    {
        b.ToTable("TargetPlans");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Year).IsUnique();
        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.CreatedById).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedById).HasMaxLength(450).IsRequired();
        b.HasMany(x => x.Values).WithOne(x => x.Plan).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Assignments).WithOne(x => x.Plan).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Audits).WithOne(x => x.Plan).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TargetValueConfig : IEntityTypeConfiguration<TargetValue>
{
    public void Configure(EntityTypeBuilder<TargetValue> b)
    {
        b.ToTable("TargetValues", t => t.HasCheckConstraint("CK_TargetValues_Month", "[Month] >= 0 AND [Month] <= 12"));
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PlanId, x.MetricCode, x.Month }).IsUnique();
        b.Property(x => x.MetricCode).HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.Value).HasPrecision(28, 2);
        b.HasOne(x => x.Metric).WithMany().HasForeignKey(x => x.MetricCode).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TargetAssignmentConfig : IEntityTypeConfiguration<TargetAssignment>
{
    public void Configure(EntityTypeBuilder<TargetAssignment> b)
    {
        b.ToTable("TargetAssignments", t => t.HasCheckConstraint("CK_TargetAssignments_Month", "[Month] >= 1 AND [Month] <= 12"));
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PlanId, x.MetricCode, x.Month, x.UserId }).IsUnique();
        b.Property(x => x.MetricCode).HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Value).HasPrecision(28, 2);
        b.HasOne(x => x.Metric).WithMany().HasForeignKey(x => x.MetricCode).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TargetPlanAuditConfig : IEntityTypeConfiguration<TargetPlanAudit>
{
    public void Configure(EntityTypeBuilder<TargetPlanAudit> b)
    {
        b.ToTable("TargetPlanAudits");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PlanId, x.Version }).IsUnique();
        b.Property(x => x.Action).HasMaxLength(40).IsRequired();
        b.Property(x => x.ActorId).HasMaxLength(450).IsRequired();
    }
}
