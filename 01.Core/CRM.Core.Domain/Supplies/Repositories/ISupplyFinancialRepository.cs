using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.Supplies.Entities;

namespace CRM.Core.Domain.Supplies.Repositories;

public interface ISupplyFinancialRepository
{
    Task<SupplyFactor> GetSourceAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ExternalSoftware>> GetDestinationsAsync(CancellationToken ct);
    Task<IReadOnlyList<ExternalMapping>> GetMappingsAsync(Guid sourceId, CancellationToken ct);
    Task<ExternalMapping> GetMappingAsync(Guid sourceId, Guid destinationId, ExternalMappingTypeEnum type, CancellationToken ct);
    Task<string> GetCreatorIdAsync(Guid destinationId, CancellationToken ct);
    Task SaveMappingAsync(ExternalMapping mapping, CancellationToken ct);
    Task<IAsyncDisposable> LockAsync(Guid sourceId, CancellationToken ct);
}
