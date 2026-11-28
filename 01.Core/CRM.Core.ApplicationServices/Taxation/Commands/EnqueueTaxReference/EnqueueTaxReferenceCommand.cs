using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;

public enum TaxCancellationReason
{
    BuyerInformationChanged = 1,
    FullReturn = 2,
    TransactionDidNotOccur = 3,
    IncorrectTransactionDate = 4
}

public sealed record TaxReferenceRequest(Guid? FactorId, TaxDispatchKind Kind, long Serial,
    DateTimeOffset IssuedAt, int? Settlement, bool CorrectsQuantityError, TaxCancellationReason? CancellationReason,
    ExportTaxDetails? Export = null, TaxSettlementShares? Shares = null);

public sealed record EnqueueTaxReferenceCommand(Guid ReferenceId, string ActorId, TaxReferenceRequest Reference) : ICommand<Guid>;

public sealed class EnqueueTaxReferenceCommandHandler(ITaxationRepository repository, ITaxSigningBindings bindings, TimeProvider clock)
    : ICommandHandler<EnqueueTaxReferenceCommand, Guid>
{
    public Task<Guid> HandleAsync(EnqueueTaxReferenceCommand request, CancellationToken ct)
    {
        var data = request.Reference;
        if (data.Kind is not (TaxDispatchKind.Correction or TaxDispatchKind.Cancellation or TaxDispatchKind.Return) ||
            data.IssuedAt.Offset != TimeSpan.FromMinutes(210) || data.IssuedAt > clock.GetUtcNow() ||
            (data.Kind == TaxDispatchKind.Cancellation ? data.FactorId.HasValue : data.FactorId.GetValueOrDefault() == Guid.Empty))
            throw new ArgumentException("نوع سند، فاکتور یا زمان صدور سند ارجاعی معتبر نیست.");

        return repository.EnqueueReferenceAsync(request.ReferenceId, data.FactorId, data.Kind, request.ActorId,
            async (source, token) => TaxReferenceInvoice.Build(source, data, await bindings.GetAsync(source.Reference.MemoryId, token)), ct);
    }
}
