using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Accountancies.Repositories;

public sealed class DepositFinancialRepository(CRMContextDb db) : IDepositFinancialRepository
{
    public Task<Accountancy?> GetDepositAsync(Guid id, CancellationToken ct) =>
        db.Accountancies.AsNoTracking().Include(x => x.AccountancyTransfer)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ExternalSoftware>> GetDestinationsAsync(CancellationToken ct) =>
        await db.ExternalSoftwares.AsNoTracking().OrderBy(x => x.Title).ToListAsync(ct);

    public Task<List<ExternalMapping>> GetMappingsAsync(Guid id, Guid? customerId, Guid? bankId, CancellationToken ct) =>
        db.ExternalMappings.AsNoTracking().Where(x =>
            (x.EntityId == id && x.MapType == ExternalMappingTypeEnum.واریزی && x.EntityName == nameof(Accountancy))
            || (x.EntityId == customerId && (x.MapType == ExternalMappingTypeEnum.مشتری
                || x.MapType == ExternalMappingTypeEnum.تامین_کننده) && x.EntityName == "Customer")
            || (x.EntityId == bankId && x.MapType == ExternalMappingTypeEnum.حساب_بانکی && x.EntityName == nameof(BankAccount)))
            .ToListAsync(ct);

    public async Task SaveMappingAsync(ExternalMapping mapping, CancellationToken ct)
    {
        if (db.Entry(mapping).State == EntityState.Detached)
        {
            var stored = await db.ExternalMappings.SingleOrDefaultAsync(x => x.Id == mapping.Id, ct);
            if (stored == null) db.ExternalMappings.Add(mapping);
            else stored.ExEntityId = mapping.ExEntityId;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteMappingAsync(ExternalMapping mapping, CancellationToken ct)
    {
        db.ExternalMappings.Remove(mapping);
        await db.SaveChangesAsync(ct);
    }

    public Task<IAsyncDisposable> LockAsync(Guid id, CancellationToken ct) => ExternalSoftwares.FinancialMappingLock.AcquireAsync(db, id, ct);
}
