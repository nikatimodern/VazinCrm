using CRM.Core.ApplicationServices.Carizos.Queries.GetTaskCards;
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.Domain.Carizos.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.EndPoint.FrontEnd.Security;
using CRM.Core.ApplicationServices.Carizos.Commands.UpdateTaskTime;
using CRM.Core.ApplicationServices.Carizos.Commands.AddSnooze;
using CRM.Core.ApplicationServices.Carizos.Commands.TaskToggle;
using CRM.Core.ApplicationServices.Carizos.Commands.DismissTask;
using CRM.Core.ApplicationServices.Carizos.Commands.ShowTask;
using CRM.Core.ApplicationServices.Carizos.Commands.UnCheckTask;
using CRM.Core.ApplicationServices.Carizos.Commands.FinishTask;
using CRM.Core.ApplicationServices.Carizos.Commands.DelTask;
using CRM.Core.ApplicationServices.Carizos.Commands.SaveTask;
using CRM.Core.ApplicationServices.Carizos.Queries.GetTask;
using CRM.Core.ApplicationServices.Carizos.Queries.GetTaskReminders;
using CRM.Core.ApplicationServices.Carizos.Queries.GetTaskList;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public class CarizoController(ICqrsDispatcher dispatcher, DocumentAccessPolicy access) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Carizo", ct)) return Forbid();
        return View(await dispatcher.QueryAsync(new GetWorkspacePageQuery("Carizo"), ct));
    }
    public async Task<IActionResult> GetRemind(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Carizo", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetTaskRemindersQuery(), ct));
    }
    [HttpGet]
    public async Task<IActionResult> GetAllTask(int type, int loc, int systematic, CancellationToken ct)
    {
        if (loc == 2 && !await DashboardAccess.CanAsync(access, User, "ViewTasks", ct)) return Forbid();
        if (!await access.HasViewAsync("Carizo", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetTaskListQuery(type, loc, systematic), ct));
    }
    [HttpGet]
    public async Task<IActionResult> GetTaskCards(Guid id, int systematic = 0, int loc = 1, CancellationToken ct = default)
    {
        if (loc == 2 && !await DashboardAccess.CanAsync(access, User, "ViewTasks", ct)) return Forbid();
        if (!await access.HasViewAsync("Carizo", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetTaskCardsQuery(id, systematic, loc), ct));
    }
    [HttpGet]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "View", id, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetTaskQuery(id), ct));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTask([FromBody] Carizo _model, CancellationToken ct)
    {
        if (_model == null) return BadRequest();
        if (!await access.HasViewAsync("Carizo", ct)) return Forbid();
        if (access.IsEnforced("Carizo") && _model.EntityId.HasValue)
        {
            if (_model.EntityType == "Deal" && !await access.CanDealAsync(_model.EntityId.Value, "View", ct)) return Forbid();
            if (_model.EntityType == "Report" && !await access.CanAsync("Report", "View", _model.EntityId.Value, ct)) return Forbid();
            if (_model.Id == Guid.Empty && _model.EntityType is not ("Deal" or "Report")) return BadRequest();
        }
        if (_model.Id == Guid.Empty ? !await access.HasAsync("Carizo", "Create", ct) : !await access.CanAsync("Carizo", "Edit", _model.Id, ct)) return Forbid();
        var result = await dispatcher.CommandAsync(new SaveTaskCommand(_model), ct);
        return result is int status ? StatusCode(status) : Ok(result);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskTime(Guid id, string _Time, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "Edit", id, ct)) return Forbid();
        return StatusCode(await dispatcher.CommandAsync(new UpdateTaskTimeCommand(id, _Time), ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSnooze(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "ChangeStatus", id, ct)) return Forbid();
        return StatusCode(await dispatcher.CommandAsync(new AddSnoozeCommand(id), ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaskToggle(Guid id, int btnC, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "ChangeStatus", id, ct)) return Forbid();
        return StatusCode(await dispatcher.CommandAsync(new TaskToggleCommand(id, btnC), ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissTask(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "ChangeStatus", id, ct)) return Forbid();
        return StatusCode(await dispatcher.CommandAsync(new DismissTaskCommand(id), ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ShowTask(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "ChangeStatus", id, ct)) return Forbid();
        return StatusCode(await dispatcher.CommandAsync(new ShowTaskCommand(id), ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnCheckTask(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "ChangeStatus", id, ct)) return Forbid();
        var result = await dispatcher.CommandAsync(new UnCheckTaskCommand(id), ct);
        return result is int status ? StatusCode(status) : Ok(result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FinishTask(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "ChangeStatus", id, ct)) return Forbid();
        var result = await dispatcher.CommandAsync(new FinishTaskCommand(id), ct);
        return result is int status ? StatusCode(status) : Ok(result);
    }

    [HttpDelete, ValidateAntiForgeryToken]
    public async Task<IActionResult> DelTask(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Carizo", "Delete", id, ct)) return Forbid();
        return StatusCode(await dispatcher.CommandAsync(new DelTaskCommand(id), ct));
    }
}
