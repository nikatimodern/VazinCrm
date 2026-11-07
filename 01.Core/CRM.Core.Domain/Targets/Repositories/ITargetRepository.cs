#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Targets.Entities;
using CRM.Core.Domain.Targets.Models;

namespace CRM.Core.Domain.Targets.Repositories;

public interface ITargetRepository
{
    Task<IReadOnlyList<TargetMetric>> GetMetricsAsync(CancellationToken ct);
    Task<IReadOnlyList<TargetExpert>> GetExpertsAsync(string role, CancellationToken ct);
    Task<IReadOnlyList<TargetPlanSummary>> ListAsync(CancellationToken ct);
    Task<TargetPlanDetails?> GetAsync(Guid id, CancellationToken ct);
    Task<TargetSaveResult> SaveAsync(TargetSaveRequest request, bool finalize, string actorId, CancellationToken ct);
}
