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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerSuppliers;

public sealed record GetCustomerSuppliersQuery() : IQuery<List<CustomerSupplier>>;
public sealed class GetCustomerSuppliersQueryHandler(ICustomerRepository repository) : IQueryHandler<GetCustomerSuppliersQuery, List<CustomerSupplier>>
{
    public async Task<List<CustomerSupplier>> HandleAsync(GetCustomerSuppliersQuery request, CancellationToken ct)
    {
        return await repository.GetSuppliersAsync(ct);
    }
}
