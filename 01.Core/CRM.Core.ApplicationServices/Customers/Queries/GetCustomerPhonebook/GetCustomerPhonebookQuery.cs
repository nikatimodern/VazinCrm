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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerPhonebook;

public sealed record GetCustomerPhonebookQuery(string? Search) : IQuery<List<CustomerPhone>>;
public sealed class GetCustomerPhonebookQueryHandler(ICustomerRepository repository, ICustomerEffects effects) : IQueryHandler<GetCustomerPhonebookQuery, List<CustomerPhone>>
{
    public async Task<List<CustomerPhone>> HandleAsync(GetCustomerPhonebookQuery request, CancellationToken ct)
    {
        var search = effects.Normalize(request.Search);
        return await repository.GetPhonebookAsync(search, CustomerSearch.Words(search), ct);
    }
}
