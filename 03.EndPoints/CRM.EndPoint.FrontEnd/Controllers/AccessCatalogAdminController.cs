#nullable enable
using CRM.Core.ApplicationServices.AccessCatalog.Services;
using CRM.Core.ApplicationServices.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccessCatalogAdminController(AccessCatalogAdministration administration) : Controller
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

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(65536)]
    public async Task<IActionResult> SaveEntity(Guid? id, [FromBody] AccessEntityDraft draft, CancellationToken ct) =>
        Result(await administration.SaveEntityAsync(id, draft, ct));

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(65536)]
    public async Task<IActionResult> SavePermission(Guid? id, [FromBody] AccessPermissionDraft draft, CancellationToken ct) =>
        Result(await administration.SavePermissionAsync(id, draft, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEntity(Guid id, CancellationToken ct) =>
        Result(await administration.DeleteEntityAsync(id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePermission(Guid id, CancellationToken ct) =>
        Result(await administration.DeletePermissionAsync(id, ct));

    private IActionResult Result(AccessCatalogResult result) =>
        StatusCode(result.Status, new { success = result.Status == 200, message = result.Message, id = result.Id });
}
