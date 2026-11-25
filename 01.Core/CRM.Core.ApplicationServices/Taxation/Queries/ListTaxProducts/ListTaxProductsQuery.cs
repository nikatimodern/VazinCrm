using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Queries.ListTaxProducts;

public sealed record ListTaxProductsQuery(string? Search, bool Unmapped, int Page = 1) : IQuery<TaxProductPage>;

public sealed class ListTaxProductsQueryHandler(ITaxationRepository repository) : IQueryHandler<ListTaxProductsQuery, TaxProductPage>
{
    public Task<TaxProductPage> HandleAsync(ListTaxProductsQuery request, CancellationToken ct)
    {
        var search = request.Search?.Trim() ?? string.Empty;
        if (search.Length > 100)
            throw new ArgumentException("عبارت جستجو نباید بیش از ۱۰۰ نویسه باشد.");
        return repository.ListProductsAsync(search, request.Unmapped, Math.Max(1, request.Page), 25, ct);
    }
}
