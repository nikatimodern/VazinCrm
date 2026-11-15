#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.AccessCatalog;

// Access policy history is independent of user profile data and credentials.
// A row stores both sides of one committed management operation.
public sealed partial class AccessHistoryService : IAccessHistoryService
{
    private readonly UserContextDb db;
    private readonly ILookupNormalizer normalizer;
    public AccessHistoryService(UserContextDb context, ILookupNormalizer lookupNormalizer)
    {
        db = context;
        normalizer = lookupNormalizer;
    }

    private sealed record RoleState(string Id, string Name);
    private sealed record EntityState(Guid Id, string Key, string Title, string? Description,
        string DbContextName, string ModelFullName, bool IsActive);
    private sealed record PermissionState(Guid Id, Guid AccessEntityId, string Key, string Title,
        string? Description, bool IsActive);
    private sealed record RolePermissionState(string RoleId, Guid PermissionId);
    private sealed record UserRoleState(string UserId, string RoleId);
    private sealed record UserOverrideState(string UserId, Guid PermissionId, bool IsAllowed);
    private sealed record StateItem(string Key, string Kind, string Data);
    private sealed record StateDiff(string Key, StateItem? Before, StateItem? After);
    private sealed record RestorePlan(List<StateItem> Target, int Conflicts);

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    private static T Parse<T>(string data) where T : class => JsonSerializer.Deserialize<T>(data)
        ?? throw new InvalidOperationException("نسخه ذخیره‌شده ناقص است.");
    private static List<StateItem> FromJson(string json) => Parse<List<StateItem>>(json);
    private static string ToJson(IEnumerable<StateItem> state) =>
        Serialize(state.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray());
    private static string Revision(IEnumerable<StateItem> state) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ToJson(state))));

    private async Task<List<StateItem>> CaptureAsync(CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking()
            .Select(x => new RoleState(x.Id, x.Name ?? "")).ToListAsync(ct);
        var entities = await db.AccessEntities.AsNoTracking()
            .Select(x => new EntityState(x.Id, x.Key, x.Title, x.Description,
                x.DbContextName, x.ModelFullName, x.IsActive)).ToListAsync(ct);
        var permissions = await db.AccessPermissions.AsNoTracking()
            .Select(x => new PermissionState(x.Id, x.AccessEntityId, x.Key, x.Title,
                x.Description, x.IsActive)).ToListAsync(ct);
        var rolePermissions = await db.AccessRolePermissions.AsNoTracking()
            .Select(x => new RolePermissionState(x.RoleId, x.AccessPermissionId)).ToListAsync(ct);
        var userRoles = await db.UserRoles.AsNoTracking()
            .Select(x => new UserRoleState(x.UserId, x.RoleId)).ToListAsync(ct);
        var overrides = await db.AccessUserPermissions.AsNoTracking()
            .Select(x => new UserOverrideState(x.UserId, x.AccessPermissionId, x.IsAllowed)).ToListAsync(ct);
        var items = new List<StateItem>(roles.Count + entities.Count + permissions.Count +
                                        rolePermissions.Count + userRoles.Count + overrides.Count);
        items.AddRange(roles.Select(x => new StateItem("role|" + x.Id, "role", Serialize(x))));
        items.AddRange(entities.Select(x => new StateItem("entity|" + x.Id, "entity", Serialize(x))));
        items.AddRange(permissions.Select(x => new StateItem("permission|" + x.Id, "permission", Serialize(x))));
        items.AddRange(rolePermissions.Select(x => new StateItem("rolegrant|" + x.RoleId + "|" + x.PermissionId,
            "rolegrant", Serialize(x))));
        items.AddRange(userRoles.Select(x => new StateItem("membership|" + x.UserId + "|" + x.RoleId,
            "membership", Serialize(x))));
        items.AddRange(overrides.Select(x => new StateItem("override|" + x.UserId + "|" + x.PermissionId,
            "override", Serialize(x))));
        return items.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
    }

    private static List<StateDiff> Delta(IEnumerable<StateItem> before, IEnumerable<StateItem> after)
    {
        var a = before.ToDictionary(x => x.Key, StringComparer.Ordinal);
        var b = after.ToDictionary(x => x.Key, StringComparer.Ordinal);
        return a.Keys.Union(b.Keys, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .Where(key => a.GetValueOrDefault(key)?.Data != b.GetValueOrDefault(key)?.Data)
            .Select(key => new StateDiff(key, a.GetValueOrDefault(key), b.GetValueOrDefault(key))).ToList();
    }

    private static (string Kind, string Title) Operation(string action) => action switch
    {
        "SaveRole" => ("roles", "ایجاد یا ویرایش نقش"),
        "DeleteRole" => ("roles", "حذف نقش و تعیین تکلیف مجوزها"),
        "SaveRolePermissions" => ("roles", "تغییر مجوزهای نقش"),
        "SaveUserAccess" => ("users", "تغییر نقش‌ها و مجوزهای کاربر"),
        "SaveUserProfile" => ("users", "تغییر نقش‌های کاربر"),
        "SaveEntity" => ("catalog", "ایجاد یا ویرایش انتیتی"),
        "DeleteEntity" => ("catalog", "حذف انتیتی"),
        "SavePermission" => ("catalog", "ایجاد یا ویرایش مجوز"),
        "DeletePermission" => ("catalog", "حذف مجوز"),
        _ => ("catalog", "تغییر تنظیمات سطح دسترسی")
    };

    public async Task<Guid> BeginAsync(string action, string? actorId, CancellationToken ct)
    {
        var (kind, title) = Operation(action);
        var before = ToJson(await CaptureAsync(ct));
        // The first administrative change also establishes an initial full restore point.
        if (!await db.AccessHistoryEntries.AsNoTracking().AnyAsync(x => x.AfterJson != null, ct))
            db.AccessHistoryEntries.Add(new AccessHistoryEntry
            {
                ActorId = actorId ?? "", Action = "Baseline", Kind = "manual",
                Title = "وضعیت اولیه مدیریت سطح دسترسی",
                BeforeJson = before, AfterJson = before
            });
        var entry = new AccessHistoryEntry
        {
            ActorId = actorId ?? "",
            Action = action, Kind = kind, Title = title, BeforeJson = before
        };
        db.AccessHistoryEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task FinishAsync(Guid id, bool succeeded, CancellationToken ct)
    {
        var entry = await db.AccessHistoryEntries.FindAsync(new object[] { id }, ct);
        if (entry is null) return;
        if (!succeeded)
        {
            db.AccessHistoryEntries.Remove(entry);
            await db.SaveChangesAsync(ct);
            return;
        }
        var after = await CaptureAsync(ct);
        var before = FromJson(entry.BeforeJson);
        var count = Delta(before, after).Count;
        if (count == 0) db.AccessHistoryEntries.Remove(entry);
        else
        {
            entry.AfterJson = ToJson(after);
            entry.ChangeCount = count;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<AccessHistoryResult> CreateManualAsync(string title, string? actorId, CancellationToken ct)
    {
        title = title.Trim();
        if (title.Length is < 3 or > 100 || title.Any(char.IsControl))
            return new(400, "عنوان نقطه بازگردانی باید بین ۳ تا ۱۰۰ کاراکتر باشد.");
        var snapshot = ToJson(await CaptureAsync(ct));
        db.AccessHistoryEntries.Add(new AccessHistoryEntry
        {
            ActorId = actorId ?? "", Kind = "manual", Action = "Manual", Title = title,
            BeforeJson = snapshot, AfterJson = snapshot
        });
        await db.SaveChangesAsync(ct);
        return new(200, "نقطه بازگردانی ثبت شد.");
    }

    private async Task<Dictionary<string, string>> ActorNamesAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var list = ids.Distinct(StringComparer.Ordinal).ToArray();
        return await db.Users.AsNoTracking().Where(x => list.Contains(x.Id))
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.UserName }).ToDictionaryAsync(
                x => x.Id, x =>
                    (x.FirstName + " " + x.LastName).Trim().Length == 0 ?
                        x.UserName ?? x.Id : (x.FirstName + " " + x.LastName).Trim(), ct);
    }

    public async Task<AccessHistoryPage> ListAsync(int page, string? kind, string? search, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 100000);
        var query = db.AccessHistoryEntries.AsNoTracking().Where(x => x.AfterJson != null);
        if (kind is "roles" or "users" or "catalog" or "restore" or "manual")
            query = query.Where(x => x.Kind == kind);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100) term = term[..100];
            query = query.Where(x => x.Title.Contains(term) || x.Action.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 20).Take(20).ToListAsync(ct);
        var actors = await ActorNamesAsync(items.Select(x => x.ActorId), ct);
        return new AccessHistoryPage(page, total, Math.Max(1, (total + 19) / 20),
            items.Select(x => new AccessHistoryItem(x.Id, x.CreatedAtUtc, x.Kind, x.Title,
                actors.GetValueOrDefault(x.ActorId, "کاربر نامشخص"), x.ChangeCount)).ToArray());
    }

    private async Task<IReadOnlyList<AccessHistoryChange>> ChangesAsync(
        List<StateItem> before, List<StateItem> after, CancellationToken ct)
    {
        var differences = Delta(before, after);
        var all = after.Concat(before).ToArray();
        var roles = all.Where(x => x.Kind == "role").Select(x => Parse<RoleState>(x.Data))
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Name);
        var entities = all.Where(x => x.Kind == "entity").Select(x => Parse<EntityState>(x.Data))
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Key);
        var permissions = all.Where(x => x.Kind == "permission").Select(x => Parse<PermissionState>(x.Data))
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());
        var userIds = all.Where(x => x.Kind is "membership" or "override")
            .Select(x => x.Kind == "membership" ? Parse<UserRoleState>(x.Data).UserId :
                Parse<UserOverrideState>(x.Data).UserId).Distinct();
        var users = await ActorNamesAsync(userIds, ct);
        string role(string id) => roles.GetValueOrDefault(id, id);
        string perm(Guid id) => permissions.TryGetValue(id, out var p) ?
            entities.GetValueOrDefault(p.AccessEntityId, "") + "." + p.Key : id.ToString();
        string user(string id) => users.GetValueOrDefault(id, id);
        string title(StateDiff d)
        {
            var value = d.After ?? d.Before!;
            return value.Kind switch
            {
                "role" => "نقش «" + Parse<RoleState>(value.Data).Name + "»",
                "entity" => "انتیتی «" + Parse<EntityState>(value.Data).Title + "»",
                "permission" => "مجوز «" + perm(Parse<PermissionState>(value.Data).Id) + "»",
                "rolegrant" => "مجوز " + perm(Parse<RolePermissionState>(value.Data).PermissionId) +
                    " برای نقش «" + role(Parse<RolePermissionState>(value.Data).RoleId) + "»",
                "membership" => "نقش «" + role(Parse<UserRoleState>(value.Data).RoleId) +
                    "» برای «" + user(Parse<UserRoleState>(value.Data).UserId) + "»",
                "override" => "مجوز اختصاصی " + perm(Parse<UserOverrideState>(value.Data).PermissionId) +
                    " برای «" + user(Parse<UserOverrideState>(value.Data).UserId) + "»",
                _ => value.Key
            };
        }
        static string describe(StateItem? item)
        {
            if (item is null) return "وجود ندارد";
            return item.Kind switch
            {
                "role" => Parse<RoleState>(item.Data).Name,
                "entity" => FormatEntity(Parse<EntityState>(item.Data)),
                "permission" => FormatPermission(Parse<PermissionState>(item.Data)),
                "rolegrant" or "membership" => "تخصیص داده شده",
                "override" => Parse<UserOverrideState>(item.Data).IsAllowed ?
                    "اجازه اختصاصی" : "محدودیت اختصاصی",
                _ => "ثبت‌شده"
            };
        }
        return differences.Select(d => new AccessHistoryChange(d.Key,
            (d.After ?? d.Before!).Kind, title(d), describe(d.Before), describe(d.After))).ToArray();
    }

    private static string FormatEntity(EntityState x) =>
        x.Title + " (" + x.Key + ") — " + (x.IsActive ? "فعال" : "غیرفعال") +
        (string.IsNullOrWhiteSpace(x.Description) ? "" : " — " + x.Description);
    private static string FormatPermission(PermissionState x) =>
        x.Title + " (" + x.Key + ") — " + (x.IsActive ? "فعال" : "غیرفعال") +
        (string.IsNullOrWhiteSpace(x.Description) ? "" : " — " + x.Description);

    public async Task<AccessHistoryDetail?> DetailAsync(Guid id, CancellationToken ct)
    {
        var entry = await db.AccessHistoryEntries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.AfterJson != null, ct);
        if (entry is null) return null;
        var actors = await ActorNamesAsync(new[] { entry.ActorId }, ct);
        return new AccessHistoryDetail(entry.Id, entry.Kind, entry.Title, entry.CreatedAtUtc,
            actors.GetValueOrDefault(entry.ActorId, "کاربر نامشخص"), entry.ChangeCount,
            await ChangesAsync(FromJson(entry.BeforeJson), FromJson(entry.AfterJson!), ct));
    }

    private static RestorePlan Plan(List<StateItem> current, AccessHistoryEntry entry, string mode)
    {
        if (mode == "point")
            return new RestorePlan(FromJson(entry.AfterJson!), 0);
        if (mode != "single") return new RestorePlan(current, 1);
        var before = FromJson(entry.BeforeJson);
        var after = FromJson(entry.AfterJson!);
        var map = current.ToDictionary(x => x.Key, StringComparer.Ordinal);
        var conflicts = 0;
        foreach (var diff in Delta(before, after))
        {
            var existing = map.GetValueOrDefault(diff.Key);
            if (existing?.Data == diff.Before?.Data) continue; // already reverted
            if (existing?.Data != diff.After?.Data) { conflicts++; continue; }
            if (diff.Before is null) map.Remove(diff.Key);
            else map[diff.Key] = diff.Before;
        }
        return new RestorePlan(map.Values.OrderBy(x => x.Key, StringComparer.Ordinal).ToList(), conflicts);
    }

    public async Task<AccessHistoryPreview?> PreviewAsync(Guid id, string mode, CancellationToken ct)
    {
        var entry = await db.AccessHistoryEntries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.AfterJson != null, ct);
        if (entry is null || mode is not ("single" or "point")) return null;
        var current = await CaptureAsync(ct);
        var plan = Plan(current, entry, mode);
        var warnings = new List<string>();
        if (plan.Conflicts != 0)
            warnings.Add("برخی تنظیمات پس از این عملیات دوباره تغییر کرده‌اند؛ بازگردانی تکی ممکن نیست.");
        if (mode == "point")
            warnings.Add("بازگردانی کامل ممکن است تغییرات جدیدتر نقش‌ها و کاربران را نیز برگرداند.");
        warnings.Add("برخی بخش‌های قدیمی CRM هنوز به نام نقش وابسته‌اند؛ پس از بازگردانی، دسترسی آن بخش‌ها را بررسی کنید.");
        return new AccessHistoryPreview(id, mode, Revision(current), plan.Conflicts,
            await ChangesAsync(current, plan.Target, ct), warnings);
    }
}
