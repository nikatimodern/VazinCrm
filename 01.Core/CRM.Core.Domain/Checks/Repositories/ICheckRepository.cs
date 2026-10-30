#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Checks.Entities;

namespace CRM.Core.Domain.Checks.Repositories;
public sealed record CheckbookInfo(Checkbook Checkbook, int ChecksCount);
public interface ICheckRepository
{
    Task<List<CheckbookInfo>> GetCheckbooksAsync(CancellationToken cancellationToken);
    Task<List<Check>> GetChecksAsync(Guid checkbookId, CancellationToken cancellationToken);
    Task<Check?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<Checkbook?> GetCheckbookAsync(Guid id, CancellationToken cancellationToken);
    void AddCheckbook(Checkbook checkbook);
    Task SaveCheckAsync(Check check, bool create, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
