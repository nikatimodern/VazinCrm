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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerSummaries;

public sealed record GetCustomerSummariesQuery() : IQuery<List<CustomerSummary>>;
public sealed class GetCustomerSummariesQueryHandler(ICustomerRepository repository) : IQueryHandler<GetCustomerSummariesQuery, List<CustomerSummary>>
{
    public async Task<List<CustomerSummary>> HandleAsync(GetCustomerSummariesQuery request, CancellationToken ct)
    {
        return await repository.GetSummariesAsync(ct);
    }
}
