using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.ToggleAccountLockout;

public sealed record ToggleAccountLockoutCommand(string UserId) : ICommand<AccountIdentityResult?>;

public sealed class ToggleAccountLockoutCommandHandler(IAccountUserRepository users) : ICommandHandler<ToggleAccountLockoutCommand, AccountIdentityResult?>
{
    public async Task<AccountIdentityResult?> HandleAsync(ToggleAccountLockoutCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null) return null;
        user.LockoutEnd = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow ? null : DateTime.UtcNow.AddYears(300);
        return await users.UpdateLockoutAsync(user, cancellationToken);
    }
}
