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
namespace CRM.Core.ApplicationServices.Customers.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(Customer Model) : ICommand<CustomerMutationResult>;
public sealed class UpdateCustomerCommandHandler(ICustomerRepository repository, CustomerAccess access, ICustomerEffects effects) : ICommandHandler<UpdateCustomerCommand, CustomerMutationResult>
{
    public async Task<CustomerMutationResult> HandleAsync(UpdateCustomerCommand request, CancellationToken ct)
    {
        var identity = await access.GetAsync(ct);
        var original = await repository.GetAsync(request.Model.Id, false, false, ct);
        if (original == null) return new(404);
        var model = request.Model;
        model.RoleTypeTxt = model.RoleType.ToString().Replace("_", " ");
        model.Accepted = false;
        CustomerAccess.Stamp(model, identity.User);
        model.DateSort = DateTime.Now; model.DateSortTxt = BaseServices.GetDateTimePersian();
        model.NormalizedCompanyName = effects.Normalize(model.CompanyName); model.NormalizedAgentName = effects.Normalize(model.AgentName);
        model.CustomerCode = original.CustomerCode; model.UserCreator = original.UserCreator; model.UserCreatorID = original.UserCreatorID;
        model.CreateDate = original.CreateDate; model.CreateDateTxt = original.CreateDateTxt;
        model.AcquaintanceWay = original.AcquaintanceWay; model.AcquaintanceWayTxt = original.AcquaintanceWayTxt;
        var status = await repository.GetStatusAsync(model.Id, ct); model.Success = status.Active; model.Export = status.Export;
        await repository.UpdateAsync(model, ct);
        await repository.SyncDealsAsync(model, ct);
        await effects.AuditAsync(original, model, LogActionEnum.ویرایش, identity.User, ct);
        await repository.SaveChangesAsync(ct);
        return new(200);
    }
}
