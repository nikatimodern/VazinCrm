using System.Text.RegularExpressions;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;

namespace CRM.Core.ApplicationServices.AccessCatalog.Services;

public sealed record AccessEntityDraft(string Key, string Title, string? Description, string DbContextName, string ModelFullName, bool IsActive);
public sealed record AccessPermissionDraft(Guid AccessEntityId, string Key, string Title, string? Description, bool IsActive);
public sealed record AccessEntityInfo(Guid Id, string Key, string Title, string? Description, string DbContextName, string ModelFullName, bool IsActive, int PermissionCount);
public sealed record AccessPermissionInfo(Guid Id, Guid AccessEntityId, string Key, string FullKey, string Title, string? Description, bool IsActive);
public sealed record AccessCatalogResult(int Status, string? Message = null, Guid? Id = null);

public sealed class AccessCatalogAdministration(IAccessCatalogRepository repository, IAccessModelCatalog models)
{
    private static readonly Regex KeyPattern = new("^[A-Za-z][A-Za-z0-9]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public IReadOnlyList<AccessModelOption> Candidates() => models.GetCandidates();

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
            entity = new AccessEntity { Key = draft.Key.Trim(), DbContextName = draft.DbContextName, ModelFullName = draft.ModelFullName };
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
        repository.Remove(permission);
        await repository.SaveAsync(ct);
        return new(200);
    }

    private static bool Valid(string? key, string? title, string? description) =>
        !string.IsNullOrWhiteSpace(key) && KeyPattern.IsMatch(key) &&
        !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= 150 &&
        (description?.Length ?? 0) <= 1000;
}
