#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Targets.Commands.SaveTarget;
using CRM.Core.ApplicationServices.Targets.Queries.GetTarget;
using CRM.Core.ApplicationServices.Targets.Queries.GetTargetSetup;
using CRM.Core.ApplicationServices.Targets.Queries.GetTargets;
using CRM.Core.ApplicationServices.Targets.Services;
using CRM.Core.Domain.Targets.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = TargetAccessPolicy.Roles)]
public sealed class TargetController(ICqrsDispatcher requests) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await requests.QueryAsync(new GetTargetSetupQuery(), ct));

    [HttpGet]
    public async Task<IActionResult> GetSetup(CancellationToken ct) =>
        Ok(await requests.QueryAsync(new GetTargetSetupQuery(), ct));

    [HttpGet]
    public async Task<IActionResult> GetPlans(CancellationToken ct) =>
        Ok(await requests.QueryAsync(new GetTargetsQuery(), ct));

    [HttpGet]
    public async Task<IActionResult> GetPlan(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return BadRequest(new { message = "شناسه معتبر نیست." });
        var data = await requests.QueryAsync(new GetTargetQuery(id), ct);
        return data == null ? NotFound(new { message = "تارگت یافت نشد." }) : Ok(data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft([FromBody] TargetSaveRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null) return BadRequest(new { message = "اطلاعات فرم معتبر نیست." });
        var result = await requests.CommandAsync(new SaveTargetCommand(request, false), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize([FromBody] TargetSaveRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null) return BadRequest(new { message = "اطلاعات فرم معتبر نیست." });
        var result = await requests.CommandAsync(new SaveTargetCommand(request, true), ct);
        return StatusCode(result.StatusCode, result);
    }
}
