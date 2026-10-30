using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Commands.CreateAdminUser;
public sealed record CreateAdminUserCommand(string UserName, string PhoneNumber, string FirstName, string LastName, string Email, string Password) : ICommand<AdminMutationResult>;
public sealed class CreateAdminUserCommandHandler(IAdminUserRepository repository) : ICommandHandler<CreateAdminUserCommand, AdminMutationResult>
{
    public async Task<AdminMutationResult> HandleAsync(CreateAdminUserCommand request, CancellationToken ct)
    {
        var user = new AppUser
        {
            UserName = request.UserName,
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            IsEmployee = true
        };
        var result = await repository.CreateAsync(user, request.Password, ct);
        return result.Succeeded ? new(200, User: user) : AdminMutationResult.Failure(result);
    }
}
