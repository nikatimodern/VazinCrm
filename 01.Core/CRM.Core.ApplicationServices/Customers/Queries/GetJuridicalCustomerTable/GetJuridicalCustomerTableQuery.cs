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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetJuridicalCustomerTable;

public sealed record GetJuridicalCustomerTableQuery(CustomerTableRequest Filter) : IQuery<CustomerTableResult>;
public sealed class GetJuridicalCustomerTableQueryHandler(ICustomerRepository repository) : IQueryHandler<GetJuridicalCustomerTableQuery, CustomerTableResult>
{
    public async Task<CustomerTableResult> HandleAsync(GetJuridicalCustomerTableQuery request, CancellationToken ct)
    {
        return await repository.GetTableAsync(request.Filter with { Juridical = 1 }, ct);
    }
}
