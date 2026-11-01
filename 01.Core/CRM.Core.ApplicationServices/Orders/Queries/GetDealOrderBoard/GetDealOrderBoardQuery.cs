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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetDealOrderBoard;
public sealed record GetDealOrderBoardQuery(Guid Id) : IQuery<object>;
public sealed class GetDealOrderBoardQueryHandler(IOrderRepository repository, IOrderEffects effects) : IQueryHandler<GetDealOrderBoardQuery, object>
{
    public async Task<object> HandleAsync(GetDealOrderBoardQuery request, CancellationToken ct)
    {
        var heads = await repository.GetHeadsByDealAsync(request.Id, ct);
        async Task<object> Column(int status)
        {
            var list = heads.Where(h => (int? )h.Status == status).ToList();
            return new
            {
                count = list.Count,
                html = await effects.RenderColumnAsync(list, 4, ct)
            };
        }

        return new
        {
            dealOrderRegistered = await Column(1),
            dealOrderProducing = await Column(2),
            dealOrderFinished = await Column(3),
            dealOrderDelivered = await Column(4)
        };
    }
}
