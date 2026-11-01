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

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public sealed class CheckController(ICqrsDispatcher requests) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View((await requests.QueryAsync(new GetCheckbooksQuery(), ct)).Select(row => new CheckbookVModel { Checkbook = row.Checkbook, ChecksCount = row.ChecksCount }).ToList());
    public async Task<IActionResult> Received(CancellationToken ct) => View((await requests.QueryAsync(new GetCheckbooksQuery(), ct)).Select(row => new CheckbookVModel { Checkbook = row.Checkbook, ChecksCount = row.ChecksCount }).ToList());
    public async Task<IActionResult> GetAllCheckbooksPartial(CancellationToken ct) => PartialView((await requests.QueryAsync(new GetCheckbooksQuery(), ct)).Select(row => new CheckbookVModel { Checkbook = row.Checkbook, ChecksCount = row.ChecksCount }).ToList());
    [HttpGet]
    public async Task<IActionResult> GetCheckListByCheckbookId(Guid checkbookId, CancellationToken ct) => Ok(await requests.QueryAsync(new GetChecksByCheckbookQuery(checkbookId), ct));
    [HttpPost]
    public async Task<IActionResult> AddCheckbook([FromBody] Checkbook model, CancellationToken ct) => model == null ? BadRequest() : StatusCode(await requests.CommandAsync(new CreateCheckbookCommand(model), ct));
    [HttpPost]
    public async Task<IActionResult> AddCheck([FromBody] Check model, CancellationToken ct) => model == null ? BadRequest() : StatusCode(await requests.CommandAsync(new SaveCheckCommand(model), ct));
    public async Task<IActionResult> ChangeStatus(Guid id, CheckStatus status, CancellationToken ct) => StatusCode(await requests.CommandAsync(new ChangeCheckStatusCommand(id, status), ct));
    public async Task<IActionResult> GetCheck(Guid id, CancellationToken ct)
    {
        var item = await requests.QueryAsync(new GetCheckQuery(id), ct);
        return item == null ? NotFound("رکوردی یافت نشد!") : Ok(item);
    }

    public async Task<IActionResult> DeleteCheck(Guid id, CancellationToken ct)
    {
        var status = await requests.CommandAsync(new DeleteCheckCommand(id), ct);
        return status == 404 ? NotFound("رکوردی یافت نشد!") : StatusCode(status);
    }

    public async Task<IActionResult> DeleteCheckbook(Guid id, CancellationToken ct)
    {
        var status = await requests.CommandAsync(new DeleteCheckbookCommand(id), ct);
        return status == 404 ? NotFound("رکوردی یافت نشد!") : StatusCode(status);
    }
}
