using System.Text.RegularExpressions;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.ApplicationServices.Security;

namespace CRM.Core.ApplicationServices.AccessCatalog.Services;

public sealed record AccessEntityDraft(string Key, string Title, string? Description, string DbContextName, string ModelFullName, bool IsActive);
public sealed record AccessPermissionDraft(Guid AccessEntityId, string Key, string Title, string? Description, bool IsActive, bool? SupportsScope);
public sealed record AccessEntityInfo(Guid Id, string Key, string Title, string? Description, string DbContextName, string ModelFullName, bool IsActive, int PermissionCount);
public sealed record AccessPermissionInfo(Guid Id, Guid AccessEntityId, string Key, string FullKey, string Title, string? Description, bool IsActive, bool SupportsScope);
public sealed record PermissionRemovalPreview(Guid Id, string Title, string FullKey,
    int RoleCount, int UserCount, int SelectedMemberCount);
public sealed record PermissionRemovalDraft(bool ConfirmRemoval,
    int RoleCount, int UserCount, int SelectedMemberCount);
public sealed record AccessCatalogResult(int Status, string? Message = null, Guid? Id = null, string? RoleId = null, string? UserId = null);
public sealed record RoleAccessPermissionInfo(Guid Id, string Key, string Title, bool SupportsScope, bool SupportsSelectedUsers);
public sealed record RoleAccessEntityInfo(Guid Id, string Key, string Title, IReadOnlyList<RoleAccessPermissionInfo> Permissions);
public sealed record RolePermissionsSaveDraft(string RoleId, Guid[]? PermissionIds, AccessRoleGrantDraft[]? Scopes);
public sealed record AccessRoleDraft(string? RoleId, string? Name, string? ExpectedName);
public sealed record AccessRoleDeleteDraft(string? RoleId, string? ExpectedName,
    string? Revision, RoleRemovalChoice[]? Decisions);
public sealed record AccessUserProfileDraft(
    string? UserId, string? UserName, string? FirstName, string? LastName, string? PhoneNumber,
    string? Email, string? InternalTels, string? Password, bool IsEmployee, bool IsCustomer, bool IsSupplier,
    Guid? PositionId, string[]? RoleIds, string? ExpectedConcurrencyStamp);
public sealed record ManagedRoleInfo(string Id, string Name);
public sealed record AccessUserListItem(string Id, string Name, string UserName, string Position, IReadOnlyList<string> Roles, bool IsEmployee, bool IsCustomer, bool IsSupplier);
public sealed record AccessUsersPage(int Total, int Page, int TotalPages, IReadOnlyList<AccessUserListItem> Items);
public sealed record AccessUserSaveDraft(string UserId, string[]? RoleIds, AccessUserOverride[]? Overrides, int? ScopeEditorVersion = null);

public sealed class AccessCatalogAdministration(IAccessCatalogRepository repository, IAccessModelCatalog models, IAdminUserRepository adminUsers)
{
    private static readonly Regex KeyPattern = new("^[A-Za-z][A-Za-z0-9]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static bool ValidRoleName(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        name == name.Trim() &&
        name.Length <= 100 &&
        !name.Contains(',') &&
        !name.Contains('،') &&
        !name.Any(char.IsControl);

    public IReadOnlyList<AccessModelOption> Candidates() => models.GetCandidates();

    public async Task<IReadOnlyList<ManagedRoleInfo>> RolesAsync(CancellationToken ct) =>
        (await adminUsers.GetRolesAsync(ct))
            .OrderBy(x => x.Name)
            .Select(x => new ManagedRoleInfo(x.Id, x.Name))
            .ToArray();

    public async Task<AccessCatalogResult> SaveRoleAsync(AccessRoleDraft? draft, CancellationToken ct)
    {
        if (draft is null || !ValidRoleName(draft.Name))
            return new(400, "نام نقش معتبر نیست؛ حداکثر ۱۰۰ کاراکتر و بدون ویرگول باشد.");
        var name = draft.Name!.Trim();
        if (string.IsNullOrWhiteSpace(draft.RoleId))
        {
            var created = await adminUsers.CreateRoleAsync(name, ct);
            return new(created.Status, created.Message, RoleId: created.RoleId);
        }
        if (draft.RoleId.Length > 450 || string.IsNullOrWhiteSpace(draft.ExpectedName))
            return new(400, "شناسه یا نام قبلی نقش معتبر نیست.");

        var current = (await adminUsers.GetRolesAsync(ct)).FirstOrDefault(x => x.Id == draft.RoleId);
        if (current is null) return new(404, "نقش پیدا نشد.");
        var updated = await adminUsers.RenameRoleAsync(current.Id, draft.ExpectedName!, name, ct);
        return new(updated.Status, updated.Message, RoleId: updated.RoleId);
    }

    public Task<RoleRemovalPreview?> RoleRemovalPreviewAsync(string roleId, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(roleId) || roleId.Length > 450
            ? Task.FromResult<RoleRemovalPreview?>(null)
            : adminUsers.GetRoleRemovalPreviewAsync(roleId, ct);

    public async Task<AccessCatalogResult> DeleteRoleAsync(AccessRoleDeleteDraft? draft, CancellationToken ct)
    {
        if (draft is null || string.IsNullOrWhiteSpace(draft.RoleId) || draft.RoleId.Length > 450 ||
            string.IsNullOrWhiteSpace(draft.ExpectedName) || string.IsNullOrWhiteSpace(draft.Revision) ||
            draft.Decisions is null || draft.Decisions.Length > 20000 ||
            draft.Decisions.Any(x => string.IsNullOrWhiteSpace(x.UserId) || x.UserId.Length > 450 || x.PermissionId == Guid.Empty))
            return new(400, "اطلاعات تعیین تکلیف کاربران یا نقش معتبر نیست.");

        var result = await adminUsers.RemoveRoleAsync(draft.RoleId, draft.ExpectedName,
            draft.Revision, draft.Decisions, ct);
        return new(result.Status, result.Message);
    }

    public Task<IReadOnlyList<AdminPosition>> PositionsAsync(CancellationToken ct) =>
        adminUsers.GetPositionsAsync(ct);

    public Task<AdminUserProfile?> UserProfileAsync(string userId, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(userId) || userId.Length > 450
            ? Task.FromResult<AdminUserProfile?>(null)
            : adminUsers.GetUserProfileAsync(userId, ct);

    public async Task<AccessCatalogResult> SaveUserProfileAsync(
        AccessUserProfileDraft? draft, string? actorId, CancellationToken ct)
    {
        if (draft is null || string.IsNullOrWhiteSpace(draft.UserName) ||
            draft.UserName.Trim().Length is < 3 or > 100 ||
            string.IsNullOrWhiteSpace(draft.FirstName) || draft.FirstName.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(draft.LastName) || draft.LastName.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(draft.PhoneNumber) || draft.PhoneNumber.Trim().Length is < 8 or > 30 ||
            (draft.Email?.Length ?? 0) > 256 || (draft.InternalTels?.Length ?? 0) > 250 ||
            (draft.UserId?.Length ?? 0) > 450 ||
            !(draft.IsEmployee || draft.IsCustomer || draft.IsSupplier) ||
            draft.RoleIds is null || draft.RoleIds.Length > 100 ||
            draft.RoleIds.Any(string.IsNullOrWhiteSpace) ||
            draft.RoleIds.Distinct(StringComparer.Ordinal).Count() != draft.RoleIds.Length ||
            (draft.Password?.Length ?? 0) > 128)
            return new(400, "اطلاعات کاربر معتبر نیست. مشخصات و حداقل یک نوع حساب را وارد کنید.");

        if (string.IsNullOrWhiteSpace(draft.UserId) && string.IsNullOrWhiteSpace(draft.Password))
            return new(400, "برای کاربر جدید رمز عبور الزامی است.");

        if (!string.IsNullOrWhiteSpace(draft.UserId) &&
            string.IsNullOrWhiteSpace(draft.ExpectedConcurrencyStamp))
            return new(400, "نسخه اطلاعات کاربر مشخص نیست. فرم را دوباره باز کنید.");

        var email = string.IsNullOrWhiteSpace(draft.Email) ? null : draft.Email.Trim();
        if (email is not null &&
            (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)))
            return new(400, "آدرس ایمیل معتبر نیست.");

        var roleIds = draft.RoleIds.ToHashSet(StringComparer.Ordinal);
        var available = await adminUsers.GetRolesAsync(ct);
        if (roleIds.Any(id => available.All(r => r.Id != id)))
            return new(400, "یک یا چند نقش انتخاب‌شده وجود ندارد.");

        // Match the existing self-lockout safeguard used when assigning permissions.
        if (string.Equals(draft.UserId, actorId, StringComparison.Ordinal))
        {
            var adminRoleId = available.FirstOrDefault(r => r.Name == AppRoles.Admin)?.Id;
            if (adminRoleId is not null && !roleIds.Contains(adminRoleId))
            {
                var previous = await adminUsers.GetUserProfileAsync(draft.UserId!, ct);
                if (previous?.RoleIds.Contains(adminRoleId) == true)
                    return new(409, "امکان حذف نقش مدیر ارشد از حساب خودتان وجود ندارد.");
            }
        }

        var input = new AdminUserProfileInput(
            draft.UserId, draft.UserName!.Trim(), draft.FirstName!.Trim(), draft.LastName!.Trim(),
            draft.PhoneNumber!.Trim(), email, string.IsNullOrWhiteSpace(draft.InternalTels) ? null : draft.InternalTels.Trim(),
            string.IsNullOrWhiteSpace(draft.Password) ? null : draft.Password,
            draft.IsEmployee, draft.IsCustomer, draft.IsSupplier, draft.PositionId,
            draft.RoleIds, draft.ExpectedConcurrencyStamp);
        var saved = await adminUsers.SaveUserProfileAsync(input, actorId, ct);
        return new(saved.Status, saved.Message, UserId: saved.UserId);
    }

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

    public Task<IReadOnlyList<AccessScopeUser>> ScopeUsersAsync(string search, CancellationToken ct) =>
        repository.SearchScopeUsersAsync(search, ct);

    public async Task<AccessCatalogResult> SaveUserAccessAsync(AccessUserSaveDraft? draft, string? actorId, CancellationToken ct)
    {
        if (draft is null || string.IsNullOrWhiteSpace(draft.UserId) || draft.UserId.Length > 450 ||
            draft.RoleIds is null || draft.RoleIds.Length > 100 || draft.Overrides is null ||
            draft.Overrides.Length > 5000 || draft.ScopeEditorVersion != 3)
            return new(400, "اطلاعات کاربر یا نسخه صفحه معتبر نیست؛ صفحه مدیریت دسترسی را تازه‌سازی کنید.");
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
        var scopedPermissions = (await repository.ListAsync(ct)).SelectMany(e => e.Permissions)
            .Where(p => p.SupportsScope).Select(p => p.Id).ToHashSet();
        var selectedCapable = (await repository.ListAsync(ct))
            .Where(e => e.IsActive && (e.Key is "Customer" or "Deal"))
            .SelectMany(e => e.Permissions.Where(p => p.IsActive && p.SupportsScope).Select(p => p.Id))
            .ToHashSet();
        if (draft.Overrides.Any(x => !AccessScopes.IsOverrideGrant(x.Scope) ||
             (x.IsAllowed && x.Scope != AccessScopes.All && !scopedPermissions.Contains(x.AccessPermissionId)) ||
             (x.Scope is AccessScopes.SelectedUsers or AccessScopes.AllExceptSelectedUsers &&
                  (!x.IsAllowed || !selectedCapable.Contains(x.AccessPermissionId))) ||
             (x.Scope is not (AccessScopes.SelectedUsers or AccessScopes.AllExceptSelectedUsers) &&
                  (x.SelectedUserIds?.Length ?? 0) > 0)))
            return new(400, "محدوده اختصاصی برای یک یا چند مجوز معتبر نیست.");
        foreach (var choice in draft.Overrides.Where(x => x.Scope is AccessScopes.SelectedUsers or AccessScopes.AllExceptSelectedUsers))
            if (!await repository.ValidateScopeUsersAsync(draft.UserId, choice.SelectedUserIds ?? [], ct))
                return new(400, "حداقل یک کاربر معتبر و متفاوت از کاربر فعلی انتخاب کنید.");

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
                    .Select(p => new RoleAccessPermissionInfo(p.Id, e.Key + "." + p.Key, p.Title, p.SupportsScope,
                        p.SupportsScope && (e.Key is "Customer" or "Deal")))
                    .ToArray())).ToArray();

    public async Task<IReadOnlyList<AccessRoleGrant>?> RolePermissionsAsync(string roleId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleId) ||
            !(await adminUsers.GetRolesAsync(ct)).Any(r => r.Id == roleId))
            return null;
        return await repository.GetRolePermissionGrantsAsync(roleId, ct);
    }

    public async Task<AccessCatalogResult> SaveRolePermissionsAsync(RolePermissionsSaveDraft? draft, CancellationToken ct)
    {
        if (draft is null || string.IsNullOrWhiteSpace(draft.RoleId) || draft.RoleId.Length > 450 ||
            draft.PermissionIds is null || draft.PermissionIds.Length > 5000 || draft.Scopes is null)
            return new(400, "اطلاعات نقش یا مجوزها معتبر نیست.");
        if (!(await adminUsers.GetRolesAsync(ct)).Any(r => r.Id == draft.RoleId))
            return new(404, "نقش موردنظر پیدا نشد.");

        var activeIds = (await repository.ListAsync(ct)).Where(e => e.IsActive)
            .SelectMany(e => e.Permissions.Where(p => p.IsActive).Select(p => p.Id)).ToHashSet();
        var requested = draft.PermissionIds.ToHashSet();
        if (requested.Count != draft.PermissionIds.Length || requested.Any(id => !activeIds.Contains(id)))
            return new(400, "یک یا چند دسترسی تکراری، غیرفعال یا نامعتبر است.");
        // Never accept stale clients without explicit scopes: that could escalate Own to All.
        var choices = draft.Scopes;
        if (choices.Length != requested.Count ||
            choices.Select(x => x.AccessPermissionId).Distinct().Count() != choices.Length ||
            choices.Any(x => !requested.Contains(x.AccessPermissionId) || !AccessScopes.IsGrant(x.Scope)))
            return new(400, "محدوده مجوزهای نقش معتبر نیست.");
        var supportsScope = (await repository.ListAsync(ct)).SelectMany(e => e.Permissions)
            .Where(p => p.SupportsScope).Select(p => p.Id).ToHashSet();
        if (choices.Any(x => x.Scope == AccessScopes.Own && !supportsScope.Contains(x.AccessPermissionId)))
            return new(400, "برای این دسترسی امکان محدودکردن به اطلاعات خود وجود ندارد.");
        try
        {
            await repository.ReplaceActiveRolePermissionsAsync(draft.RoleId, activeIds.ToArray(), choices, ct);
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
            .Select(x => new AccessPermissionInfo(x.Id, entity.Id, x.Key, entity.Key + "." + x.Key, x.Title, x.Description, x.IsActive, x.SupportsScope)).ToArray();
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
                    new AccessPermission { Key = "View", Title = "مشاهده", IsActive = true, SupportsScope = true },
                    new AccessPermission { Key = "Create", Title = "ایجاد", IsActive = true },
                    new AccessPermission { Key = "Edit", Title = "ویرایش", IsActive = true, SupportsScope = true },
                    new AccessPermission { Key = "Delete", Title = "حذف", IsActive = true, SupportsScope = true }
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
        if (draft is null || !Valid(draft.Key, draft.Title, draft.Description) || draft.SupportsScope is null)
            return new(400, "مشخصات دسترسی معتبر نیست؛ صفحه را به‌روزرسانی کنید.");

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
        if (permission.SupportsScope && !draft.SupportsScope.Value &&
            await repository.HasOwnScopeAssignmentsAsync(permission.Id, ct))
            return new(409, "ابتدا مجوزهای «فقط خود» این دسترسی را به «همه» تغییر دهید.");
        permission.IsActive = draft.IsActive;
        permission.SupportsScope = draft.SupportsScope.Value;
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

    public async Task<PermissionRemovalPreview?> PermissionRemovalPreviewAsync(Guid id, CancellationToken ct)
    {
        var permission = await repository.FindPermissionAsync(id, ct);
        if (permission is null) return null;
        var parent = await repository.FindEntityAsync(permission.AccessEntityId, ct);
        if (parent is null) return null;
        var counts = await repository.PermissionAssignmentsAsync(id, ct);
        return new PermissionRemovalPreview(permission.Id, permission.Title,
            parent.Key + "." + permission.Key,
            counts.RoleCount, counts.UserCount, counts.SelectedMemberCount);
    }

    public async Task<AccessCatalogResult> DeletePermissionAsync(
        Guid id, PermissionRemovalDraft? confirmation, CancellationToken ct)
    {
        // The server requires explicit consent to remove grants, even if a
        // client bypasses the browser's confirmation dialog.
        if (confirmation is null || !confirmation.ConfirmRemoval ||
            confirmation.RoleCount < 0 || confirmation.UserCount < 0 ||
            confirmation.SelectedMemberCount < 0)
            return new(400, "ابتدا پیام تأیید حذف دسترسی و تخصیص‌های آن را تأیید کنید.");

        var expected = new PermissionAssignmentCounts(
            confirmation.RoleCount, confirmation.UserCount, confirmation.SelectedMemberCount);
        try
        {
            var deleted = await repository.DeletePermissionWithAssignmentsAsync(id, expected, ct);
            if (deleted) return new(200, "دسترسی و تمام تخصیص‌های آن حذف شدند.");
            if (await repository.FindPermissionAsync(id, ct) is null)
                return new(404, "دسترسی پیدا نشد.");
            return new(409, "تخصیص‌های این دسترسی تغییر کرده‌اند. دوباره حذف را انتخاب و تأیید کنید.");
        }
        catch (AccessCatalogConflictException)
        {
            return new(409, "این دسترسی هم‌زمان تغییر کرده است. صفحه را به‌روزرسانی کنید.");
        }
    }

    private static bool Valid(string? key, string? title, string? description) =>
        !string.IsNullOrWhiteSpace(key) && KeyPattern.IsMatch(key) &&
        !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= 150 &&
        (description?.Length ?? 0) <= 1000;
}
