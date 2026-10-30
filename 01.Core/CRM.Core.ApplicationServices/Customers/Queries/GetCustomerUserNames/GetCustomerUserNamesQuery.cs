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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerUserNames;

public sealed record GetCustomerUserNamesQuery(List<Guid> UserIds) : IQuery<string>;
public sealed class GetCustomerUserNamesQueryHandler(ICustomerRepository repository) : IQueryHandler<GetCustomerUserNamesQuery, string>
{
    public async Task<string> HandleAsync(GetCustomerUserNamesQuery request, CancellationToken ct)
    {
        return await repository.GetUserNamesAsync(request.UserIds, ct);
    }
}
