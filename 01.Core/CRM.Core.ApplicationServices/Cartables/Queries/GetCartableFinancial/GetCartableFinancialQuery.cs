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

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFinancial;
public sealed record GetCartableFinancialQuery(string? Search, int Type) : IQuery<CartableFinancialData>;
public sealed class GetCartableFinancialQueryHandler(ICartableRepository repository, ICartableEffects effects) : IQueryHandler<GetCartableFinancialQuery, CartableFinancialData>
{
    public async Task<CartableFinancialData> HandleAsync(GetCartableFinancialQuery request, CancellationToken ct)
    {
        return await repository.GetFinancialAsync(request.Search, request.Type, effects.ParseNumber(request.Search), ct);
    }
}
