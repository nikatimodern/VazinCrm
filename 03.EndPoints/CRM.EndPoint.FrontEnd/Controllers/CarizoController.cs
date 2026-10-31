using CRM.Core.ApplicationServices.Carizos.Queries.GetTaskCards;
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.Domain.Carizos.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
public class CarizoController(ICqrsDispatcher dispatcher) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await dispatcher.QueryAsync(new GetWorkspacePageQuery("Carizo"), ct));
    public async Task<IActionResult> GetRemind(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetTaskRemindersQuery(), ct));
    [HttpGet]
    public async Task<IActionResult> GetAllTask(int type, int loc, int systematic, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetTaskListQuery(type, loc, systematic), ct));
    [HttpGet]
    public async Task<IActionResult> GetTaskCards(Guid id, int systematic = 0, int loc = 1, CancellationToken ct = default) => Ok(await dispatcher.QueryAsync(new GetTaskCardsQuery(id, systematic, loc), ct));
    [HttpGet]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetTaskQuery(id), ct));
    [HttpPost]
    public async Task<IActionResult> AddTask([FromBody] Carizo _model, CancellationToken ct) => Ok(await dispatcher.CommandAsync(new SaveTaskCommand(_model), ct));
    public async Task<IActionResult> UpdateTaskTime(Guid id, string _Time, CancellationToken ct)
    {
        return StatusCode(await dispatcher.CommandAsync(new UpdateTaskTimeCommand(id, _Time), ct));
    }

    public async Task<IActionResult> AddSnooze(Guid id, CancellationToken ct)
    {
        return StatusCode(await dispatcher.CommandAsync(new AddSnoozeCommand(id), ct));
    }

    public async Task<IActionResult> TaskToggle(Guid id, int btnC, CancellationToken ct)
    {
        return StatusCode(await dispatcher.CommandAsync(new TaskToggleCommand(id, btnC), ct));
    }

    public async Task<IActionResult> DismissTask(Guid id, CancellationToken ct)
    {
        return StatusCode(await dispatcher.CommandAsync(new DismissTaskCommand(id), ct));
    }

    public async Task<IActionResult> ShowTask(Guid id, CancellationToken ct)
    {
        return StatusCode(await dispatcher.CommandAsync(new ShowTaskCommand(id), ct));
    }

    public async Task<IActionResult> UnCheckTask(Guid id, CancellationToken ct)
    {
        var result = await dispatcher.CommandAsync(new UnCheckTaskCommand(id), ct);
        return result is int status ? StatusCode(status) : Ok(result);
    }

    public async Task<IActionResult> FinishTask(Guid id, CancellationToken ct)
    {
        var result = await dispatcher.CommandAsync(new FinishTaskCommand(id), ct);
        return result is int status ? StatusCode(status) : Ok(result);
    }

    public async Task<IActionResult> DelTask(Guid id, CancellationToken ct)
    {
        return StatusCode(await dispatcher.CommandAsync(new DelTaskCommand(id), ct));
    }
}
