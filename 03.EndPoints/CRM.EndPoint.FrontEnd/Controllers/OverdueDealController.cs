using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.ApplicationServices.Security;
using Microsoft.Extensions.Configuration;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.ApplicationServices.OverdueDeals.Queries.GetOverdueDeals;
using CRM.Core.ApplicationServices.OverdueDeals.Queries.GetOverdueDealDetails;
using CRM.Core.ApplicationServices.OverdueDeals.Commands.UpdateOverdueFollowUp;
using CRM.Core.Domain.Deals.Enums;
using CRM.EndPoint.FrontEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public sealed class OverdueDealController(ICqrsDispatcher requests, IAccessScopeService accessScopes,
    IConfiguration configuration) : Controller
{

    private bool Enforce => configuration.GetValue<bool>("AccessCatalog:EnforceDealPermissions");
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private bool LegacyCanView => new[] { AppRoles.SalesAdmin, AppRoles.Admin,
        AppRoles.ManagingDirector, AppRoles.SalesExpert }.Any(User.IsInRole);
    private bool LegacyCanFollowUp => User.IsInRole(AppRoles.SalesAdmin) || User.IsInRole(AppRoles.Admin);

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (Enforce)
        {
            if (await accessScopes.GetEffectiveScopeAsync(UserId, "Deal.View", ct) == AccessScopes.None)
                return Forbid();
            ViewBag.CanManageFollowUp = await accessScopes.GetEffectiveScopeAsync(
                UserId, "Deal.ManageFollowUp", ct) != AccessScopes.None;
        }
        else
        {
            if (!LegacyCanView) return Forbid();
            ViewBag.CanManageFollowUp = LegacyCanFollowUp;
        }
        return View(await requests.QueryAsync(new GetWorkspacePageQuery("OverdueDeal"), ct));
    }

    [HttpPost]
    public async Task<IActionResult> GetOverdueDeals([FromBody] FilterVModel model,
        DateTime? startDate, DateTime? endDate, string sortBy = "createDate",
        string sortDirection = "desc", bool withFollowUp = false, CancellationToken ct = default)
    {
        if (!Enforce && !LegacyCanView) return Forbid();
        DealOwnerAccess? access = null;
        if (Enforce)
        {
            access = await accessScopes.GetDealOwnerAccessAsync(UserId, "Deal.View", ct);
            if (access.AllowedOwnerIds is { Length: 0 }) return Forbid();
        }
        var result = await requests.QueryAsync(new GetOverdueDealsQuery(model.ToApplicationFilter(),
            startDate, endDate, sortBy, sortDirection, withFollowUp,
            access?.AllowedOwnerIds, access?.ExcludedOwnerIds, Enforce), ct);
        var followUpAccess = Enforce ? await accessScopes.GetDealOwnerAccessAsync(
            UserId, "Deal.ManageFollowUp", ct) : null;
        var statusAccess = Enforce ? await accessScopes.GetDealOwnerAccessAsync(
            UserId, "Deal.ChangeStatus", ct) : null;
        foreach (var row in result.Data)
        {
            row.CanManageFollowUp = Enforce
                ? followUpAccess!.Allows(row.UserCreatorID) : LegacyCanFollowUp;
            row.CanChangeStatus = Enforce
                ? statusAccess!.Allows(row.UserCreatorID) : LegacyCanFollowUp;
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOverdueDealDetails(Guid id, CancellationToken ct)
    {
        if (!Enforce && !LegacyCanView) return Forbid();
        DealOwnerAccess? access = null;
        if (Enforce)
        {
            access = await accessScopes.GetDealOwnerAccessAsync(UserId, "Deal.View", ct);
            if (access.AllowedOwnerIds is { Length: 0 }) return Forbid();
            if (!await accessScopes.CanAccessDealAsync(UserId, "Deal.View", id, ct))
                return Forbid();
        }
        var result = await requests.QueryAsync(new GetOverdueDealDetailsQuery(id,
            access?.AllowedOwnerIds, access?.ExcludedOwnerIds, Enforce), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateFollowUp([FromBody] OverdueFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "توضیحات پیگیری نباید بیشتر از ۴۰۰۰ نویسه باشد." });
        if (!Enforce && !LegacyCanFollowUp) return Forbid();
        if (Enforce &&
            (!await accessScopes.CanAccessDealAsync(UserId, "Deal.View", request.Id, cancellationToken) ||
             !await accessScopes.CanAccessDealAsync(UserId, "Deal.ManageFollowUp", request.Id, cancellationToken) ||
             (request.Status != DealStatusEnum.باز &&
              !await accessScopes.CanAccessDealAsync(UserId, "Deal.ChangeStatus", request.Id, cancellationToken))))
            return Forbid();
        var result = await requests.CommandAsync(new UpdateOverdueFollowUpCommand(
            request.Id, request.FollowUpDate, request.Status, request.Note), cancellationToken);
        return result.Message == null ? StatusCode(result.Status) :
            StatusCode(result.Status, new { message = result.Message });
    }

}

public sealed class OverdueFollowUpRequest
{
    public Guid Id { get; set; }
    public string FollowUpDate { get; set; }
    public DealStatusEnum Status { get; set; }
    public string Note { get; set; }
}
