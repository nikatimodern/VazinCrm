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
namespace CRM.Core.ApplicationServices.Customers.Commands.AcceptCustomer;

public sealed record AcceptCustomerCommand(Guid Id) : ICommand<CustomerMutationResult>;
public sealed class AcceptCustomerCommandHandler(ICustomerRepository repository, CustomerAccess access, ICustomerEffects effects) : ICommandHandler<AcceptCustomerCommand, CustomerMutationResult>
{
    public async Task<CustomerMutationResult> HandleAsync(AcceptCustomerCommand request, CancellationToken ct)
    {
        var identity = await access.GetAsync(ct);
        if (!identity.CanManage) return new(403);
        var original = await repository.GetAsync(request.Id, false, false, ct);
        if (original == null) return new(404);
        var customer = await repository.GetAsync(request.Id, false, false, ct);
        customer!.Accepted = true;
        CustomerAccess.Stamp(customer, identity.User);
        customer.DateSort = DateTime.Now; customer.DateSortTxt = BaseServices.GetDateTimePersian();
        await repository.UpdateAsync(customer, ct);
        await effects.AuditAsync(original, customer, LogActionEnum.تایید_و_تغییر_وضعیت, identity.User, ct);
        await repository.SaveChangesAsync(ct);
        effects.NotifyAccepted(customer, identity.User);
        return new(200);
    }
}
