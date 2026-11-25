using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxInvoiceBatch;

public sealed record TaxInvoiceBatchResult(Guid FactorId, Guid? DispatchId, IReadOnlyList<string> Errors);

public sealed record EnqueueTaxInvoiceBatchCommand(IReadOnlyList<Guid> FactorIds, string ActorId)
    : ICommand<IReadOnlyList<TaxInvoiceBatchResult>>;

public sealed class EnqueueTaxInvoiceBatchCommandHandler(ITaxationRepository repository)
    : ICommandHandler<EnqueueTaxInvoiceBatchCommand, IReadOnlyList<TaxInvoiceBatchResult>>
{
    public async Task<IReadOnlyList<TaxInvoiceBatchResult>> HandleAsync(EnqueueTaxInvoiceBatchCommand request, CancellationToken ct)
    {
        if (request.FactorIds is not { Count: > 0 and <= 25 } || request.FactorIds.Contains(Guid.Empty))
            throw new ArgumentException("در هر مرحله بین یک تا ۲۵ فاکتور انتخاب کنید.");
        var results = new List<TaxInvoiceBatchResult>();
        foreach (var id in request.FactorIds.Distinct())
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<string> errors = [];
            try
            {
                var dispatch = await repository.EnqueueAsync(id, request.ActorId, source =>
                {
                    var preview = InvoicePreview.Build(source);
                    errors = preview.Errors;
                    return SaveTaxPreviewCommandHandler.SerializeReady(preview);
                }, ct);
                results.Add(new(id, dispatch, []));
            }
            catch (KeyNotFoundException)
            {
                results.Add(new(id, null, ["فاکتور در دسترس نیست."]));
            }
            catch (ArgumentException exception)
            {
                results.Add(new(id, null, errors.Count == 0 ? [exception.Message] : errors));
            }
        }
        return results;
    }
}
