using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Cartables.Repositories;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cartables.Abstractions;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSales;
public sealed record GetCartableSalesQuery(string? Search, int Type) : IQuery<CartableSalesData>;
public sealed class GetCartableSalesQueryHandler(ICartableRepository repository, ICartableEffects effects) : IQueryHandler<GetCartableSalesQuery, CartableSalesData>
{
    public async Task<CartableSalesData> HandleAsync(GetCartableSalesQuery request, CancellationToken ct)
    {
        return await repository.GetSalesAsync(request.Search, request.Type, effects.ParseNumber(request.Search), ct);
    }
}
