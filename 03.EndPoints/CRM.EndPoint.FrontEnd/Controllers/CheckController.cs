using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Checks.Queries.GetCheckbooks;
using CRM.Core.ApplicationServices.Checks.Queries.GetChecksByCheckbook;
using CRM.Core.ApplicationServices.Checks.Queries.GetCheck;
using CRM.Core.ApplicationServices.Checks.Commands.CreateCheckbook;
using CRM.Core.ApplicationServices.Checks.Commands.SaveCheck;
using CRM.Core.ApplicationServices.Checks.Commands.ChangeCheckStatus;
using CRM.Core.ApplicationServices.Checks.Commands.DeleteCheck;
using CRM.Core.ApplicationServices.Checks.Commands.DeleteCheckbook;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.EndPoint.FrontEnd.Models.Check;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.EndPoint.FrontEnd.Security;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public sealed class CheckController(ICqrsDispatcher requests, DocumentAccessPolicy access, CRMContextDb db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Checkbook", ct)) return Forbid();
        return View((await requests.QueryAsync(new GetCheckbooksQuery(), ct)).Select(row => new CheckbookVModel { Checkbook = row.Checkbook, ChecksCount = row.ChecksCount }).ToList());
    }
    public async Task<IActionResult> Received(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Check", ct) || !await access.HasViewAsync("Accountancy", ct)) return Forbid();
        return View((await requests.QueryAsync(new GetCheckbooksQuery(), ct)).Select(row => new CheckbookVModel { Checkbook = row.Checkbook, ChecksCount = row.ChecksCount }).ToList());
    }
    public async Task<IActionResult> GetAllCheckbooksPartial(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Checkbook", ct)) return Forbid();
        return PartialView((await requests.QueryAsync(new GetCheckbooksQuery(), ct)).Select(row => new CheckbookVModel { Checkbook = row.Checkbook, ChecksCount = row.ChecksCount }).ToList());
    }
    [HttpGet]
    public async Task<IActionResult> GetCheckListByCheckbookId(Guid checkbookId, CancellationToken ct)
    {
        if (!await access.HasViewAsync("Check", ct) || !await access.CanAsync("Checkbook", "View", checkbookId, ct)) return Forbid();
        return Ok(await requests.QueryAsync(new GetChecksByCheckbookQuery(checkbookId), ct));
    }
    [HttpPost]
    public async Task<IActionResult> AddCheckbook([FromBody] Checkbook model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await access.HasAsync("Checkbook", "Create", ct) || !await access.CanAsync("BankAccount", "View", model.BankAccountId, ct)) return Forbid();
        if (access.IsEnforced("Checkbook"))
        {
            if (model.Id != Guid.Empty) return BadRequest();
            model.Checks = null;
            model.BankAccount = null;
            model.IsDeleted = false;
        }
        return model == null ? BadRequest() : StatusCode(await requests.CommandAsync(new CreateCheckbookCommand(model), ct));
    }
    [HttpPost]
    public async Task<IActionResult> AddCheck([FromBody] Check model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (model.Id == Guid.Empty)
        {
            if (!await access.HasAsync("Check", "Create", ct)) return Forbid();
            if (access.IsEnforced("Check"))
            {
                model.Status = CheckStatus.نزد_ما;
                model.IsDeleted = false;
            }
        }
        else
        {
            if (!await access.CanAsync("Check", "Edit", model.Id, ct)) return Forbid();
            if (access.IsEnforced("Check"))
            {
                var old = await db.Checks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.Id, ct);
                if (old == null) return NotFound();
                model.CheckbookId = old.CheckbookId;
                model.Status = old.Status;
                model.UserCreatorID = old.UserCreatorID;
                model.UserCreator = old.UserCreator;
                model.IsDeleted = old.IsDeleted;
            }
        }
        if (!await access.CanAsync("Checkbook", "View", model.CheckbookId, ct)) return Forbid();
        if (access.IsEnforced("Check")) model.Checkbook = null;
        return model == null ? BadRequest() : StatusCode(await requests.CommandAsync(new SaveCheckCommand(model), ct));
    }
    public async Task<IActionResult> ChangeStatus(Guid id, CheckStatus status, CancellationToken ct)
    {
        if (!await access.CanAsync("Check", "ChangeStatus", id, ct)) return Forbid();
        if (!Enum.IsDefined(status)) return BadRequest();
        return StatusCode(await requests.CommandAsync(new ChangeCheckStatusCommand(id, status), ct));
    }
    public async Task<IActionResult> GetCheck(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Check", "View", id, ct)) return Forbid();
        var item = await requests.QueryAsync(new GetCheckQuery(id), ct);
        return item == null ? NotFound("رکوردی یافت نشد!") : Ok(item);
    }

    public async Task<IActionResult> DeleteCheck(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Check", "Delete", id, ct)) return Forbid();
        var status = await requests.CommandAsync(new DeleteCheckCommand(id), ct);
        return status == 404 ? NotFound("رکوردی یافت نشد!") : StatusCode(status);
    }

    public async Task<IActionResult> DeleteCheckbook(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Checkbook", "Delete", id, ct)) return Forbid();
        if (access.IsEnforced("Check"))
        {
            var rows = db.Checks.Where(x => x.CheckbookId == id);
            var allowed = await access.ApplyAsync(await access.ApplyAsync(rows, "View", ct), "Delete", ct);
            if (await rows.AnyAsync(x => !allowed.Any(a => a.Id == x.Id), ct)) return Forbid();
        }
        var status = await requests.CommandAsync(new DeleteCheckbookCommand(id), ct);
        return status == 404 ? NotFound("رکوردی یافت نشد!") : StatusCode(status);
    }
}
