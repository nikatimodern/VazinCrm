using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.SelectAccount;

public sealed record SelectAccountCommand(string UserId, string Type = "Login") : ICommand<AccountOutcome>;

public sealed class SelectAccountCommandHandler(IAccountUserRepository users, AccountSignInFlow login) : ICommandHandler<SelectAccountCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(SelectAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(request.UserId, cancellationToken);
        return user is null ? new(404, "کاربر یافت نشد") : new(200, "موفق", await login.CompleteOtpAsync(user, request.Type, cancellationToken));
    }
}
