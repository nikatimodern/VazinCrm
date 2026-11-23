using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.ApplicationServices.RequestAmounts.Dtos;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Querying;
using CRM.EndPoint.FrontEnd.Models.Supply;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.EndPoint.FrontEnd.Security;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.AddRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.UpdateRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.ChangeDocTypeRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.SetAcceptedRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.ChangeStatusRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.AddPaymentByRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.DeletePayment;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.DeleteRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountDetails;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountList;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestsByPayment;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetSelectedRequests;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestPaymentsTable;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountsTable;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public class RequestAmountController(ICqrsDispatcher dispatcher, DocumentAccessPolicy access, CRMContextDb db) : Controller
{
    private Task<bool> Can(string key, Guid id, CancellationToken ct) => access.CanAsync("RequestAmount", key, id, ct);
    private async Task<bool> ValidateRequest(RequestAmount model, bool create, CancellationToken ct)
    {
        if (model == null) return false;
        if (create)
        {
            if (!await access.HasAsync("RequestAmount", "Create", ct)) return false;
            if (access.IsEnforced("RequestAmount"))
            {
                if (model.Id != Guid.Empty) return false;
                model.IsDeleted = false;
                model.RequestStatus = RequestStatusEnum.ثبت_اولیه;
                model.AcceptedAmount = 0;
                model.AcceptedAmountPercent = null;
                model.RequestAmountPayments = null;
                model.Accountancy = null;
            }
        }
        else
        {
            if (!await Can("Edit", model.Id, ct)) return false;
            if (access.IsEnforced("RequestAmount"))
            {
                var old = await db.RequestAmounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.Id, ct);
                if (old == null) return false;
                model.UserCreatorID = old.UserCreatorID;
                model.UserCreator = old.UserCreator;
                model.RequestStatus = old.RequestStatus;
                model.AcceptedAmount = old.AcceptedAmount;
                model.AcceptedAmountPercent = old.AcceptedAmountPercent;
                model.DocType = old.DocType;
                model.IsDeleted = old.IsDeleted;
                model.Accountancy = null;
                model.RequestAmountPayments = null;
            }
        }
        foreach (var link in model.RequestAmountOrderHeads ?? [])
        {
            if (!await access.CanAsync("OrderHead", "View", link.OrderHeadId, ct)) return false;
            link.OrderHead = null;
            link.RequestAmount = null;
            link.RequestAmountId = model.Id;
        }
        foreach (var link in model.RequestAmountSupplyFactor ?? [])
        {
            if (!await access.CanAsync("SupplyFactor", "View", link.SupplyFactorId, ct)) return false;
            link.SupplyFactor = null;
            link.RequestAmount = null;
            link.RequestAmountId = model.Id;
        }
        return true;
    }

    private async Task<bool> CanPayment(Payment model, CancellationToken ct)
    {
        if (model == null) return false;
        if (model.Id == Guid.Empty)
        {
            if (!await access.HasAsync("Payment", "Create", ct)) return false;
        }
        else if (!await access.CanAsync("Payment", "Edit", model.Id, ct)) return false;
        if (!await access.CanAsync("BankAccount", "View", model.FromAccountId, ct) ||
            !await access.CanAsync("BankAccount", "View", model.ToAccountId, ct)) return false;
        foreach (var link in model.RequestAmountPayments ?? [])
        {
            if (!await Can("Pay", link.RequestAmountId, ct)) return false;
            link.RequestAmount = null;
            link.Payment = null;
            link.PaymentId = model.Id;
        }
        if (model.Id != Guid.Empty)
        {
            var oldIds = await db.RequestAmountPayments.Where(x => x.PaymentId == model.Id).Select(x => x.RequestAmountId).ToListAsync(ct);
            foreach (var id in oldIds) if (!await Can("Pay", id, ct)) return false;
        }
        if (access.IsEnforced("Payment"))
        {
            model.PaymentSupplyFactors = null;
            model.IsDeleted = false;
            foreach (var attachment in model.Attachments ?? [])
                if (attachment.Id != Guid.Empty && !await db.Attachments.AsNoTracking().AnyAsync(x => x.Id == attachment.Id && x.EntityId == model.Id, ct)) return false;
        }
        return true;
    }

    [Authorize(Roles = "مدیر تامین ,کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی, تنخواه دار")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await access.HasViewAsync("RequestAmount", ct)) return Forbid();
        return View(await dispatcher.QueryAsync(new GetWorkspacePageQuery("RequestAmount"), ct));
    }
    [HttpPost]
    public async Task<IActionResult> AddRequestAmount([FromBody] RequestAmount model, CancellationToken ct)
    {
        if (!await ValidateRequest(model, true, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new AddRequestAmountCommand(model), ct));
    }
    [HttpPost]
    public async Task<IActionResult> UpdateRequestAmount([FromBody] RequestAmount model, CancellationToken ct)
    {
        if (!await ValidateRequest(model, false, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new UpdateRequestAmountCommand(model), ct));
    }
    [HttpPost]
    public async Task<IActionResult> ChangeDocTypeRequestAmount([FromBody] RequestAmount model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await Can("ChangeDocType", model.Id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new ChangeDocTypeRequestAmountCommand(model), ct));
    }
    [HttpPost]
    public async Task<IActionResult> SetAcceptedRequestAmount([FromBody] RequestAmount model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await Can("Approve", model.Id, ct)) return Forbid();
        if (access.IsEnforced("RequestAmount") && !await db.RequestAmounts.AnyAsync(x => x.Id == model.Id && x.RequestStatus == RequestStatusEnum.در_انتظار_تایید_مدیرعامل && model.AcceptedAmount > 0 && model.AcceptedAmount <= x.Amount, ct)) return BadRequest();
        return Map(await dispatcher.CommandAsync(new SetAcceptedRequestAmountCommand(model), ct));
    }
    public async Task<IActionResult> ChangeStatusRequestAmount(Guid id, RequestStatusEnum status, CancellationToken ct)
    {
        if (!await Can("ChangeStatus", id, ct)) return Forbid();
        if (!Enum.IsDefined(status)) return BadRequest();
        if (access.IsEnforced("RequestAmount"))
        {
            var row = await db.RequestAmounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (row == null) return NotFound();
            var from = row.RequestStatus;
            // A creator may submit a draft, but may not skip the approval/finance stages.
            if (from != RequestStatusEnum.ثبت_اولیه || status != RequestStatusEnum.در_انتظار_تایید_مدیرعامل)
            {
                if (from is RequestStatusEnum.در_انتظار_تایید_مدیرعامل or RequestStatusEnum.در_انتظار_ارسال_به_مالی)
                {
                    if (!await Can("Approve", id, ct)) return Forbid();
                }
                else if (!await Can("Pay", id, ct)) return Forbid();
                if (from == RequestStatusEnum.ثبت_اولیه) return Forbid();
            }
            if (status == RequestStatusEnum.پرداخت_شده &&
                (row.AcceptedAmount <= 0 || await db.RequestAmountPayments.Where(x => x.RequestAmountId == id).SumAsync(x => x.Amount, ct) < row.AcceptedAmount))
                return Conflict("مبلغ تایید شده هنوز کامل پرداخت نشده است.");
        }
        return Map(await dispatcher.CommandAsync(new ChangeStatusRequestAmountCommand(id, status), ct));
    }
    [HttpPost]
    [Authorize(Roles = "کارشناس مالی, مدیر مالی, مدیر ارشد")]
    public async Task<IActionResult> AddPaymentByRequestAmount([FromBody] Payment model, CancellationToken ct)
    {
        if (!await CanPayment(model, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new AddPaymentByRequestAmountCommand(model), ct));
    }
    public async Task<IActionResult> DeletePayment(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("Payment", "Delete", id, ct)) return Forbid();
        foreach (var requestId in await db.RequestAmountPayments.Where(x => x.PaymentId == id).Select(x => x.RequestAmountId).ToListAsync(ct))
            if (!await Can("Pay", requestId, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new DeletePaymentCommand(id), ct));
    }
    public async Task<IActionResult> DeleteRequestAmount(Guid id, CancellationToken ct)
    {
        if (!await Can("Delete", id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new DeleteRequestAmountCommand(id), ct));
    }
    [HttpPost]
    public async Task<IActionResult> GetAllPaymentsByRequestAmount([FromQuery] Guid id, CancellationToken ct)
    {
        if (!await Can("View", id, ct)) return Forbid();
        return Json(await dispatcher.QueryAsync(new GetRequestPaymentsTableQuery(id, await ReadTableAsync(ct)), ct));
    }
    public async Task<IActionResult> GetAllRequestAmountsPartial(int type = -1, string search = "", Guid? orderHeadId = null, CancellationToken ct = default)
    {
        if (!await access.HasViewAsync("RequestAmount", ct)) return Forbid();
        if (orderHeadId.HasValue && !await access.CanAsync("OrderHead", "View", orderHeadId.Value, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetRequestAmountListQuery(type, search, orderHeadId), ct));
    }
    public async Task<IActionResult> GetRequestAmount(Guid id, CancellationToken ct)
    {
        if (!await Can("View", id, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetRequestAmountDetailsQuery(id), ct));
    }
    [HttpPost]
    public async Task<IActionResult> GetRequestsAmountByFilteredIds([FromBody] GetAllBySelectedVModel model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await access.HasViewAsync("RequestAmount", ct)) return Forbid();
        foreach (var id in model.FilterIds ?? [])
            if (!await Can("View", id, ct)) return Forbid();
        var data = await dispatcher.QueryAsync(new GetSelectedRequestsQuery(model.FilterIds ?? []), ct);
        return data.Error == null ? Ok(data.Data) : BadRequest(data.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetRequestAmountsWithPaidAmountByPayment(Guid paymentId, CancellationToken ct)
    {
        if (!await access.CanAsync("Payment", "View", paymentId, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetRequestsByPaymentQuery(paymentId), ct));
    }
    public async Task<IActionResult> GetAllRequestAmountsDt(CancellationToken ct)
    {
        if (!await access.HasViewAsync("RequestAmount", ct)) return Forbid();
        return Json(await dispatcher.QueryAsync(new GetRequestAmountsTableQuery(await ReadTableAsync(ct)), ct));
    }
    private IActionResult Map(RequestAmountOutcome result) => result.Message == null ? StatusCode(result.Status) : StatusCode(result.Status, result.Message);
    private async Task<TableRequest> ReadTableAsync(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        int.TryParse(form["start"], out var start);
        var length = int.TryParse(form["length"], out var value) ? value : 10;
        var column = form["order[0][column]"].FirstOrDefault();
        return new(form["draw"].FirstOrDefault(), start, length, form[$"columns[{column}][name]"].FirstOrDefault(), form["order[0][dir]"].FirstOrDefault(), form["search[value]"].FirstOrDefault()?.ToLower().Trim());
    }
}
