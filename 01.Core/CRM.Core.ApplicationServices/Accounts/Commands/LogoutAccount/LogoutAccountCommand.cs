using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.LogoutAccount;

public sealed record LogoutAccountCommand() : ICommand<AccountRedirect>;

public sealed class LogoutAccountCommandHandler(IAccountUserRepository users, IAccountRequestContext context, IAccountAudit audit, IAccountRepository repository, IAccountAuthentication authentication) : ICommandHandler<LogoutAccountCommand, AccountRedirect>
{
    public async Task<AccountRedirect> HandleAsync(LogoutAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(context.UserId, cancellationToken) ?? throw new UnauthorizedAccessException();
        await audit.WriteAsync(user, AccountAuditAction.Logout, $"logout: {user.UserName}, {context.RemoteIp}", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await authentication.SignOutAsync(cancellationToken);
        return new("OtpLogin");
    }
}
