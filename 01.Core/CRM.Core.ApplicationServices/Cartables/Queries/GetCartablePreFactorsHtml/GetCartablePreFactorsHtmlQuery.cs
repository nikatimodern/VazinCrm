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

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartablePreFactorsHtml;
public sealed record GetCartablePreFactorsHtmlQuery(QueryFilterInput? Filter) : IQuery<object>;
public sealed class GetCartablePreFactorsHtmlQueryHandler(ICqrsDispatcher dispatcher, ICartablePresentation presentation) : IQueryHandler<GetCartablePreFactorsHtmlQuery, object>
{
    public async Task<object> HandleAsync(GetCartablePreFactorsHtmlQuery request, CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetCartablePreFactorsQuery(request.Filter), ct);
        return await presentation.PreFactorsAsync(data, ct);
    }
}
