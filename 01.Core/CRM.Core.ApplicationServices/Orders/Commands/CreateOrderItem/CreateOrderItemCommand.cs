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

namespace CRM.Core.ApplicationServices.Orders.Commands.CreateOrderItem;
public sealed record CreateOrderItemCommand(Order Item) : ICommand<int>;
public sealed class CreateOrderItemCommandHandler(IOrderRepository repository) : ICommandHandler<CreateOrderItemCommand, int>
{
    public async Task<int> HandleAsync(CreateOrderItemCommand request, CancellationToken ct)
    {
        request.Item.OrderHead = null !;
        await repository.SaveItemAsync(request.Item, true, ct);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
