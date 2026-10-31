using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Abstractions.Identity;
public sealed record ApplicationActor(AppUser User, IList<string> Roles)
{
    public bool HasRole(params string[] roles) => roles.Any(Roles.Contains);
}

public sealed class CurrentActor(IAccountUserRepository users, IAccountRequestContext context)
{
    public async Task<ApplicationActor> GetAsync(CancellationToken cancellationToken)
    {
        return await FindAsync(cancellationToken) ?? throw new UnauthorizedAccessException();
    }

    public async Task<ApplicationActor?> FindAsync(CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(context.UserId, cancellationToken);
        return user == null ? null : new(user, await users.GetRolesAsync(user, cancellationToken));
    }
}
