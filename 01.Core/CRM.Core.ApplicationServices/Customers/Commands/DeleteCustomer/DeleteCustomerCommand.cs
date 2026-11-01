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
namespace CRM.Core.ApplicationServices.Customers.Commands.DeleteCustomer;

public sealed record DeleteCustomerCommand(Guid Id) : ICommand<CustomerMutationResult>;
public sealed class DeleteCustomerCommandHandler(ICustomerRepository repository, CustomerAccess access, ICustomerEffects effects) : ICommandHandler<DeleteCustomerCommand, CustomerMutationResult>
{
    public async Task<CustomerMutationResult> HandleAsync(DeleteCustomerCommand request, CancellationToken ct)
    {
        var identity = await access.GetAsync(ct);
        var customer = await repository.GetAsync(request.Id, false, false, ct);
        if (customer == null) return new(404);
        await effects.AuditAsync(customer, null, LogActionEnum.حذف, identity.User, ct);
        CustomerAccess.Stamp(customer, identity.User);
        await repository.DeleteWithPreFactorsAsync(customer, ct);
        await repository.SaveChangesAsync(ct);
        return new(200);
    }
}
