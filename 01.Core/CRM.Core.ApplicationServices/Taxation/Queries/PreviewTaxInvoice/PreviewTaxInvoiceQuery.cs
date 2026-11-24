using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;
using CRM.Core.ApplicationServices.Taxation.Dtos;

namespace CRM.Core.ApplicationServices.Taxation.Queries.PreviewTaxInvoice;

public sealed record PreviewTaxInvoiceQuery(Guid FactorId) : IQuery<InvoicePreview>;

public sealed class PreviewTaxInvoiceQueryHandler(ITaxationRepository repository) : IQueryHandler<PreviewTaxInvoiceQuery, InvoicePreview>
{
    public async Task<InvoicePreview> HandleAsync(PreviewTaxInvoiceQuery request, CancellationToken ct)
    {
        var source = await repository.GetPreviewSourceAsync(request.FactorId, ct);
        return InvoicePreview.Build(source);
    }
}
