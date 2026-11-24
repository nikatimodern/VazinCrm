using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.Taxation;

public sealed record TaxInvoiceFilter(string Search, int? InvoiceNumber, bool? Export, TaxDispatchStatus? Status,
    bool Unqueued, int Page, int PageSize);

public sealed record TaxInvoiceDocument(Guid Id, Guid FactorId, TaxDispatchKind Kind, TaxDispatchStatus Status,
    string TaxId, string ReferenceTaxId, int? ReferenceInvoiceNumber);

public sealed record TaxInvoiceListItem(Guid Id, int Number, string Customer, DateTime? Date, decimal Amount,
    string Currency, bool IsExport, bool IsReturn, IReadOnlyList<TaxInvoiceDocument> Documents);

public sealed record TaxInvoicePage(IReadOnlyList<TaxInvoiceListItem> Items, int Page, int PageCount, int Total);

public interface ITaxInvoiceListing
{
    Task<TaxInvoicePage> ListAsync(TaxInvoiceFilter filter, CancellationToken ct);
    Task<TaxDispatchDetails> ReadDispatchAsync(Guid id, CancellationToken ct);
}
