using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed class TaxInvoiceListing(CRMContextDb db) : ITaxInvoiceListing
{
    public async Task<TaxInvoicePage> ListAsync(TaxInvoiceFilter filter, CancellationToken ct)
    {
        var dispatches = db.TaxDispatches.AsNoTracking();
        var factors = db.Factors.AsNoTracking();
        var candidates = factors.Where(factor => !factor.IsDeleted &&
            ((factor.OfficialType == 1 && factor.status == FactorStatus.تایید_شده &&
                factor.ParentReturnedId == null) || dispatches.Any(x => x.FactorId == factor.Id)));
        if (filter.Search.Length > 0)
            candidates = candidates.Where(x => x.Customer.Contains(filter.Search) || x.SerialNumber == filter.InvoiceNumber);
        if (filter.Export.HasValue)
            candidates = candidates.Where(x => (x.Export ?? false) == filter.Export.Value);
        if (filter.Status.HasValue)
            candidates = candidates.Where(factor => dispatches.Any(x => x.FactorId == factor.Id && x.Status == filter.Status.Value));
        if (filter.Unqueued)
            candidates = candidates.Where(factor => !dispatches.Any(x => x.FactorId == factor.Id));

        var count = await candidates.CountAsync(ct);
        var pageCount = Math.Max(1, (int)Math.Ceiling(count / (decimal)filter.PageSize));
        var page = Math.Min(pageCount, filter.Page);
        var invoices = await candidates.OrderByDescending(x => x.DateMiladi).ThenByDescending(x => x.SerialNumber)
            .ThenBy(x => x.Id).Skip((page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(x => new
            {
                x.Id, x.SerialNumber, x.Customer, x.DateMiladi, x.PaymentAmount, x.CurrencyType,
                Export = x.Export == true, IsReturn = x.ParentReturnedId != null
            }).ToListAsync(ct);
        var ids = invoices.Select(x => x.Id).ToArray();
        if (ids.Length == 0)
            return new([], page, pageCount, count);

        var documents = await (from document in dispatches
            where ids.Contains(document.FactorId)
            join original in dispatches on document.ReferencedDispatchId equals (Guid?)original.Id into originals
            from original in originals.DefaultIfEmpty()
            join source in factors on original.FactorId equals source.Id into sources
            from source in sources.DefaultIfEmpty()
            orderby document.QueuedUtc descending, document.Id
            select new TaxInvoiceDocument(document.Id, document.FactorId, document.Kind, document.Status,
                document.TaxId, original.TaxId, (int?)source.SerialNumber)).ToListAsync(ct);
        var byFactor = documents.ToLookup(x => x.FactorId);
        return new(invoices.Select(x => new TaxInvoiceListItem(x.Id, x.SerialNumber, x.Customer, x.DateMiladi,
            x.PaymentAmount, x.CurrencyType.ToString().Replace('_', ' '), x.Export, x.IsReturn,
            byFactor[x.Id].ToArray())).ToArray(), page, pageCount, count);
    }
}
