using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Commands.CreateLegacyAdminUser;
public sealed record CreateLegacyAdminUserCommand(AppUser User) : ICommand<List<AccountIdentityError>>;
public sealed class CreateLegacyAdminUserCommandHandler(IAdminUserRepository repository) : ICommandHandler<CreateLegacyAdminUserCommand, List<AccountIdentityError>>
{
    public async Task<List<AccountIdentityError>> HandleAsync(CreateLegacyAdminUserCommand request, CancellationToken ct)
    {
        var errors = new List<AccountIdentityError>();
        if (request.User.UserName == null)
            errors.Add(new("501", "نام کاربری اجباریست"));
        if (request.User.PasswordHash == null)
            errors.Add(new("502", "کلمه عبور اجباریست"));
        if (errors.Count > 0)
            return errors;
        var password = request.User.PasswordHash!;
        request.User.PasswordHash = null;
        var result = await repository.CreateAsync(request.User, password, ct);
        if (!result.Succeeded)
            return result.Errors;
        var role = await repository.AddRolesAsync(request.User, ["User"], ct);
        return role.Succeeded ? [new("200", "حسابت کاربری با موفقیت ساخته شد")] : role.Errors;
    }
}
