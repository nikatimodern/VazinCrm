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
namespace CRM.Core.ApplicationServices.Customers.Queries.SearchCustomerCards;

public sealed record SearchCustomerCardsQuery(string? Search, int RoleType, int Juridical, bool? ReadAllOverride = null, Guid[]? AllowedOwnerIds = null, Guid[]? ExcludedOwnerIds = null) : IQuery<CustomerListResult>;
public sealed class SearchCustomerCardsQueryHandler(ICustomerRepository repository, CustomerAccess access, ICustomerEffects effects) : IQueryHandler<SearchCustomerCardsQuery, CustomerListResult>
{
    public async Task<CustomerListResult> HandleAsync(SearchCustomerCardsQuery request, CancellationToken ct)
    {
        var identity = await access.GetAsync(ct);
        var search = effects.Normalize(request.Search);
        var words = CustomerSearch.Words(search);
        return new(await repository.GetListAsync(new(0, 0, request.RoleType, request.Juridical, search, words, request.AllowedOwnerIds, request.ExcludedOwnerIds), identity.User.Id, request.ReadAllOverride ?? identity.CanManage, ct), identity.CanManage);
    }
}
