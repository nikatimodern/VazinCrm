using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Orders.Repositories;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Orders.Services;

namespace CRM.Core.ApplicationServices.Orders.Queries.SelectOrderHeads;
public sealed record OrderHeadSelection(List<OrderHead> Heads, bool MultiSelect, int PassType);
public sealed record SelectOrderHeadsQuery(HeadSelection Selection, int PassType) : IQuery<OrderHeadSelection>;
public sealed class SelectOrderHeadsQueryHandler(IOrderRepository repository) : IQueryHandler<SelectOrderHeadsQuery, OrderHeadSelection>
{
    public async Task<OrderHeadSelection> HandleAsync(SelectOrderHeadsQuery request, CancellationToken ct)
    {
        return new(await repository.SelectHeadsAsync(request.Selection, ct), request.PassType != 4 || await repository.AllowMultipleSelectionAsync(ct), request.PassType);
    }
}
