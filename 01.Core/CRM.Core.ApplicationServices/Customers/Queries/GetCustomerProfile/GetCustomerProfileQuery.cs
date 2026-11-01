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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerProfile;

public sealed record GetCustomerProfileQuery(Guid Id) : IQuery<CustomerProfileResult>;
public sealed class GetCustomerProfileQueryHandler(ICustomerRepository repository, CustomerAccess access) : IQueryHandler<GetCustomerProfileQuery, CustomerProfileResult>
{
    public async Task<CustomerProfileResult> HandleAsync(GetCustomerProfileQuery request, CancellationToken ct)
    {
        if (request.Id == Guid.Empty) return new(400);
        var identity = await access.GetAsync(ct);
        var customer = await repository.GetAsync(request.Id, identity.IsAdmin, true, ct);
        if (customer == null) return new(404);
        var allowed = identity.CanManage || (identity.IsCustomer
            ? !customer.IsDeleted && customer.Mobile == identity.User.PhoneNumber
            : await repository.HasAssignmentAsync(request.Id, Guid.Parse(identity.User.Id), ct));
        if (!allowed) return new(403);
        var name = $"پنل مشتری {customer.CustomerCode} {customer.CompanyName} ({(customer.Sex == 1 ? "جناب آقای" : "سرکار خانم")} {customer.AgentName})";
        return new(200, customer.Id, name, await repository.GetUserNamesAsync(customer.UserCustomers.Select(u => u.UserId).ToList(), ct));
    }
}
