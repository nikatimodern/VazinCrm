#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;

namespace CRM.Core.Domain.AppUsers.Repositories;
public sealed record AdminRole(string Id, string Name);
public sealed record AdminRoleWriteResult(int Status, string? Message = null, string? RoleId = null);
public sealed record RoleRemovalPermission(Guid Id, string Key, string Title);
public sealed record RoleRemovalUser(string Id, string Name, string UserName, IReadOnlyList<RoleRemovalPermission> Permissions);
public sealed record RoleRemovalPreview(string RoleId, string RoleName, string Revision, int AssignedUserCount,
    int AffectedPermissionCount, IReadOnlyList<RoleRemovalUser> Users);
public sealed record RoleRemovalChoice(string UserId, Guid PermissionId, bool Keep);
public sealed record RoleRemovalResult(int Status, string? Message = null);
public sealed record AdminPosition(Guid Id, string Title);
public sealed record AdminUserProfile(
    string Id, string UserName, string? FirstName, string? LastName, string? PhoneNumber, string? Email,
    string? InternalTels, bool IsEmployee, bool IsCustomer, bool IsSupplier, Guid? PositionId,
    string? PositionTxt, string[] RoleIds, string? ConcurrencyStamp);
public sealed record AdminUserProfileInput(
    string? UserId, string UserName, string FirstName, string LastName, string PhoneNumber, string? Email,
    string? InternalTels, string? Password, bool IsEmployee, bool IsCustomer, bool IsSupplier,
    Guid? PositionId, IReadOnlyList<string> RoleIds, string? ExpectedConcurrencyStamp);
public sealed record AdminUserProfileWriteResult(int Status, string? Message = null, string? UserId = null);
public sealed record AdminUserFilter(int Page, int PageSize, string? Search, string EmployeeType, string AccountStatus, List<string> Roles, DateTimeOffset Now, string? UserType = null);
public sealed record AdminUserPage(List<AppUser> Users, int TotalCount, int Page, int PageSize, int TotalPages);
public sealed record AdminRoleUpdateResult(bool UserFound, AccountIdentityResult? Error, IList<string> Roles);
public interface IAdminUserRepository
{
    Task<List<AdminRole>> GetRolesAsync(CancellationToken ct);
    Task<IReadOnlyList<AdminPosition>> GetPositionsAsync(CancellationToken ct);
    Task<AdminUserProfile?> GetUserProfileAsync(string userId, CancellationToken ct);
    Task<AdminUserProfileWriteResult> SaveUserProfileAsync(AdminUserProfileInput input, string? actorId, CancellationToken ct);
    Task<AdminRoleWriteResult> CreateRoleAsync(string name, CancellationToken ct);
    Task<AdminRoleWriteResult> RenameRoleAsync(string roleId, string expectedName, string name, CancellationToken ct);
    Task<RoleRemovalPreview?> GetRoleRemovalPreviewAsync(string roleId, CancellationToken ct);
    Task<RoleRemovalResult> RemoveRoleAsync(string roleId, string expectedName, string revision,
        IReadOnlyList<RoleRemovalChoice> choices, CancellationToken ct);
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
