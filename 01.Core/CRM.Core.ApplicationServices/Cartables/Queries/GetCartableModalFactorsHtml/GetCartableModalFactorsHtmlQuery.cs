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

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableModalFactorsHtml;
public sealed record GetCartableModalFactorsHtmlQuery(Guid Id, int Type) : IQuery<object>;
public sealed class GetCartableModalFactorsHtmlQueryHandler(ICqrsDispatcher dispatcher, ICartablePresentation presentation) : IQueryHandler<GetCartableModalFactorsHtmlQuery, object>
{
    public async Task<object> HandleAsync(GetCartableModalFactorsHtmlQuery request, CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetCartableModalFactorsQuery(request.Id, request.Type), ct);
        return await presentation.FactorsAsync(data, request.Type, ct);
    }
}
