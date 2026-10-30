using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.HandleAccountAccessDenied;

public sealed record HandleAccountAccessDeniedCommand(bool ShowNotifyOnly) : ICommand<bool>;

public sealed class HandleAccountAccessDeniedCommandHandler(IAccountRequestContext context, IAccountUserRepository users, IAccountNotifications notifications, IAccountAuthentication authentication) : ICommandHandler<HandleAccountAccessDeniedCommand, bool>
{
    public async Task<bool> HandleAsync(HandleAccountAccessDeniedCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(context.UserId, cancellationToken);
        if (request.ShowNotifyOnly)
            await notifications.AccessDeniedAsync(user?.Id ?? throw new UnauthorizedAccessException(), cancellationToken);
        else await authentication.SignOutAsync(cancellationToken);
        return request.ShowNotifyOnly;
    }
}
