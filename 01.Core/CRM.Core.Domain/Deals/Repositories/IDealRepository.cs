#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Deals.Enums;

namespace CRM.Core.Domain.Deals.Repositories;

public interface IDealRepository
{
    Task<Deal?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<OverdueFollowUpUpdateResult> UpdateOverdueFollowUpAsync(Guid id, DateTime? followUpAt,
        DealStatusEnum status, string note, string userId, string userName,
        CancellationToken cancellationToken);
}

public enum OverdueFollowUpUpdateResult
{
    Updated,
    NotFound,
    HasFactors
}
