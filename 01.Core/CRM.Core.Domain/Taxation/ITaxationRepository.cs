using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Factors.Entities;

namespace CRM.Core.Domain.Taxation;

public sealed record TaxPreviewSource(Factor Factor, TaxMemory ActiveMemory, IReadOnlyList<TaxProductMapping> Mappings, bool? TaxAfterDiscount);

public sealed record TaxReferenceSource(TaxDispatch Reference, string Preview,
    string Payload, string WorkFolderEvidence, TaxPreviewSource Candidate);

public sealed record TaxReferenceRegistration(string Preview, string Payload, string TaxId,
    long Serial, TaxSigningBinding Binding);

public sealed class TaxPreviewConflictException : InvalidOperationException
{
    public TaxPreviewConflictException() : base("اطلاعات پیش‌نمایش تغییر کرده است. پیش‌نمایش جدید دریافت کنید.") { }
    public TaxPreviewConflictException(string message) : base(message) { }
}

public sealed class TaxFullReturnRequiresCancellationException(Guid referenceId)
    : ArgumentException("برای برگشت تمام اقلام باید سند ابطالی صادر شود.")
{
    public Guid ReferenceId { get; } = referenceId;
}

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
    Task DeactivateAsync(Guid id, CancellationToken ct);
    Task<TaxProductPage> ListProductsAsync(string search, bool unmapped, int page, int size, CancellationToken ct);
    Task<TaxPreviewSource> GetPreviewSourceAsync(Guid factorId, CancellationToken ct);
    Task<string> ReadSnapshotAsync(Guid id, CancellationToken ct);
    Task<Guid> SaveSnapshotAsync(Guid factorId, string actorId, Func<TaxPreviewSource, string> validate, CancellationToken ct);
    Task<Guid> EnqueueReferenceAsync(Guid referenceId, Guid? factorId, TaxDispatchKind kind, string actorId,
        Func<TaxReferenceSource, CancellationToken, Task<TaxReferenceRegistration>> register, CancellationToken ct);
    Task<Guid> EnqueueAsync(Guid factorId, string actorId, Func<TaxPreviewSource, string> validate, CancellationToken ct);
}
