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
public sealed class GetCustomerByMobileQueryHandler(ICustomerRepository repository) : IQueryHandler<GetCustomerByMobileQuery, Customer?>
{
    public async Task<Customer?> HandleAsync(GetCustomerByMobileQuery request, CancellationToken ct)
    {
        return await repository.GetByMobileAsync(request.Mobile, ct);
    }
}
