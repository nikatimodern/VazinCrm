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
using CRM.EndPoint.FrontEnd.Security;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public sealed class TransferController(ICqrsDispatcher requests, DocumentAccessPolicy access, CRMContextDb db) : Controller
{
    private async Task<bool> CanUseAccounts(Transfer transfer, CancellationToken ct)
    {
        return (!transfer.FromAccountId.HasValue || await access.CanAsync("BankAccount", "View", transfer.FromAccountId.Value, ct))
            && (!transfer.ToAccountId.HasValue || await access.CanAsync("BankAccount", "View", transfer.ToAccountId.Value, ct));
    }

    private async Task<bool> CanUseExistingAccounts(Guid id, CancellationToken ct)
    {
        if (!access.IsEnforced("BankAccount")) return true;
        var row = await db.Transfers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return row != null && await CanUseAccounts(row, ct);
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Transfer", ct)) return Forbid();
        return View(await requests.QueryAsync(new GetWorkspacePageQuery("Transfer"), ct));
    }
    [HttpPost]
    public async Task<IActionResult> GetAllTransfersDt(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Transfer", ct)) return Forbid();
        var form = await Request.ReadFormAsync(ct);
        var model = JsonConvert.DeserializeObject<FilterVModel>(form["model"].FirstOrDefault());
        var search = form["search[value]"].FirstOrDefault();
        if (string.IsNullOrEmpty(search))
            search = model?.serVal;
        var column = form["order[0][column]"].FirstOrDefault();
        var filter = new TransferTableRequest(form["draw"].FirstOrDefault(), Convert.ToInt32(form["start"].FirstOrDefault() ?? "0"), Convert.ToInt32(form["length"].FirstOrDefault() ?? "10"), search, 0, form[$"columns[{column}][data]"].FirstOrDefault(), form["order[0][dir]"] == "asc", model?._type);
        var result = await requests.QueryAsync(new GetTransfersTableQuery(filter), ct);
        var permissions = new System.Collections.Generic.Dictionary<Guid, object>();
        foreach (var row in result.Data)
            permissions[row.Id] = new
            {
                canEdit = await access.AllowsRowAsync(row, "Edit", ct),
                canDelete = await access.AllowsRowAsync(row, "Delete", ct),
                canChangeStatus = await access.AllowsRowAsync(row, "ChangeStatus", ct),
                canApprove = await access.AllowsRowAsync(row, "Approve", ct)
            };
        return Json(new { result.Draw, result.RecordsTotal, result.RecordsFiltered, result.Data, result.FilterCounts, permissions });
    }

    [HttpPost]
    public async Task<IActionResult> AddTransfer([FromBody] Transfer model, CancellationToken ct)
    {
        if (model == null)
            return BadRequest();
        if (model.Id == Guid.Empty ? !await access.HasAsync("Transfer", "Create", ct) : !await access.CanAsync("Transfer", "Edit", model.Id, ct)) return Forbid();
        if (access.IsEnforced("Transfer"))
        {
            if (model.Id == Guid.Empty)
            {
                model.Status = TransferStatusEnum.ثبت_اولیه;
                model.IsDeleted = false;
            }
            else
            {
                var old = await db.Transfers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id, ct);
                if (old == null) return NotFound();
                model.Status = old.Status;
                model.UserCreatorID = old.UserCreatorID;
                model.UserCreator = old.UserCreator;
                model.CreateDate = old.CreateDate;
                model.CreateDateTxt = old.CreateDateTxt;
                model.IsDeleted = old.IsDeleted;
            }
            foreach (var attachment in model.Attachments ?? [])
                if (attachment.Id != Guid.Empty && !await db.Attachments.AsNoTracking().AnyAsync(x => x.Id == attachment.Id && x.EntityId == model.Id && x._AttachType == CRM.Core.Domain.Attachments.Enum._AttachType.transfer, ct)) return Forbid();
        }
        if (!await CanUseAccounts(model, ct) || (model.Id != Guid.Empty && !await CanUseExistingAccounts(model.Id, ct))) return Forbid();
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
        if (!await access.CanAsync("Transfer", "View", id, ct)) return Forbid();
        if (!await CanUseExistingAccounts(id, ct)) return Forbid();
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
        if (!await access.CanAsync("Transfer", "Delete", id, ct)) return Forbid();
        if (!await CanUseExistingAccounts(id, ct)) return Forbid();
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
        if (!await access.CanAsync("Transfer", "ChangeStatus", id, ct) || !await CanUseExistingAccounts(id, ct)) return Forbid();
        if (!Enum.IsDefined(request.Status)) return BadRequest();
        if (access.IsEnforced("Transfer"))
        {
            var row = await db.Transfers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row == null) return NotFound();
            if ((row.Status != TransferStatusEnum.ثبت_اولیه || request.Status != TransferStatusEnum.در_دست_تایید)
                && !await access.CanAsync("Transfer", "Approve", id, ct)) return Forbid();
        }
        var status = await requests.CommandAsync(new ChangeTransferStatusCommand(id, request.Status), ct);
        return status == 404 ? NotFound("نقل و انتقال مورد نظر یافت نشد.") : StatusCode(status);
    }
}

public sealed record ChangeTransferStatusRequest(TransferStatusEnum Status);
