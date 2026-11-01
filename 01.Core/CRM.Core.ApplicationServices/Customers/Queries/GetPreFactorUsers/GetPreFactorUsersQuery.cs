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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetPreFactorUsers;

public sealed record GetPreFactorUsersQuery(Guid CustomerId, Guid PreFactorId) : IQuery<PreFactorUsersResult?>;
public sealed class GetPreFactorUsersQueryHandler(ICustomerRepository repository, IAccountUserRepository users) : IQueryHandler<GetPreFactorUsersQuery, PreFactorUsersResult?>
{
    public async Task<PreFactorUsersResult?> HandleAsync(GetPreFactorUsersQuery request, CancellationToken ct)
    {
        var pf = await repository.GetPreFactorAsync(request.PreFactorId, ct);
        if (pf == null) return null;
        var sales = request.CustomerId == Guid.Empty ? await users.GetInRoleAsync("کارشناس فروش", ct) : await repository.GetAssignedUsersAsync(request.CustomerId, ct);
        if (request.CustomerId == Guid.Empty && sales.All(u => u.Id != pf.UserCreatorID))
        {
            var current = await users.FindByIdAsync(pf.UserCreatorID, ct);
            if (current != null) sales.Insert(0, current);
        }
        var supply = await users.GetInRoleAsync("کارشناس تامین", ct);
        return new(sales.Select(u => new PreFactorUserOption(Guid.Parse(u.Id), u.FullName, u.Id == pf.UserCreatorID)).ToList(),
            supply.Select(u => new PreFactorUserOption(Guid.Parse(u.Id), u.FullName, Guid.Parse(u.Id) == pf.UserTaminId)).ToList());
    }
}
