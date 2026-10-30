using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Commands.UpdateAdminUserRoles;
public sealed record UpdateAdminUserRolesCommand(string UserName, List<string>? RoleNames) : ICommand<AdminMutationResult>;
public sealed class UpdateAdminUserRolesCommandHandler(IAdminUserRepository repository) : ICommandHandler<UpdateAdminUserRolesCommand, AdminMutationResult>
{
    public async Task<AdminMutationResult> HandleAsync(UpdateAdminUserRolesCommand request, CancellationToken ct)
    {
        var user = await repository.FindByNameAsync(request.UserName, ct);
        if (user == null)
            return AdminMutationResult.Missing();
        var selected = request.RoleNames?.Distinct().ToList() ?? [];
        foreach (var role in selected)
            if (!await repository.RoleExistsAsync(role, ct))
                return new(400, "یک یا چند نقش انتخاب‌شده معتبر نیست.");
        var current = await repository.GetUserRolesAsync(user, ct);
        var remove = current.Except(selected).ToList();
        var add = selected.Except(current).ToList();
        await repository.BeginRoleUpdateAsync(ct);
        if (remove.Count > 0)
        {
            var result = await repository.RemoveRolesAsync(user, remove, ct);
            if (!result.Succeeded)
                return AdminMutationResult.Failure(result);
        }

        if (add.Count > 0)
        {
            var result = await repository.AddRolesAsync(user, add, ct);
            if (!result.Succeeded)
                return AdminMutationResult.Failure(result);
        }

        var updated = await repository.GetUserRolesAsync(user, ct);
        await repository.CommitRoleUpdateAsync(ct);
        return new(200, Roles: updated);
    }
}
