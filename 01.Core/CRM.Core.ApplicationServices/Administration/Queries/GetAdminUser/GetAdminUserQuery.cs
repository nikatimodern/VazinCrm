using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Queries.GetAdminUser;
public sealed record GetAdminUserQuery(string Id) : IQuery<AppUser?>;
public sealed class GetAdminUserQueryHandler(IAdminUserRepository repository) : IQueryHandler<GetAdminUserQuery, AppUser?>
{
    public async Task<AppUser?> HandleAsync(GetAdminUserQuery request, CancellationToken ct)
    {
        var user = await repository.FindByIdAsync(request.Id, false, ct);
        if (user != null)
            user.PasswordHash = "";
        return user;
    }
}
