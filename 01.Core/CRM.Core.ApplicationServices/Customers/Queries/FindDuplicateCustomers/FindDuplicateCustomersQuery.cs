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
namespace CRM.Core.ApplicationServices.Customers.Queries.FindDuplicateCustomers;

public sealed record FindDuplicateCustomersQuery(Customer Model) : IQuery<List<Customer>>;
public sealed class FindDuplicateCustomersQueryHandler(ICustomerRepository repository) : IQueryHandler<FindDuplicateCustomersQuery, List<Customer>>
{
    public async Task<List<Customer>> HandleAsync(FindDuplicateCustomersQuery request, CancellationToken ct)
    {
        return await repository.GetDuplicatesAsync(request.Model, ct);
    }
}
