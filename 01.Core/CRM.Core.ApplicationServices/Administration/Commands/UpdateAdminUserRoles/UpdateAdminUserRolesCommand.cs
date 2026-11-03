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
        var selected = request.RoleNames?.Distinct().ToList() ?? [];
        foreach (var role in selected)
            if (!await repository.RoleExistsAsync(role, ct))
                return new(400, "یک یا چند نقش انتخاب‌شده معتبر نیست.");
        var result = await repository.ReplaceRolesAsync(request.UserName, selected, ct);
        if (!result.UserFound)
            return AdminMutationResult.Missing();
        return result.Error == null ? new(200, Roles: result.Roles) : AdminMutationResult.Failure(result.Error);
    }
}
