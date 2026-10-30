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

namespace CRM.Core.ApplicationServices.Orders.Queries.GetOrderPrint;
public sealed record GetOrderPrintQuery(Guid Id) : IQuery<OrderPrintData>;
public sealed class GetOrderPrintQueryHandler(IOrderRepository repository) : IQueryHandler<GetOrderPrintQuery, OrderPrintData>
{
    public async Task<OrderPrintData> HandleAsync(GetOrderPrintQuery request, CancellationToken ct)
    {
        return await repository.GetPrintDataAsync(request.Id, ct);
    }
}
