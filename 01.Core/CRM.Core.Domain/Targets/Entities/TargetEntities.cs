#nullable enable
using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.Targets.Entities;

public enum TargetValueKind : byte { Amount = 0, Count = 1, Percentage = 2 }
public enum TargetPlanStatus : byte { Draft = 0, Final = 1 }

public sealed class TargetMetric
{
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Unit { get; set; } = "";
    public TargetValueKind Kind { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TargetPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public TargetPlanStatus Status { get; set; } = TargetPlanStatus.Draft;
    public long Version { get; set; } = 1;
    public string CreatedById { get; set; } = "";
    public string ModifiedById { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAtUtc { get; set; }
    public List<TargetValue> Values { get; set; } = [];
    public List<TargetAssignment> Assignments { get; set; } = [];
    public List<TargetPlanAudit> Audits { get; set; } = [];
}

public sealed class TargetValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public TargetPlan Plan { get; set; } = null!;
    public string MetricCode { get; set; } = "";
    public TargetMetric Metric { get; set; } = null!;
    // Month 0 = annual company target; months 1..12 = monthly targets.
    public int Month { get; set; }
    public decimal Value { get; set; }
}

public sealed class TargetAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public TargetPlan Plan { get; set; } = null!;
    public string MetricCode { get; set; } = "";
    public TargetMetric Metric { get; set; } = null!;
    public int Month { get; set; }
    // Identity users are stored in UserContextDb, so no cross-database FK.
    public string UserId { get; set; } = "";
    public decimal Value { get; set; }
}

public sealed class TargetPlanAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public TargetPlan Plan { get; set; } = null!;
    public long Version { get; set; }
    public string Action { get; set; } = "";
    public string ActorId { get; set; } = "";
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
}
