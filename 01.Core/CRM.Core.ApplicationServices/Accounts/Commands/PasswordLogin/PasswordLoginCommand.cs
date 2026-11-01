using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.PasswordLogin;

public sealed record PasswordLoginCommand(string UserName, string Password, string? ReturnUrl) : ICommand<AccountOutcome>
{
    public override string ToString() => nameof(PasswordLoginCommand);
}

public sealed class PasswordLoginCommandHandler(IAccountUserRepository users, IAccountAuthentication authentication, AccountSignInFlow login) : ICommandHandler<PasswordLoginCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(PasswordLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByNameAsync(request.UserName, cancellationToken);
        if (user is not null)
        {
            await authentication.SignOutAsync(cancellationToken);
            var result = await authentication.PasswordSignInAsync(user, request.Password, cancellationToken);
            if (result.Succeeded) return new(200, "موفق", await login.CompleteAsync(user, request.ReturnUrl, cancellationToken));
            if (result.IsLockedOut) return new(403, "حساب کاربری شما غیرفعال شده است.");
        }
        return new(404, "نام کاربری یا رمز عبور اشتباه است!");
    }
}
