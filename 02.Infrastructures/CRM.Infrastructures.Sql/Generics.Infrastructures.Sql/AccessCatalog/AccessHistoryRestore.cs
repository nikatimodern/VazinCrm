#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.AccessCatalog;

public sealed partial class AccessHistoryService
{
    public async Task<AccessHistoryResult> RestoreAsync(
        Guid id, string mode, string revision, string? actorId, CancellationToken ct)
    {
        if (mode is not ("single" or "point") ||
            string.IsNullOrWhiteSpace(revision) || string.IsNullOrWhiteSpace(actorId))
            return new(400, "درخواست بازگردانی معتبر نیست.");
        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var entry = await db.AccessHistoryEntries.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id && x.AfterJson != null, ct);
                if (entry is null) return new AccessHistoryResult(404, "نقطه بازگردانی پیدا نشد.");
                var current = await CaptureAsync(ct);
                if (!string.Equals(Revision(current), revision, StringComparison.Ordinal))
                    return new AccessHistoryResult(409, "سطح دسترسی از زمان نمایش پیش‌نمایش تغییر کرده است. دوباره بررسی کنید.");

                var plan = Plan(current, entry, mode);
                if (plan.Conflicts != 0)
                    return new AccessHistoryResult(409, "برخی از این تنظیمات پس از تغییر انتخاب‌شده ویرایش شده‌اند؛ بازگردانی تکی تداخل دارد.");
                var changes = Delta(current, plan.Target);
                if (changes.Count == 0) return new AccessHistoryResult(409, "این وضعیت همین حالا نیز برقرار است.");

                var checkedState = await ValidateTargetAsync(current, plan.Target, actorId, ct);
                if (checkedState is not null) return new AccessHistoryResult(409, checkedState);
                await ApplyStateAsync(plan.Target, ct);
                var actual = await CaptureAsync(ct);
                if (Delta(plan.Target, actual).Count != 0)
                    throw new InvalidOperationException("نتیجه بازگردانی با نسخه موردنظر مطابقت ندارد.");

                // The pre-restore state is retained within the same transaction, so
                // reversing this restoration is possible via history as well.
                db.AccessHistoryEntries.Add(new AccessHistoryEntry
                {
                    ActorId = actorId, Action = mode == "single" ? "RestoreSingle" : "RestorePoint",
                    Kind = "restore", Title = mode == "single" ?
                        "برگرداندن تغییر «" + entry.Title + "»" :
                        "بازگردانی کامل به «" + entry.Title + "»",
                    BeforeJson = ToJson(current), AfterJson = ToJson(actual),
                    ChangeCount = changes.Count
                });
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return new AccessHistoryResult(200, "بازگردانی انجام شد و وضعیت قبلی نیز در تاریخچه ثبت شد.");
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(409, "داده‌ها هم‌زمان تغییر کرده‌اند؛ دوباره پیش‌نمایش بگیرید.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql &&
                                           sql.Number is 547 or 2601 or 2627 or 1205)
        {
            return new(409, "بازگردانی به‌دلیل مغایرت یا ارتباط با داده‌های دیگر انجام نشد.");
        }
    }

    private async Task<string?> ValidateTargetAsync(
        List<StateItem> current, List<StateItem> target, string actorId, CancellationToken ct)
    {
        var roles = target.Where(x => x.Kind == "role")
            .Select(x => Parse<RoleState>(x.Data)).ToArray();
        var admins = roles.Where(x => x.Name == "مدیر ارشد").Select(x => x.Id).ToHashSet();
        var memberships = target.Where(x => x.Kind == "membership")
            .Select(x => Parse<UserRoleState>(x.Data)).ToArray();
        if (admins.Count == 0 || !memberships.Any(x => x.UserId == actorId && admins.Contains(x.RoleId)))
            return "بازگردانی نباید نقش مدیر ارشد را از حساب فعلی حذف کند.";

        var permissions = target.Where(x => x.Kind == "permission")
            .Select(x => Parse<PermissionState>(x.Data)).ToArray();
        var entityIds = target.Where(x => x.Kind == "entity")
            .Select(x => Parse<EntityState>(x.Data).Id).ToHashSet();
        var permissionIds = permissions.Select(x => x.Id).ToHashSet();
        var roleIds = roles.Select(x => x.Id).ToHashSet();
        var users = memberships.Select(x => x.UserId)
            .Concat(target.Where(x => x.Kind == "override")
                .Select(x => Parse<UserOverrideState>(x.Data).UserId))
            .Distinct().ToArray();
        var existingUsers = await db.Users.AsNoTracking().Where(x => users.Contains(x.Id))
            .Select(x => x.Id).ToListAsync(ct);
        if (existingUsers.Count != users.Length)
            return "برخی کاربران این نسخه دیگر وجود ندارند؛ بازگردانی بدون تغییر حساب کاربران ممکن نیست.";
        if (permissions.Any(x => !entityIds.Contains(x.AccessEntityId)))
            return "برخی مجوزها به انتیتی حذف‌شده مرتبط‌اند.";
        if (memberships.Any(x => !roleIds.Contains(x.RoleId)))
            return "برخی ارتباط‌های کاربر به نقشی ناموجود اشاره می‌کنند.";
        if (target.Where(x => x.Kind == "rolegrant").Select(x => Parse<RolePermissionState>(x.Data))
            .Any(x => !roleIds.Contains(x.RoleId) || !permissionIds.Contains(x.PermissionId)))
            return "برخی مجوزهای نقش به نقش یا مجوز ناموجود اشاره می‌کنند.";
        if (target.Where(x => x.Kind == "override").Select(x => Parse<UserOverrideState>(x.Data))
            .Any(x => !permissionIds.Contains(x.PermissionId)))
            return "برخی مجوزهای اختصاصی دیگر معتبر نیستند.";

        var removedRoleIds = current.Where(x => x.Kind == "role")
            .Select(x => Parse<RoleState>(x.Data).Id).Except(roleIds).ToArray();
        if (removedRoleIds.Length != 0 &&
            await db.RoleClaims.AsNoTracking().AnyAsync(x => removedRoleIds.Contains(x.RoleId), ct))
            return "نقش‌هایی که ادعاهای Identity دارند قابل حذف خودکار نیستند.";
        return null;
    }

    private async Task ApplyStateAsync(List<StateItem> target, CancellationToken ct)
    {
        // First detach associations that would block deleting roles or permissions.
        var roles = target.Where(x => x.Kind == "role").Select(x => Parse<RoleState>(x.Data)).ToArray();
        var entities = target.Where(x => x.Kind == "entity").Select(x => Parse<EntityState>(x.Data)).ToArray();
        var perms = target.Where(x => x.Kind == "permission").Select(x => Parse<PermissionState>(x.Data)).ToArray();
        var grants = target.Where(x => x.Kind == "rolegrant").Select(x => Parse<RolePermissionState>(x.Data)).ToArray();
        var members = target.Where(x => x.Kind == "membership").Select(x => Parse<UserRoleState>(x.Data)).ToArray();
        var overrides = target.Where(x => x.Kind == "override").Select(x => Parse<UserOverrideState>(x.Data)).ToArray();

        var desiredRoleIds = roles.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var desiredEntityIds = entities.Select(x => x.Id).ToHashSet();
        var desiredPermissionIds = perms.Select(x => x.Id).ToHashSet();
        var desiredGrantKeys = grants.Select(x => (x.RoleId, x.PermissionId)).ToHashSet();
        var desiredMembershipKeys = members.Select(x => (x.UserId, x.RoleId)).ToHashSet();
        var desiredOverrideKeys = overrides.Select(x => (x.UserId, x.PermissionId)).ToHashSet();

        var currentGrants = await db.AccessRolePermissions.ToListAsync(ct);
        db.AccessRolePermissions.RemoveRange(currentGrants.Where(x =>
            !desiredGrantKeys.Contains((x.RoleId, x.AccessPermissionId))));
        var currentMembers = await db.UserRoles.ToListAsync(ct);
        db.UserRoles.RemoveRange(currentMembers.Where(x =>
            !desiredMembershipKeys.Contains((x.UserId, x.RoleId))));
        var currentOverrides = await db.AccessUserPermissions.ToListAsync(ct);
        db.AccessUserPermissions.RemoveRange(currentOverrides.Where(x =>
            !desiredOverrideKeys.Contains((x.UserId, x.AccessPermissionId))));
        await db.SaveChangesAsync(ct);

        var currentPermissions = await db.AccessPermissions.ToListAsync(ct);
        db.AccessPermissions.RemoveRange(currentPermissions.Where(x => !desiredPermissionIds.Contains(x.Id)));
        await db.SaveChangesAsync(ct);

        var currentEntities = await db.AccessEntities.ToListAsync(ct);
        db.AccessEntities.RemoveRange(currentEntities.Where(x => !desiredEntityIds.Contains(x.Id)));
        var currentRoles = await db.Roles.ToListAsync(ct);
        db.Roles.RemoveRange(currentRoles.Where(x => !desiredRoleIds.Contains(x.Id)));
        await db.SaveChangesAsync(ct);

        var entityMap = currentEntities.Where(x => desiredEntityIds.Contains(x.Id)).ToDictionary(x => x.Id);
        foreach (var item in entities)
        {
            if (!entityMap.TryGetValue(item.Id, out var entity))
            {
                entity = new AccessEntity { Id = item.Id };
                db.AccessEntities.Add(entity);
            }
            entity.Key = item.Key;
            entity.Title = item.Title;
            entity.Description = item.Description;
            entity.DbContextName = item.DbContextName;
            entity.ModelFullName = item.ModelFullName;
            entity.IsActive = item.IsActive;
        }

        var roleMap = currentRoles.Where(x => desiredRoleIds.Contains(x.Id)).ToDictionary(x => x.Id);
        foreach (var item in roles)
        {
            if (!roleMap.TryGetValue(item.Id, out var role))
            {
                role = new IdentityRole { Id = item.Id };
                db.Roles.Add(role);
            }
            if (!string.Equals(role.Name, item.Name, StringComparison.Ordinal))
                role.ConcurrencyStamp = Guid.NewGuid().ToString();
            role.Name = item.Name;
            role.NormalizedName = normalizer.NormalizeName(item.Name);
        }
        await db.SaveChangesAsync(ct);

        var permissionMap = currentPermissions.Where(x => desiredPermissionIds.Contains(x.Id))
            .ToDictionary(x => x.Id);
        foreach (var item in perms)
        {
            if (!permissionMap.TryGetValue(item.Id, out var p))
            {
                p = new AccessPermission { Id = item.Id };
                db.AccessPermissions.Add(p);
            }
            p.AccessEntityId = item.AccessEntityId;
            p.Key = item.Key;
            p.Title = item.Title;
            p.Description = item.Description;
            p.IsActive = item.IsActive;
        }
        await db.SaveChangesAsync(ct);

        var currentGrantKeys = currentGrants.Select(x => (x.RoleId, x.AccessPermissionId)).ToHashSet();
        foreach (var item in grants.Where(x => !currentGrantKeys.Contains((x.RoleId, x.PermissionId))))
            db.AccessRolePermissions.Add(new AccessRolePermission
            {
                RoleId = item.RoleId, AccessPermissionId = item.PermissionId
            });

        var currentMemberKeys = currentMembers.Select(x => (x.UserId, x.RoleId)).ToHashSet();
        foreach (var item in members.Where(x => !currentMemberKeys.Contains((x.UserId, x.RoleId))))
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = item.UserId, RoleId = item.RoleId });

        var currentOverrideMap = currentOverrides
            .Where(x => desiredOverrideKeys.Contains((x.UserId, x.AccessPermissionId)))
            .ToDictionary(x => (x.UserId, x.AccessPermissionId));
        foreach (var item in overrides)
        {
            if (currentOverrideMap.TryGetValue((item.UserId, item.PermissionId), out var existing))
                existing.IsAllowed = item.IsAllowed;
            else
                db.AccessUserPermissions.Add(new AccessUserPermission
                {
                    UserId = item.UserId, AccessPermissionId = item.PermissionId, IsAllowed = item.IsAllowed
                });
        }
        await db.SaveChangesAsync(ct);
    }
}
