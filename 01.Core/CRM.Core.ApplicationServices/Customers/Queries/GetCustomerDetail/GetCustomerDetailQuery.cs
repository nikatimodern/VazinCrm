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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerDetail;

public sealed record GetCustomerDetailQuery(Guid Id) : IQuery<Customer?>;
public sealed class GetCustomerDetailQueryHandler(ICustomerRepository repository) : IQueryHandler<GetCustomerDetailQuery, Customer?>
{
    public async Task<Customer?> HandleAsync(GetCustomerDetailQuery request, CancellationToken ct)
    {
        return request.Id == Guid.Empty ? null : await repository.GetAsync(request.Id, false, false, ct);
    }
}
