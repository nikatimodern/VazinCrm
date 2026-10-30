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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomer;

public sealed record GetCustomerQuery(Guid Id) : IQuery<Customer?>;
public sealed class GetCustomerQueryHandler(ICustomerRepository repository) : IQueryHandler<GetCustomerQuery, Customer?>
{
    public async Task<Customer?> HandleAsync(GetCustomerQuery request, CancellationToken ct)
    {
        return await repository.GetAsync(request.Id, false, false, ct);
    }
}
