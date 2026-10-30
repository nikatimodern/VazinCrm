#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ExternalSoftwares.Entites;

namespace CRM.Core.Domain.Cashes.Repositories;
public interface ICashExternalRepository
{
    Task<List<ExternalSoftware>> GetAccountingSoftwareAsync(CancellationToken ct);
    Task<List<CashOption<string>>> GetDetailedAccountsAsync(int softwareCode, CancellationToken ct);
    Task<string?> GetCashAccountAsync(Guid softwareId, CancellationToken ct);
    Task<bool> IsMappedAsync(Guid cashId, Guid softwareId, CancellationToken ct);
    Task<string?> GetCostCenterMappingAsync(Guid? centerId, Guid softwareId, CancellationToken ct);
    void AddMapping(ExternalMapping mapping);
}
