#nullable enable
using System;
using System.Collections.Generic;
using CRM.Core.Domain.Targets.Entities;

namespace CRM.Core.Domain.Targets.Models;

public sealed class TargetAmountInput
{
    public string MetricCode { get; set; } = "";
    public int Month { get; set; }
    public string? UserId { get; set; }
    public decimal Value { get; set; }
}

public class TargetSaveRequest
{
    public Guid? Id { get; set; }
    public int Year { get; set; }
    public long Version { get; set; }
    public List<TargetAmountInput> Yearly { get; set; } = [];
    public List<TargetAmountInput> Monthly { get; set; } = [];
    public List<TargetAmountInput> Assignments { get; set; } = [];
}

public sealed class TargetExpert
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class TargetSetup
{
    public IReadOnlyList<TargetMetric> Metrics { get; set; } = [];
    public IReadOnlyList<TargetExpert> Experts { get; set; } = [];
}

public sealed class TargetPlanSummary
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public TargetPlanStatus Status { get; set; }
    public decimal SalesTarget { get; set; }
    public long Version { get; set; }
    public int AllocatedMonths { get; set; }
    public int ExpertCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class TargetPlanDetails : TargetSaveRequest
{
    public TargetPlanStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed record TargetSaveResult(int StatusCode, string Message, Guid? Id = null, long Version = 0);
