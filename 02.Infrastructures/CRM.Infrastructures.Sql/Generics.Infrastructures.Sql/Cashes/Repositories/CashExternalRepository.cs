#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Cashes.Repositories;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.ASettings.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Cashes.Repositories;
public sealed class CashExternalRepository(CRMContextDb db, Sepidar1ContextDb first, Sepidar2ContextDb second) : ICashExternalRepository
{
    public Task<List<ExternalSoftware>> GetAccountingSoftwareAsync(CancellationToken ct) => db.ExternalSoftwares.AsNoTracking().Where(s => s.SoftwareType == SoftwareTypeEnum.حسابداری).ToListAsync(ct);
    public Task<List<CashOption<string>>> GetDetailedAccountsAsync(int code, CancellationToken ct)
    {
        if (code == 1)
            return first.DLs.AsNoTracking().Where(d => d.Type == 4 || d.Type == 2).OrderBy(d => d.Title).Select(d => new CashOption<string>(d.Code, d.Title)).ToListAsync(ct);
        if (code == 2)
            return second.DLs.AsNoTracking().Where(d => d.Type == 4 || d.Type == 2).OrderBy(d => d.Title).Select(d => new CashOption<string>(d.Code, d.Title)).ToListAsync(ct);
        return Task.FromResult(new List<CashOption<string>>());
    }

    public Task<string?> GetCashAccountAsync(Guid id, CancellationToken ct) => db.ExternalSoftwareSettings.AsNoTracking().Where(s => s.SettingNameCode == ExternalSoftwareSettingTypeEnum.کد_تفصیلی_صندوق && s.ExternalSoftwareId == id).Select(s => s.SettingValue).FirstOrDefaultAsync(ct);
    public Task<bool> IsMappedAsync(Guid cash, Guid software, CancellationToken ct) => db.ExternalMappings.AnyAsync(m => m.EntityId == cash && m.ExternalSoftwareId == software && m.MapType == ExternalMappingTypeEnum.تنخواه, ct);
    public Task<string?> GetCostCenterMappingAsync(Guid? id, Guid software, CancellationToken ct) => db.ExternalMappings.AsNoTracking().Where(m => m.MapType == ExternalMappingTypeEnum.مرکز_هزینه && m.EntityName == nameof(BaseCenterCash) && m.EntityId == id && m.ExternalSoftwareId == software).Select(m => m.ExEntityId).FirstOrDefaultAsync(ct);
    public void AddMapping(ExternalMapping mapping) => db.ExternalMappings.Add(mapping);
}
