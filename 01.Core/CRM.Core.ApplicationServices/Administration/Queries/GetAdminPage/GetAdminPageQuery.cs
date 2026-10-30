using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Queries.GetAdminPage;
public sealed record GetAdminPageQuery() : IQuery<List<AdminRole>>;
public sealed class GetAdminPageQueryHandler(IAdminUserRepository repository) : IQueryHandler<GetAdminPageQuery, List<AdminRole>>
{
    public async Task<List<AdminRole>> HandleAsync(GetAdminPageQuery request, CancellationToken ct)
    {
        return await repository.GetRolesAsync(ct);
    }
}
