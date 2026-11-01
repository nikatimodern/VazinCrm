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
using CRM.Core.ApplicationServices.Orders.Abstractions;

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderBoard;
public sealed record GetOrderBoardQuery(QueryFilterInput? Filter, int InvoiceStatus) : IQuery<object>;
public sealed class GetOrderBoardQueryHandler(IOrderRepository repository, CurrentActor actor, IApplicationDates dates, IOrderEffects effects) : IQueryHandler<GetOrderBoardQuery, object>
{
    public async Task<object> HandleAsync(GetOrderBoardQuery request, CancellationToken ct)
    {
        var current = await actor.GetAsync(ct);
        var filter = QueryFilterResolver.Resolve(request.Filter, dates);
        var heads = await repository.GetBoardAsync(filter, request.InvoiceStatus, OrderAccess.Reader(current), ct);
        async Task<object> Column(int status)
        {
            var list = heads.Where(h => (int? )h.Status == status).ToList();
            return new
            {
                count = list.Count,
                html = await effects.RenderColumnAsync(list, 1, ct)
            };
        }

        if (filter.Type > 0)
            return await Column(filter.Type);
        return new
        {
            registered = await Column(1),
            producing = await Column(2),
            finished = await Column(3),
            deliveredCount = filter.HasFilter || !string.IsNullOrEmpty(filter.Search) ? heads.Count(h => (int? )h.Status == 4) : 0
        };
    }
}
