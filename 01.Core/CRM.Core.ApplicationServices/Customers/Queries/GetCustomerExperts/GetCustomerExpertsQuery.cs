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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerExperts;

public sealed record GetCustomerExpertsQuery(Guid Id) : IQuery<List<AppUser>>;
public sealed class GetCustomerExpertsQueryHandler(ICustomerRepository repository) : IQueryHandler<GetCustomerExpertsQuery, List<AppUser>>
{
    public async Task<List<AppUser>> HandleAsync(GetCustomerExpertsQuery request, CancellationToken ct)
    {
        return await repository.GetAssignedUsersAsync(request.Id, ct);
    }
}
