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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerCards;

public sealed record GetCustomerCardsQuery(int Skip, int Take, int RoleType, int Juridical) : IQuery<CustomerListResult>;
public sealed class GetCustomerCardsQueryHandler(ICustomerRepository repository, CustomerAccess access) : IQueryHandler<GetCustomerCardsQuery, CustomerListResult>
{
    public async Task<CustomerListResult> HandleAsync(GetCustomerCardsQuery request, CancellationToken ct)
    {
        var identity = await access.GetAsync(ct);
        return new(await repository.GetListAsync(new(request.Skip, request.Take, request.RoleType, request.Juridical), identity.User.Id, identity.CanManage, ct), identity.CanManage);
    }
}
