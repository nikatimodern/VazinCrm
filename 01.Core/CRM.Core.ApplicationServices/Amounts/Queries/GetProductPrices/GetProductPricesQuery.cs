using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Products.Repositories;

namespace CRM.Core.ApplicationServices.Amounts.Queries.GetProductPrices;
public sealed record ProductPricesResult(List<Product> Products, bool HasAccess, string From);
public sealed record GetProductPricesQuery(string? Search, string From) : IQuery<ProductPricesResult>;
public sealed class GetProductPricesQueryHandler(IProductPriceRepository repository, CurrentActor actor, IApplicationDates dates) : IQueryHandler<GetProductPricesQuery, ProductPricesResult>
{
    public async Task<ProductPricesResult> HandleAsync(GetProductPricesQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var products = await repository.SearchAsync(request.Search, ct);
        foreach (var product in products)
            foreach (var price in product.ProductAmounts ?? [])
                price.ElapsTime = dates.Elapsed(price.ModifyDate ?? price.CreateDate);
        return new(products, current.HasRole("مدیر ارشد", "مدیر فروش", "مدیر تامین", "کارشناس تامین"), request.From);
    }
}
