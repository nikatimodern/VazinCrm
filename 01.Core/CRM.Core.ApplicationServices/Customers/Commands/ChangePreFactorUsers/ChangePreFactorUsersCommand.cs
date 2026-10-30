using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Customers.Abstractions;
using CRM.Core.ApplicationServices.Customers.Dtos;
using CRM.Core.ApplicationServices.Customers.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Repositories;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
namespace CRM.Core.ApplicationServices.Customers.Commands.ChangePreFactorUsers;

public sealed record ChangePreFactorUsersCommand(Guid PreFactorId, Guid SalesUserId, Guid SupplyUserId) : ICommand<CustomerMutationResult>;
public sealed class ChangePreFactorUsersCommandHandler(ICustomerRepository repository, IAccountUserRepository users) : ICommandHandler<ChangePreFactorUsersCommand, CustomerMutationResult>
{
    public async Task<CustomerMutationResult> HandleAsync(ChangePreFactorUsersCommand request, CancellationToken ct)
    {
        if (await repository.GetPreFactorAsync(request.PreFactorId, ct) == null) return new(404, "رکوردی یافت نشد!");
        var sales = request.SalesUserId == Guid.Empty ? null : await users.FindByIdAsync(request.SalesUserId.ToString(), ct);
        var supply = request.SupplyUserId == Guid.Empty ? null : await users.FindByIdAsync(request.SupplyUserId.ToString(), ct);
        if ((request.SalesUserId != Guid.Empty && sales == null) || (request.SupplyUserId != Guid.Empty && supply == null)) return new(400);
        await repository.ChangePreFactorUsersAsync(request.PreFactorId, sales, supply, ct);
        await repository.SaveChangesAsync(ct);
        return new(200);
    }
}
