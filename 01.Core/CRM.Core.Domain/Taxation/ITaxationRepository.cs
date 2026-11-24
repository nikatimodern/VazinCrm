using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Factors.Entities;

namespace CRM.Core.Domain.Taxation;

public sealed record TaxPreviewSource(Factor Factor, TaxMemory ActiveMemory, IReadOnlyList<TaxProductMapping> Mappings);

public interface ITaxPreviewProtector
{
    string Protect(string preview);
    string Unprotect(string preview);
}

public interface ITaxationRepository
{
    Task<IReadOnlyList<TaxMemory>> ListMemoriesAsync(CancellationToken ct);
    Task<Guid> SaveMemoryAsync(Guid? id, string fiscalId, string title, CancellationToken ct);
    Task SetMappingAsync(Guid productId, int unit, string goodsId, string taxUnit, CancellationToken ct);
    Task ActivateAsync(Guid id, CancellationToken ct);
    Task<TaxPreviewSource> GetPreviewSourceAsync(Guid factorId, CancellationToken ct);
    Task<string> ReadSnapshotAsync(Guid id, CancellationToken ct);
    Task<Guid> SaveSnapshotAsync(Guid factorId, Guid memoryId, string preview, string actorId, CancellationToken ct);
}
