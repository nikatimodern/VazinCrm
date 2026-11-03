#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;

namespace CRM.Core.Domain.AppUsers.Repositories;
public sealed record AdminRole(string Id, string Name);
public sealed record AdminUserFilter(int Page, int PageSize, string? Search, string EmployeeType, string AccountStatus, List<string> Roles, DateTimeOffset Now);
public sealed record AdminUserPage(List<AppUser> Users, int TotalCount, int Page, int PageSize, int TotalPages);
public sealed record AdminRoleUpdateResult(bool UserFound, AccountIdentityResult? Error, IList<string> Roles);
public interface IAdminUserRepository
{
    Task<List<AdminRole>> GetRolesAsync(CancellationToken ct);
    Task<AdminUserPage> GetPageAsync(AdminUserFilter filter, CancellationToken ct);
    Task<AppUser?> FindByIdAsync(string id, bool tracking, CancellationToken ct);
    Task<AppUser?> FindByNameAsync(string name, CancellationToken ct);
    Task<IList<string>> GetUserRolesAsync(AppUser user, CancellationToken ct);
    Task<bool> RoleExistsAsync(string role, CancellationToken ct);
    Task<AccountIdentityResult> CreateAsync(AppUser user, string password, CancellationToken ct);
    Task<AccountIdentityResult> UpdateAsync(AppUser user, CancellationToken ct);
    Task<AccountIdentityResult> DeleteAsync(AppUser user, CancellationToken ct);
    Task<AccountIdentityResult> ValidateUserAsync(AppUser user, CancellationToken ct);
    Task<AccountIdentityResult> ValidatePasswordAsync(AppUser user, string password, CancellationToken ct);
    string HashPassword(AppUser user, string password);
    Task<AccountIdentityResult> AddRolesAsync(AppUser user, IEnumerable<string> roles, CancellationToken ct);
    Task<AccountIdentityResult> RemoveRolesAsync(AppUser user, IEnumerable<string> roles, CancellationToken ct);
    Task<AdminRoleUpdateResult> ReplaceRolesAsync(string userName, IReadOnlyCollection<string> selectedRoles, CancellationToken ct);
}
