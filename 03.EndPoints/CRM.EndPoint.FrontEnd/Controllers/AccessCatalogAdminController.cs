#nullable enable
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.AccessCatalog.Services;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

public sealed record AccessHistoryManualDraft(string? Title);
public sealed record AccessHistoryRestoreDraft(Guid Id, string? Mode, string? Revision);

[Authorize(Roles = AppRoles.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccessCatalogAdminController(
    AccessCatalogAdministration administration, IAccessHistoryService history) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Models() => Json(administration.Candidates());

    [HttpGet]
    public async Task<IActionResult> Entities(CancellationToken ct) =>
        Json(await administration.EntitiesAsync(ct));

    [HttpGet]
    public async Task<IActionResult> Permissions(Guid entityId, CancellationToken ct)
    {
        var result = await administration.PermissionsAsync(entityId, ct);
        return result is null ? NotFound(new { message = "انتیتی پیدا نشد." }) : Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> Roles(CancellationToken ct) =>
        Json(await administration.RolesAsync(ct));

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(8192)]
    public async Task<IActionResult> SaveRole([FromBody] AccessRoleDraft draft, CancellationToken ct) =>
        await Recorded("SaveRole", () => administration.SaveRoleAsync(draft, ct), ct);

    [HttpGet]
    public async Task<IActionResult> RoleRemovalPreview(string roleId, CancellationToken ct)
    {
        var preview = await administration.RoleRemovalPreviewAsync(roleId, ct);
        return preview is null ? NotFound(new { message = "نقش پیدا نشد." }) : Json(preview);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(1048576)]
    public async Task<IActionResult> DeleteRole([FromBody] AccessRoleDeleteDraft draft, CancellationToken ct) =>
        await Recorded("DeleteRole", () => administration.DeleteRoleAsync(draft, ct), ct);

    [HttpGet]
    public async Task<IActionResult> RoleCatalog(CancellationToken ct) =>
        Json(await administration.RoleCatalogAsync(ct));

    [HttpGet]
    public async Task<IActionResult> RolePermissions(string roleId, CancellationToken ct)
    {
        var ids = await administration.RolePermissionsAsync(roleId, ct);
        return ids is null ? NotFound(new { message = "نقش موردنظر پیدا نشد." }) : Json(ids);
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? search, string? type = "employee", int page = 1, CancellationToken ct = default) =>
        Json(await administration.UsersAsync(search, page, type, ct));

    [HttpGet]
    public async Task<IActionResult> Positions(CancellationToken ct) =>
        Json(await administration.PositionsAsync(ct));

    [HttpGet]
    public async Task<IActionResult> UserProfile(string userId, CancellationToken ct)
    {
        var profile = await administration.UserProfileAsync(userId, ct);
        return profile is null ? NotFound(new { message = "کاربر پیدا نشد." }) : Json(profile);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(16384)]
    public async Task<IActionResult> SaveUserProfile([FromBody] AccessUserProfileDraft draft, CancellationToken ct) =>
        await Recorded("SaveUserProfile", () => administration.SaveUserProfileAsync(draft, User.FindFirstValue(ClaimTypes.NameIdentifier), ct), ct);

    [HttpGet]
    public async Task<IActionResult> UserAccess(string userId, CancellationToken ct)
    {
        var data = await administration.UserAccessAsync(userId, ct);
        return data is null ? NotFound(new { message = "کاربر پیدا نشد." }) : Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> RoleGrants(CancellationToken ct) =>
        Json(await administration.RoleGrantsAsync(ct));

    [HttpGet]
    public async Task<IActionResult> ScopeUsers(string? search, CancellationToken ct) =>
        Json(string.IsNullOrWhiteSpace(search) ? Array.Empty<AccessScopeUser>() :
             await administration.ScopeUsersAsync(search, ct));

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(65536)]
    public async Task<IActionResult> SaveUserAccess([FromBody] AccessUserSaveDraft draft, CancellationToken ct) =>
        await Recorded("SaveUserAccess", () => administration.SaveUserAccessAsync(draft, User.FindFirstValue(ClaimTypes.NameIdentifier), ct), ct);

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(65536)]
    public async Task<IActionResult> SaveRolePermissions([FromBody] RolePermissionsSaveDraft draft, CancellationToken ct) =>
        await Recorded("SaveRolePermissions", () => administration.SaveRolePermissionsAsync(draft, ct), ct);

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(65536)]
    public async Task<IActionResult> SaveEntity(Guid? id, [FromBody] AccessEntityDraft draft, CancellationToken ct) =>
        await Recorded("SaveEntity", () => administration.SaveEntityAsync(id, draft, ct), ct);

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(65536)]
    public async Task<IActionResult> SavePermission(Guid? id, [FromBody] AccessPermissionDraft draft, CancellationToken ct) =>
        await Recorded("SavePermission", () => administration.SavePermissionAsync(id, draft, ct), ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEntity(Guid id, CancellationToken ct) =>
        await Recorded("DeleteEntity", () => administration.DeleteEntityAsync(id, ct), ct);

    [HttpGet]
    public async Task<IActionResult> PermissionRemovalPreview(Guid id, CancellationToken ct)
    {
        var preview = await administration.PermissionRemovalPreviewAsync(id, ct);
        return preview is null ? NotFound(new { message = "دسترسی پیدا نشد." }) : Json(preview);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(8192)]
    public async Task<IActionResult> DeletePermission(Guid id,
        [FromBody] PermissionRemovalDraft? confirmation, CancellationToken ct)
    {
        if (confirmation is null || !confirmation.ConfirmRemoval)
            return BadRequest(new { success = false, message = "تأیید حذف ضروری است." });
        return await Recorded("DeletePermission",
            () => administration.DeletePermissionAsync(id, confirmation, ct), ct);
    }

    [HttpGet]
    public async Task<IActionResult> History(int page = 1, string? kind = null,
        string? search = null, CancellationToken ct = default) =>
        Json(await history.ListAsync(page, kind, search, ct));

    [HttpGet]
    public async Task<IActionResult> HistoryDetail(Guid id, CancellationToken ct)
    {
        var detail = await history.DetailAsync(id, ct);
        return detail is null ? NotFound(new { message = "نسخه موردنظر پیدا نشد." }) : Json(detail);
    }

    [HttpGet]
    public async Task<IActionResult> HistoryPreview(Guid id, string mode, CancellationToken ct)
    {
        var preview = await history.PreviewAsync(id, mode, ct);
        return preview is null ? NotFound(new { message = "پیش‌نمایش پیدا نشد." }) : Json(preview);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(8192)]
    public async Task<IActionResult> CreateHistoryPoint([FromBody] AccessHistoryManualDraft draft, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(draft?.Title))
            return BadRequest(new { success = false, message = "عنوان نقطه بازگردانی را وارد کنید." });
        var result = await history.CreateManualAsync(draft.Title!, User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        return StatusCode(result.Status, new { success = result.Status == 200, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(8192)]
    public async Task<IActionResult> RestoreHistory([FromBody] AccessHistoryRestoreDraft draft, CancellationToken ct)
    {
        if (draft is null || draft.Id == Guid.Empty || string.IsNullOrWhiteSpace(draft.Mode) ||
            string.IsNullOrWhiteSpace(draft.Revision))
            return BadRequest(new { success = false, message = "درخواست بازگردانی معتبر نیست." });
        var result = await history.RestoreAsync(draft.Id, draft.Mode!, draft.Revision!,
            User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        return StatusCode(result.Status, new { success = result.Status == 200, message = result.Message });
    }

    private async Task<IActionResult> Recorded(string action, Func<Task<AccessCatalogResult>> operation,
        CancellationToken ct)
    {
        // A before-image is persisted before any write, and completed only on success.
        // Incomplete events are never offered as restore points.
        var eventId = await history.BeginAsync(action, User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        try
        {
            var result = await operation();
            await history.FinishAsync(eventId, result.Status == 200, ct);
            return Result(result);
        }
        catch
        {
            try { await history.FinishAsync(eventId, false, CancellationToken.None); }
            catch { /* Preserve the original failure; unfinished entries are hidden. */ }
            throw;
        }
    }

    private IActionResult Result(AccessCatalogResult result) =>
        StatusCode(result.Status, new { success = result.Status == 200, message = result.Message, id = result.Id, roleId = result.RoleId, userId = result.UserId });
}
