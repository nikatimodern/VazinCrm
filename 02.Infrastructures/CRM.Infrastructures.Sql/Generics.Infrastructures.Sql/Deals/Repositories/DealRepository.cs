#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Deals.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Deals.Repositories;

public sealed class DealRepository(CRMContextDb db) : IDealRepository
{
    public Task<Deal?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Deals.FirstOrDefaultAsync(deal => deal.Id == id, cancellationToken);
    }
}
