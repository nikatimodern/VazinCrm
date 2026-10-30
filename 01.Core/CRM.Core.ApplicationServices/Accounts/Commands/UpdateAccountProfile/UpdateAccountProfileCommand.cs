using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.UpdateAccountProfile;

public sealed record UpdateAccountProfileCommand(string? FirstName, string? LastName, string? PhoneNumber, int ImageStatus, AccountUpload? Image) : ICommand<bool>
{
    public override string ToString() => nameof(UpdateAccountProfileCommand);
}

public sealed class UpdateAccountProfileCommandHandler(IAccountUserRepository users, IAccountRequestContext context, IAccountImages images) : ICommandHandler<UpdateAccountProfileCommand, bool>
{
    public async Task<bool> HandleAsync(UpdateAccountProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(context.UserId, cancellationToken) ?? throw new UnauthorizedAccessException();
        switch (request.ImageStatus)
        {
            case 1: user.PersonImage = null; user.PersonImageUrl = null; break;
            case 2:
                var image = images.Save(request.Image ?? throw new ArgumentNullException(nameof(request.Image)), user.Id, user.PersonImageUrl);
                user.PersonImage = image.Image; user.PersonImageUrl = image.Url; break;
        }
        user.FirstName = request.FirstName; user.LastName = request.LastName; user.PhoneNumber = request.PhoneNumber;
        await users.SaveChangesAsync(cancellationToken);
        return true;
    }
}
