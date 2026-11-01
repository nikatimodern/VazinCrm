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
namespace CRM.Core.ApplicationServices.Customers.Queries.SearchCustomerOptions;

public sealed record SearchCustomerOptionsQuery(string? Search) : IQuery<List<CustomerOption>?>;
public sealed class SearchCustomerOptionsQueryHandler(ICustomerRepository repository) : IQueryHandler<SearchCustomerOptionsQuery, List<CustomerOption>?>
{
    public async Task<List<CustomerOption>?> HandleAsync(SearchCustomerOptionsQuery request, CancellationToken ct)
    {
        return request.Search == null ? null : await repository.SearchOptionsAsync(request.Search, ct);
    }
}
