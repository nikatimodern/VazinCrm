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
namespace CRM.Core.ApplicationServices.Customers.Commands.RemoveCustomerExpert;

public sealed record RemoveCustomerExpertCommand(Guid CustomerId, Guid UserId) : ICommand<CustomerMutationResult>;
public sealed class RemoveCustomerExpertCommandHandler(ICustomerRepository repository, CustomerAccess access) : ICommandHandler<RemoveCustomerExpertCommand, CustomerMutationResult>
{
    public async Task<CustomerMutationResult> HandleAsync(RemoveCustomerExpertCommand request, CancellationToken ct)
    {
        if (access.ScopeEnforced)
        {
            var identity = await access.GetAsync(ct);
            if (!await access.CanAccessAllAsync(identity, "ManageExperts", ct) ||
                !await access.CanAccessAsync(identity, "View", request.CustomerId, ct))
                return new(403);
        }
        await repository.RemoveAssignmentAsync(request.CustomerId, request.UserId, ct);
        await repository.SaveChangesAsync(ct);
        return new(200);
    }
}
