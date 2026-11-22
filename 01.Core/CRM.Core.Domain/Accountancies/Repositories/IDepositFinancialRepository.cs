using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;

namespace CRM.Core.Domain.Accountancies.Repositories;

public interface IDepositFinancialRepository
{
    Task<Accountancy?> GetDepositAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ExternalSoftware>> GetDestinationsAsync(CancellationToken ct);
    Task<List<ExternalMapping>> GetMappingsAsync(Guid id, Guid? customerId, Guid? bankId, CancellationToken ct);
    Task SaveMappingAsync(ExternalMapping mapping, CancellationToken ct);
    Task DeleteMappingAsync(ExternalMapping mapping, CancellationToken ct);
    Task<IAsyncDisposable> LockAsync(Guid id, CancellationToken ct);
}
