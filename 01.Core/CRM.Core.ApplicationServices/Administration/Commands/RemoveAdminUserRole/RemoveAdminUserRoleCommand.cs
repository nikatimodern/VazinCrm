using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Commands.RemoveAdminUserRole;
public sealed record RemoveAdminUserRoleCommand(string Id, string Role) : ICommand<AdminMutationResult>;
public sealed class RemoveAdminUserRoleCommandHandler(IAdminUserRepository repository) : ICommandHandler<RemoveAdminUserRoleCommand, AdminMutationResult>
{
    public async Task<AdminMutationResult> HandleAsync(RemoveAdminUserRoleCommand request, CancellationToken ct)
    {
        var user = await repository.FindByIdAsync(request.Id, true, ct);
        if (user == null)
            return AdminMutationResult.Missing();
        var result = await repository.RemoveRolesAsync(user, [request.Role], ct);
        return result.Succeeded ? new(200) : AdminMutationResult.Failure(result);
    }
}
