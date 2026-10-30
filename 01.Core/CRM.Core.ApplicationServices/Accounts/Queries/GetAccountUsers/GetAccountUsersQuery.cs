using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Queries.GetAccountUsers;

public sealed record GetAccountUsersQuery(int? Type = 2) : IQuery<List<AccountUserOption>?>;

public sealed class GetAccountUsersQueryHandler(IAccountUserRepository users, IAccountRequestContext context) : IQueryHandler<GetAccountUsersQuery, List<AccountUserOption>?>
{
    public async Task<List<AccountUserOption>?> HandleAsync(GetAccountUsersQuery request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(context.UserId, cancellationToken) ?? throw new UnauthorizedAccessException();
        List<AppUser> found;
        switch (request.Type)
        {
            case 1: found = await users.GetEmployeesAsync(cancellationToken); break;
            case 2: found = await users.GetInRoleAsync("کارشناس فروش", cancellationToken); break;
            case 3:
                var experts = await users.GetInRoleAsync("کارشناس تامین", cancellationToken);
                var admins = await users.GetInRoleAsync("مدیر تامین", cancellationToken);
                found = experts.Concat(admins).DistinctBy(u => u.Id).ToList(); break;
            default: return null;
        }
        var currentId = Guid.Parse(user.Id);
        return found.Select(u => new AccountUserOption { id = Guid.Parse(u.Id), text = u.FullName, Target = Guid.Parse(u.Id) == currentId }).ToList();
    }
}
public sealed class AccountUserOption { public Guid id { get; init; } public string text { get; init; } = ""; public bool? Target { get; init; } = false; }
