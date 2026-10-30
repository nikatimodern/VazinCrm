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
namespace CRM.Core.ApplicationServices.Customers.Commands.AddCustomerToCurrentUser;

public sealed record AddCustomerToCurrentUserCommand(Guid Id) : ICommand<CustomerMutationResult>;
public sealed class AddCustomerToCurrentUserCommandHandler(ICustomerRepository repository, CustomerAccess access) : ICommandHandler<AddCustomerToCurrentUserCommand, CustomerMutationResult>
{
    public async Task<CustomerMutationResult> HandleAsync(AddCustomerToCurrentUserCommand request, CancellationToken ct)
    {
        var identity = await access.GetAsync(ct);
        var customer = await repository.GetAsync(request.Id, false, false, ct);
        if (customer == null) return new(404);
        var userId = Guid.Parse(identity.User.Id);
        if (await repository.HasAssignmentAsync(request.Id, userId, ct)) return new(200, 0);
        customer.Accepted = false;
        await repository.UpdateAsync(customer, ct);
        await repository.AddAssignmentsAsync(request.Id, [userId], ct);
        await repository.SaveChangesAsync(ct);
        return new(200, 1);
    }
}
