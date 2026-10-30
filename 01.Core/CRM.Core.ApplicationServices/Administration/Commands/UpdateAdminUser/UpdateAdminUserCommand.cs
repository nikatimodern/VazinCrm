using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Commands.UpdateAdminUser;
public sealed record AdminUserEditResult(bool Saved, AppUser? User, List<string> Errors);
public sealed record UpdateAdminUserCommand(string Id, string? Email, string? UserName, string? FirstName, string? LastName, string? PhoneNumber, string? Password, List<string>? InternalTels) : ICommand<AdminUserEditResult>;
public sealed class UpdateAdminUserCommandHandler(IAdminUserRepository repository) : ICommandHandler<UpdateAdminUserCommand, AdminUserEditResult>
{
    public async Task<AdminUserEditResult> HandleAsync(UpdateAdminUserCommand request, CancellationToken ct)
    {
        var user = await repository.FindByIdAsync(request.Id, true, ct);
        if (user == null)
            return new(false, null, ["نام کاربری یافت نشد"]);
        user.Email = request.Email;
        user.UserName = request.UserName;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.InternalTels = request.InternalTels == null ? null : string.Join(",", request.InternalTels.Where(t => !string.IsNullOrWhiteSpace(t)));
        var errors = new List<string>();
        var validation = await repository.ValidateUserAsync(user, ct);
        errors.AddRange(validation.Errors.Select(e => e.Description));
        if (!string.IsNullOrEmpty(request.Password))
        {
            var password = await repository.ValidatePasswordAsync(user, request.Password, ct);
            errors.AddRange(password.Errors.Select(e => e.Description));
            if (password.Succeeded && validation.Succeeded)
                user.PasswordHash = repository.HashPassword(user, request.Password);
        }

        if (errors.Count == 0)
        {
            var result = await repository.UpdateAsync(user, ct);
            if (result.Succeeded)
                return new(true, null, []);
            errors.AddRange(result.Errors.Select(e => e.Description));
        }

        user.PasswordHash = "";
        return new(false, user, errors);
    }
}
