using System.Text.RegularExpressions;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Security;

namespace CRM.Core.ApplicationServices.AccessCatalog.Services;

public sealed record AccessEntityDraft(string Key, string Title, string? Description, string DbContextName, string ModelFullName, bool IsActive);
public sealed record AccessPermissionDraft(Guid AccessEntityId, string Key, string Title, string? Description, bool IsActive);
public sealed record AccessEntityInfo(Guid Id, string Key, string Title, string? Description, string DbContextName, string ModelFullName, bool IsActive, int PermissionCount);
public sealed record AccessPermissionInfo(Guid Id, Guid AccessEntityId, string Key, string FullKey, string Title, string? Description, bool IsActive);
public sealed record AccessCatalogResult(int Status, string? Message = null, Guid? Id = null);
public sealed record RoleAccessPermissionInfo(Guid Id, string Key, string Title);
public sealed record RoleAccessEntityInfo(Guid Id, string Key, string Title, IReadOnlyList<RoleAccessPermissionInfo> Permissions);
public sealed record RolePermissionsSaveDraft(string RoleId, Guid[]? PermissionIds);
public sealed record AccessUserListItem(string Id, string Name, string UserName, string Position, IReadOnlyList<string> Roles, bool IsEmployee, bool IsCustomer, bool IsSupplier);
public sealed record AccessUsersPage(int Total, int Page, int TotalPages, IReadOnlyList<AccessUserListItem> Items);
public sealed record AccessUserSaveDraft(string UserId, string[]? RoleIds, AccessUserOverride[]? Overrides);

public sealed class AccessCatalogAdministration(IAccessCatalogRepository repository, IAccessModelCatalog models, IAdminUserRepository adminUsers)
{
    private static readonly Regex KeyPattern = new("^[A-Za-z][A-Za-z0-9]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public IReadOnlyList<AccessModelOption> Candidates() => models.GetCandidates();

    public Task<List<AdminRole>> RolesAsync(CancellationToken ct) => adminUsers.GetRolesAsync(ct);

    public async Task<AccessUsersPage> UsersAsync(string? search, int page, string? userType, CancellationToken ct)
    {
        // Default to employees even when the caller does not supply a valid filter.
        var type = userType is "all" or "customer" or "supplier" ? userType : "employee";
        var filter = new AdminUserFilter(Math.Max(page, 1), 25, search, "all", "all",
            new List<string>(), DateTimeOffset.UtcNow, UserType: type);
        var data = await adminUsers.GetPageAsync(filter, ct);
        return new AccessUsersPage(data.TotalCount, data.Page, data.TotalPages,
            data.Users.Select(u => new AccessUserListItem(u.Id,
                string.IsNullOrWhiteSpace(u.FullName) ? u.UserName ?? "بدون نام" : u.FullName,
                u.UserName ?? "", u.PositionTxt ?? "", u.RolsUsers?.ToArray() ?? [],
                u.IsEmployee, u.IsCustomer, u.IsSupplier)).ToArray());
    }

    public Task<AccessUserState?> UserAccessAsync(string userId, CancellationToken ct) =>
        repository.UserAccessAsync(userId, ct);

    public Task<IReadOnlyList<AccessRoleGrant>> RoleGrantsAsync(CancellationToken ct) =>
        repository.RoleGrantsAsync(ct);

    public async Task<AccessCatalogResult> SaveUserAccessAsync(AccessUserSaveDraft? draft, string? actorId, CancellationToken ct)
    {
        if (draft is null || string.IsNullOrWhiteSpace(draft.UserId) || draft.UserId.Length > 450 ||
            draft.RoleIds is null || draft.RoleIds.Length > 100 || draft.Overrides is null ||
            draft.Overrides.Length > 5000)
            return new(400, "اطلاعات کاربر، نقش‌ها یا مجوزها معتبر نیست.");
        var roleIds = draft.RoleIds.ToHashSet(StringComparer.Ordinal);
        if (roleIds.Count != draft.RoleIds.Length || roleIds.Any(x => string.IsNullOrWhiteSpace(x)))
            return new(400, "نقش‌های تکراری یا نامعتبر ارسال شده است.");
        var allRoles = await adminUsers.GetRolesAsync(ct);
        if (roleIds.Any(id => allRoles.All(r => r.Id != id)))
            return new(400, "نقش نامعتبر انتخاب شده است.");

        var permissionIds = (await repository.ListAsync(ct)).Where(e => e.IsActive)
            .SelectMany(e => e.Permissions.Where(p => p.IsActive).Select(p => p.Id)).ToHashSet();
        var overrideIds = draft.Overrides.Select(x => x.AccessPermissionId).ToArray();
        if (overrideIds.Distinct().Count() != overrideIds.Length || overrideIds.Any(id => !permissionIds.Contains(id)))
            return new(400, "یک یا چند دسترسی اختصاصی تکراری یا نامعتبر است.");

        // An administrator must not accidentally remove their own Admin role.
        if (string.Equals(draft.UserId, actorId, StringComparison.Ordinal))
        {
            var adminId = allRoles.FirstOrDefault(r => r.Name == AppRoles.Admin)?.Id;
            if (adminId is not null && !roleIds.Contains(adminId))
            {
                var current = await repository.UserAccessAsync(draft.UserId, ct);
                if (current is not null && current.RoleIds.Contains(adminId))
                    return new(409, "امکان حذف نقش مدیر ارشد از حساب کاربری خودتان وجود ندارد.");
            }
        }

        try
        {
            var result = await repository.ReplaceUserAccessAsync(draft.UserId, roleIds.ToArray(),
                draft.Overrides, permissionIds.ToArray(), ct);
            if (result.NotFound) return new(404, "کاربر پیدا نشد.");
            return result.Success ? new(200) : new(409, result.Error ?? "ثبت تغییرات موفق نبود.");
        }
        catch (AccessCatalogConflictException)
        {
            return new(409, "اطلاعات دسترسی هم‌زمان تغییر کرده است. صفحه را تازه‌سازی کنید.");
        }
    }

    public async Task<IReadOnlyList<RoleAccessEntityInfo>> RoleCatalogAsync(CancellationToken ct) =>
        (await repository.ListAsync(ct)).Where(e => e.IsActive).OrderBy(e => e.Title)
            .Select(e => new RoleAccessEntityInfo(e.Id, e.Key, e.Title,
                e.Permissions.Where(p => p.IsActive).OrderBy(p => p.Title)
                    .Select(p => new RoleAccessPermissionInfo(p.Id, e.Key + "." + p.Key, p.Title))
                    .ToArray())).ToArray();

    public async Task<IReadOnlyList<Guid>?> RolePermissionsAsync(string roleId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleId) ||
            !(await adminUsers.GetRolesAsync(ct)).Any(r => r.Id == roleId))
            return null;
        return await repository.GetRolePermissionIdsAsync(roleId, ct);
    }

    public async Task<AccessCatalogResult> SaveRolePermissionsAsync(RolePermissionsSaveDraft? draft, CancellationToken ct)
    {
        if (draft is null || string.IsNullOrWhiteSpace(draft.RoleId) || draft.RoleId.Length > 450 ||
            draft.PermissionIds is null || draft.PermissionIds.Length > 5000)
            return new(400, "اطلاعات نقش یا مجوزها معتبر نیست.");
        if (!(await adminUsers.GetRolesAsync(ct)).Any(r => r.Id == draft.RoleId))
            return new(404, "نقش موردنظر پیدا نشد.");

        var activeIds = (await repository.ListAsync(ct)).Where(e => e.IsActive)
            .SelectMany(e => e.Permissions.Where(p => p.IsActive).Select(p => p.Id)).ToHashSet();
        var requested = draft.PermissionIds.ToHashSet();
        if (requested.Count != draft.PermissionIds.Length || requested.Any(id => !activeIds.Contains(id)))
            return new(400, "یک یا چند دسترسی تکراری، غیرفعال یا نامعتبر است.");
        try
        {
            await repository.ReplaceActiveRolePermissionsAsync(draft.RoleId, activeIds.ToArray(), requested.ToArray(), ct);
            return new(200);
        }
        catch (AccessCatalogConflictException)
        {
            return new(409, "دسترسی‌های نقش هم‌زمان تغییر کرده‌اند. فهرست را دوباره دریافت کنید.");
        }
    }

    public async Task<IReadOnlyList<AccessEntityInfo>> EntitiesAsync(CancellationToken ct) =>
        (await repository.ListAsync(ct))
        .OrderBy(x => x.Title)
        .Select(x => new AccessEntityInfo(x.Id, x.Key, x.Title, x.Description, x.DbContextName, x.ModelFullName, x.IsActive, x.Permissions.Count))
        .ToArray();

    public async Task<IReadOnlyList<AccessPermissionInfo>?> PermissionsAsync(Guid entityId, CancellationToken ct)
    {
        var entity = (await repository.ListAsync(ct)).FirstOrDefault(x => x.Id == entityId);
        return entity is null ? null : entity.Permissions.OrderBy(x => x.Title)
            .Select(x => new AccessPermissionInfo(x.Id, entity.Id, x.Key, entity.Key + "." + x.Key, x.Title, x.Description, x.IsActive)).ToArray();
    }

    public async Task<AccessCatalogResult> SaveEntityAsync(Guid? id, AccessEntityDraft? draft, CancellationToken ct)
    {
        if (draft is null || !Valid(draft.Key, draft.Title, draft.Description) ||
            string.IsNullOrWhiteSpace(draft.DbContextName) || string.IsNullOrWhiteSpace(draft.ModelFullName))
            return new(400, "مشخصات انتیتی معتبر نیست.");

        var candidate = models.GetCandidates().FirstOrDefault(x =>
            x.DbContextName == draft.DbContextName && x.ModelFullName == draft.ModelFullName);
        if (candidate is null) return new(400, "مدل انتخاب‌شده در Context موجود نیست.");

        AccessEntity? entity = null;
        if (id.HasValue)
        {
            entity = await repository.FindEntityAsync(id.Value, ct);
            if (entity is null) return new(404, "انتیتی پیدا نشد.");
            if (!string.Equals(entity.Key, draft.Key, StringComparison.Ordinal) ||
                entity.DbContextName != draft.DbContextName || entity.ModelFullName != draft.ModelFullName)
                return new(400, "کلید و مدل انتیتی بعد از ثبت قابل تغییر نیست.");
        }

        if (await repository.EntityKeyExistsAsync(draft.Key, id, ct) ||
            await repository.ModelExistsAsync(draft.DbContextName, draft.ModelFullName, id, ct))
            return new(409, "این کلید یا مدل قبلاً ثبت شده است.");

        if (entity is null)
        {
            entity = new AccessEntity
            {
                Key = draft.Key.Trim(),
                DbContextName = draft.DbContextName,
                ModelFullName = draft.ModelFullName,
                Permissions =
                [
                    new AccessPermission { Key = "View", Title = "مشاهده", IsActive = true },
                    new AccessPermission { Key = "Create", Title = "ایجاد", IsActive = true },
                    new AccessPermission { Key = "Edit", Title = "ویرایش", IsActive = true },
                    new AccessPermission { Key = "Delete", Title = "حذف", IsActive = true }
                ]
            };
            repository.Add(entity);
        }
        entity.Title = draft.Title.Trim();
        entity.Description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim();
        entity.IsActive = draft.IsActive;

        try { await repository.SaveAsync(ct); }
        catch (AccessCatalogConflictException) { return new(409, "این کلید یا مدل قبلاً ثبت شده است."); }
        return new(200, Id: entity.Id);
    }

    public async Task<AccessCatalogResult> SavePermissionAsync(Guid? id, AccessPermissionDraft? draft, CancellationToken ct)
    {
        if (draft is null || !Valid(draft.Key, draft.Title, draft.Description))
            return new(400, "مشخصات دسترسی معتبر نیست.");

        var parent = await repository.FindEntityAsync(draft.AccessEntityId, ct);
        if (parent is null) return new(404, "انتیتی مربوط پیدا نشد.");

        AccessPermission? permission = null;
        if (id.HasValue)
        {
            permission = await repository.FindPermissionAsync(id.Value, ct);
            if (permission is null) return new(404, "دسترسی پیدا نشد.");
            if (permission.AccessEntityId != draft.AccessEntityId || !string.Equals(permission.Key, draft.Key, StringComparison.Ordinal))
                return new(400, "کلید یا انتیتی دسترسی بعد از ثبت قابل تغییر نیست.");
        }
        if (await repository.PermissionKeyExistsAsync(parent.Id, draft.Key, id, ct))
            return new(409, "این دسترسی قبلاً برای انتیتی ثبت شده است.");

        if (permission is null)
        {
            permission = new AccessPermission { AccessEntityId = parent.Id, Key = draft.Key.Trim() };
            repository.Add(permission);
        }
        permission.Title = draft.Title.Trim();
        permission.Description = string.IsNullOrWhiteSpace(draft.Description) ? null : draft.Description.Trim();
        permission.IsActive = draft.IsActive;
        try { await repository.SaveAsync(ct); }
        catch (AccessCatalogConflictException) { return new(409, "این دسترسی قبلاً ثبت شده است."); }
        return new(200, Id: permission.Id);
    }

    public async Task<AccessCatalogResult> DeleteEntityAsync(Guid id, CancellationToken ct)
    {
        var entity = await repository.FindEntityAsync(id, ct);
        if (entity is null) return new(404, "انتیتی پیدا نشد.");
        var all = await repository.ListAsync(ct);
        if (all.First(x => x.Id == id).Permissions.Count > 0)
            return new(409, "ابتدا دسترسی‌های این انتیتی را حذف کنید.");
        repository.Remove(entity);
        await repository.SaveAsync(ct);
        return new(200);
    }

    public async Task<AccessCatalogResult> DeletePermissionAsync(Guid id, CancellationToken ct)
    {
        var permission = await repository.FindPermissionAsync(id, ct);
        if (permission is null) return new(404, "دسترسی پیدا نشد.");
        if (await repository.PermissionHasRoleAssignmentsAsync(id, ct))
            return new(409, "این دسترسی به نقش یا کاربر اختصاص داده شده و قابل حذف نیست.");
        repository.Remove(permission);
        await repository.SaveAsync(ct);
        return new(200);
    }

    private static bool Valid(string? key, string? title, string? description) =>
        !string.IsNullOrWhiteSpace(key) && KeyPattern.IsMatch(key) &&
        !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= 150 &&
        (description?.Length ?? 0) <= 1000;
}
