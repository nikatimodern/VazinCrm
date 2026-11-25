using System.Globalization;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Queries.ListTaxInvoices;

public sealed record ListTaxInvoicesQuery(string? Search = null, bool? Export = null, TaxDispatchStatus? Status = null,
    bool Unqueued = false, int Page = 1) : IQuery<TaxInvoicePage>
{
    public const int PageSize = 25;
}

public sealed class ListTaxInvoicesQueryHandler(ITaxInvoiceListing listing)
    : IQueryHandler<ListTaxInvoicesQuery, TaxInvoicePage>
{
    public Task<TaxInvoicePage> HandleAsync(ListTaxInvoicesQuery request, CancellationToken ct)
    {
        var search = request.Search?.Trim() ?? string.Empty;
        if (search.Length > 100 || request.Status is { } status && !Enum.IsDefined(status) ||
            request.Status.HasValue && request.Unqueued)
            throw new ArgumentException("فیلتر صورتحساب معتبر نیست.");

        var normalized = string.Concat(search.Select(character => character switch
        {
            >= '۰' and <= '۹' => (char)('0' + character - '۰'),
            >= '٠' and <= '٩' => (char)('0' + character - '٠'),
            _ => character
        }));
        int? invoiceNumber = int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? number : null;
        return listing.ListAsync(new(search, invoiceNumber, request.Export, request.Status,
            request.Unqueued, Math.Max(1, request.Page), ListTaxInvoicesQuery.PageSize), ct);
    }
}
