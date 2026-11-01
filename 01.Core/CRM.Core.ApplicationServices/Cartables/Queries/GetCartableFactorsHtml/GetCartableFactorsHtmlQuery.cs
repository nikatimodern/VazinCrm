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

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactorsHtml;
public sealed record GetCartableFactorsHtmlQuery(QueryFilterInput? Filter, Guid? CustomerId, int PassType) : IQuery<object>;
public sealed class GetCartableFactorsHtmlQueryHandler(ICqrsDispatcher dispatcher, ICartablePresentation presentation) : IQueryHandler<GetCartableFactorsHtmlQuery, object>
{
    public async Task<object> HandleAsync(GetCartableFactorsHtmlQuery request, CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetCartableFactorsQuery(request.Filter, request.CustomerId), ct);
        return await presentation.FactorsAsync(data, request.PassType, ct);
    }
}
