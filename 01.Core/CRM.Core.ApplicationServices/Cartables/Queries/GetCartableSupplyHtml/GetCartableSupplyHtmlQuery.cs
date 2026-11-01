using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.ApplicationServices.Cartables.Abstractions;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartablePreFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableModalFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupply;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFinancial;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSales;
using CRM.Core.Domain.Querying;

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupplyHtml;
public sealed record GetCartableSupplyHtmlQuery(string? Search, int Type) : IQuery<object>;
public sealed class GetCartableSupplyHtmlQueryHandler(ICqrsDispatcher dispatcher, ICartablePresentation presentation) : IQueryHandler<GetCartableSupplyHtmlQuery, object>
{
    public async Task<object> HandleAsync(GetCartableSupplyHtmlQuery request, CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetCartableSupplyQuery(request.Search, request.Type), ct);
        return await presentation.SupplyAsync(data, ct);
    }
}
