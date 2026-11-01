using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.ApplicationServices.Transfers.Queries.GetTransfersTable;
using CRM.Core.ApplicationServices.Transfers.Queries.GetTransfer;
using CRM.Core.ApplicationServices.Transfers.Commands.SaveTransfer;
using CRM.Core.ApplicationServices.Transfers.Commands.DeleteTransfer;
using CRM.Core.ApplicationServices.Transfers.Commands.ChangeTransferStatus;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Transfers.Enums;
using CRM.Core.Domain.Transfers.Repositories;
using CRM.EndPoint.FrontEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public sealed class TransferController(ICqrsDispatcher requests) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await requests.QueryAsync(new GetWorkspacePageQuery("Transfer"), ct));
    [HttpPost]
    public async Task<IActionResult> GetAllTransfersDt(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        var model = JsonConvert.DeserializeObject<FilterVModel>(form["model"].FirstOrDefault());
        var search = form["search[value]"].FirstOrDefault();
        if (string.IsNullOrEmpty(search))
            search = model?.serVal;
        var column = form["order[0][column]"].FirstOrDefault();
        var filter = new TransferTableRequest(form["draw"].FirstOrDefault(), Convert.ToInt32(form["start"].FirstOrDefault() ?? "0"), Convert.ToInt32(form["length"].FirstOrDefault() ?? "10"), search, 0, form[$"columns[{column}][data]"].FirstOrDefault(), form["order[0][dir]"] == "asc", model?._type);
        return Json(await requests.QueryAsync(new GetTransfersTableQuery(filter), ct));
    }

    [HttpPost]
    public async Task<IActionResult> AddTransfer([FromBody] Transfer model, CancellationToken ct)
    {
        if (model == null)
            return BadRequest();
        try
        {
            return StatusCode(await requests.CommandAsync(new SaveTransferCommand(model), ct));
        }
        catch (Exception)when (!ct.IsCancellationRequested)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetTransfer(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await requests.QueryAsync(new GetTransferQuery(id), ct);
            return result == null ? NotFound("رکوردی یافت نشد!") : Ok(result);
        }
        catch (Exception)when (!ct.IsCancellationRequested)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> DeleteTransfer(Guid id, CancellationToken ct)
    {
        try
        {
            var status = await requests.CommandAsync(new DeleteTransferCommand(id), ct);
            return status == 404 ? NotFound("رکوردی یافت نشد!") : StatusCode(status);
        }
        catch (Exception)when (!ct.IsCancellationRequested)
        {
            return BadRequest();
        }
    }

    [HttpPatch]
    public async Task<IActionResult> ChangeTransferStatus(Guid id, [FromBody] ChangeTransferStatusRequest request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest();
        var status = await requests.CommandAsync(new ChangeTransferStatusCommand(id, request.Status), ct);
        return status == 404 ? NotFound("نقل و انتقال مورد نظر یافت نشد.") : StatusCode(status);
    }
}

public sealed record ChangeTransferStatusRequest(TransferStatusEnum Status);
