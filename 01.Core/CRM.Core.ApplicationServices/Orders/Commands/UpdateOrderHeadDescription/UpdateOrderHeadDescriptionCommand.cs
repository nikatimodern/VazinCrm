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

namespace CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderHeadDescription;
public sealed record UpdateOrderHeadDescriptionCommand(Guid Id, string Description) : ICommand<int>;
public sealed class UpdateOrderHeadDescriptionCommandHandler(IOrderRepository repository) : ICommandHandler<UpdateOrderHeadDescriptionCommand, int>
{
    public async Task<int> HandleAsync(UpdateOrderHeadDescriptionCommand request, CancellationToken ct)
    {
        var head = await repository.GetHeadAsync(request.Id, true, ct);
        if (head == null)
            return 404;
        head.Description = request.Description;
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
