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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerByMobile;

public sealed record GetCustomerByMobileQuery(string Mobile) : IQuery<Customer?>;
public sealed class GetCustomerByMobileQueryHandler(ICustomerRepository repository, CustomerAccess access) : IQueryHandler<GetCustomerByMobileQuery, Customer?>
{
    public async Task<Customer?> HandleAsync(GetCustomerByMobileQuery request, CancellationToken ct)
    {
        var person = await repository.GetByMobileAsync(request.Mobile, ct);
        if (person is not null && access.ScopeEnforced)
        {
            var identity = await access.GetAsync(ct);
            if (!await access.CanAccessAsync(identity, "View", person.Id, ct)) return null;
        }
        return person;
    }
}
