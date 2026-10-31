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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomersPage;

public sealed record GetCustomersPageQuery() : IQuery<AppUser>;
public sealed class GetCustomersPageQueryHandler(CustomerAccess access) : IQueryHandler<GetCustomersPageQuery, AppUser>
{
    public async Task<AppUser> HandleAsync(GetCustomersPageQuery request, CancellationToken ct)
    {
        return (await access.GetAsync(ct)).User;
    }
}
