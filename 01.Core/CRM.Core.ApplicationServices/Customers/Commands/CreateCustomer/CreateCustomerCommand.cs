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
namespace CRM.Core.ApplicationServices.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(Customer Model) : ICommand<CustomerMutationResult>;
public sealed class CreateCustomerCommandHandler(ICustomerRepository repository, CustomerAccess access, ICustomerEffects effects) : ICommandHandler<CreateCustomerCommand, CustomerMutationResult>
{
    public async Task<CustomerMutationResult> HandleAsync(CreateCustomerCommand request, CancellationToken ct)
    {
        await repository.BeginWriteAsync(ct);
        var identity = await access.GetAsync(ct);
        var settings = await repository.GetSettingsAsync(ct);
        var model = request.Model;
        if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();
        model.UserCreator = identity.User.FullName; model.UserCreatorID = identity.User.Id;
        model.RoleTypeTxt = model.RoleType.ToString().Replace("_", " ");
        if (settings?.SmallOffice == true) model.Accepted = true;
        model.NormalizedCompanyName = effects.Normalize(model.CompanyName); model.NormalizedAgentName = effects.Normalize(model.AgentName);
        model.UserCustomers = null; model.PreFactors = null; model.Accountancies = null; model.Deals = null;
        model.Contracts = null; model.Contacts = null; model.orderHeads = null;
        await repository.AddAsync(model, Guid.Parse(identity.User.Id), ct);
        await effects.AuditAsync(model, null, LogActionEnum.ثبت, identity.User, ct);
        await repository.SaveChangesAsync(ct);
        await repository.CommitAsync(ct);
        return new(200, model.Id);
    }
}
