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

namespace CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFinancialHtml;
public sealed record GetCartableFinancialHtmlQuery(string? Search, int Type) : IQuery<object>;
public sealed class GetCartableFinancialHtmlQueryHandler(ICqrsDispatcher dispatcher, ICartablePresentation presentation) : IQueryHandler<GetCartableFinancialHtmlQuery, object>
{
    public async Task<object> HandleAsync(GetCartableFinancialHtmlQuery request, CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetCartableFinancialQuery(request.Search, request.Type), ct);
        return await presentation.FinancialAsync(data, ct);
    }
}
