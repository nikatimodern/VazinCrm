using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Queries.GetAdminUserRoles;
public sealed record GetAdminUserRolesQuery(string UserName) : IQuery<IList<string>?>;
public sealed class GetAdminUserRolesQueryHandler(IAdminUserRepository repository) : IQueryHandler<GetAdminUserRolesQuery, IList<string>?>
{
    public async Task<IList<string>?> HandleAsync(GetAdminUserRolesQuery request, CancellationToken ct)
    {
        var user = await repository.FindByNameAsync(request.UserName, ct);
        return user == null ? null : await repository.GetUserRolesAsync(user, ct);
    }
}
