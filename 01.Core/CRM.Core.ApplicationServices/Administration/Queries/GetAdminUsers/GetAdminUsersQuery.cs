using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Administration.Dtos;

namespace CRM.Core.ApplicationServices.Administration.Queries.GetAdminUsers;
public sealed record GetAdminUsersQuery(int Page, int PageSize, string? Search, string EmployeeType, string AccountStatus, List<string>? Roles) : IQuery<AdminUserPage>;
public sealed class GetAdminUsersQueryHandler(IAdminUserRepository repository) : IQueryHandler<GetAdminUsersQuery, AdminUserPage>
{
    public async Task<AdminUserPage> HandleAsync(GetAdminUsersQuery request, CancellationToken ct)
    {
        var selected = request.Roles?.Where(r => !string.IsNullOrWhiteSpace(r) && r != "all").Distinct().ToList() ?? [];
        return await repository.GetPageAsync(new(Math.Max(1, request.Page), request.PageSize, request.Search, request.EmployeeType, request.AccountStatus, selected, DateTimeOffset.UtcNow), ct);
    }
}
