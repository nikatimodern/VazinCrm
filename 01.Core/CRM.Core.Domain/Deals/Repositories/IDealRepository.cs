#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Deals.Entities;

namespace CRM.Core.Domain.Deals.Repositories;

public interface IDealRepository
{
    Task<Deal?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
}
