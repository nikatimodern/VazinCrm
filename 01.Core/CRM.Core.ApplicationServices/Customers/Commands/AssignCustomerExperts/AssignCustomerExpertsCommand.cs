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
namespace CRM.Core.ApplicationServices.Customers.Commands.AssignCustomerExperts;

public sealed record AssignCustomerExpertsCommand(Guid CustomerId, List<Guid> UserIds) : ICommand<CustomerMutationResult>;
public sealed class AssignCustomerExpertsCommandHandler(ICustomerRepository repository) : ICommandHandler<AssignCustomerExpertsCommand, CustomerMutationResult>
{
    public async Task<CustomerMutationResult> HandleAsync(AssignCustomerExpertsCommand request, CancellationToken ct)
    {
        await repository.AddAssignmentsAsync(request.CustomerId, request.UserIds, ct);
        await repository.SaveChangesAsync(ct);
        return new(200);
    }
}
