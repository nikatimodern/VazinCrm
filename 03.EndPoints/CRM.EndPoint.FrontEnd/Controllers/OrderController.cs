using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Orders.Repositories;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Querying;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Order;
using CRM.EndPoint.FrontEnd.Models.Supply;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.EndPoint.FrontEnd.Security;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadsByPreFactor;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderPreFactorOptions;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderFactorOptions;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderItem;
using CRM.Core.ApplicationServices.Orders.Queries.GetMissingOrderItems;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderPrint;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadsTable;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderCustomerOptions;
using CRM.Core.ApplicationServices.Orders.Queries.GetVisibleOrderHeadIds;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderItemsTable;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadDetails;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderPreFactorHeads;
using CRM.Core.ApplicationServices.Orders.Queries.SelectOrderHeads;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderBoard;
using CRM.Core.ApplicationServices.Orders.Queries.GetDealOrderBoard;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderDocument;
using CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderHeadDescription;
using CRM.Core.ApplicationServices.Orders.Commands.CreateOrderItem;
using CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderItem;
using CRM.Core.ApplicationServices.Orders.Commands.DeleteOrderItem;
using CRM.Core.ApplicationServices.Orders.Commands.DeleteOrderHead;
using CRM.Core.ApplicationServices.Orders.Commands.ChangeOrderHeadStatus;
using CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderHead;
using CRM.Core.ApplicationServices.Orders.Commands.SaveOrderHeads;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public class OrderController(ICqrsDispatcher dispatcher, DocumentAccessPolicy access, CRMContextDb db) : Controller
{
    private Task<bool> HasOrder(string action, CancellationToken ct) => access.HasAnyAsync("OrderHead", action, ct);
    private Task<bool> CanOrder(string action, Guid id, CancellationToken ct) => access.CanAsync("OrderHead", action, id, ct);
    private async Task<bool> CanItem(string action, Guid id, CancellationToken ct)
    {
        if (!access.IsEnforced("OrderHead")) return true;
        var head = await db.Orders.AsNoTracking().Where(x => x.Id == id).Select(x => x.OrderHeadId).FirstOrDefaultAsync(ct);
        return head.HasValue && await CanOrder(action, head.Value, ct);
    }

    private async Task<bool> ValidateHead(OrderHead model, CancellationToken ct)
    {
        if (!access.IsEnforced("OrderHead")) return true;
        if (model == null) return false;
        if (model.Id == Guid.Empty)
        {
            if (!await access.HasAsync("OrderHead", "Create", ct)) return false;
            if (model.Prefactor.HasValue && !await access.CanAsync("PreFactor", "View", model.Prefactor.Value, ct)) return false;
            model.Status = CRM.Core.Domain.Orders.Enums.OrderHeadStatusEnum.ثبت_اولیه;
            model.IsDeleted = false;
        }
        else
        {
            if (!await CanOrder("Edit", model.Id, ct)) return false;
            var current = await db.OrderHeads.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.Id, ct);
            if (current == null) return false;
            model.UserCreatorID = current.UserCreatorID;
            model.UserCreator = current.UserCreator;
            model.UserTaminId = current.UserTaminId;
            model.UserTaminTxt = current.UserTaminTxt;
            model.Status = current.Status;
            model.IsDeleted = current.IsDeleted;
            model.Prefactor = current.Prefactor;
            model.PrefactorSer = current.PrefactorSer;
            model.Customer = current.Customer;
            model.CustomerCode = current.CustomerCode;
            model.CustomerTxt = current.CustomerTxt;
            model.DealId = current.DealId;
            model.DealSerialNumber = current.DealSerialNumber;
        }
        model.Deal = null;
        model.SupplyFactorOrderHeads = null;
        model.RequestAmountOrderHeads = null;
        model.CashOrderHeads = null;
        foreach (var item in model.Orders ?? new List<Order>())
        {
            if (item.Id != Guid.Empty && !await db.Orders.AsNoTracking().AnyAsync(x => x.Id == item.Id && x.OrderHeadId == model.Id, ct)) return false;
            if (item.PreFactorItemId.HasValue && !await db.PreFactorItems.AsNoTracking().AnyAsync(x => x.Id == item.PreFactorItemId && x.PrefactorId == model.Prefactor, ct)) return false;
            item.OrderHead = null;
            item.OrderHeadId = model.Id == Guid.Empty ? null : model.Id;
        }
        return true;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        return View(await dispatcher.QueryAsync(new GetWorkspacePageQuery("Order"), ct));
    }
    public async Task<IActionResult> Orders(Guid id, CancellationToken ct)
    {
        if (!await CanOrder("View", id, ct)) return Forbid();
        var page = await dispatcher.QueryAsync(new GetWorkspacePageQuery("Order/Orders"), ct);
        ViewBag.headOrId = id;
        return View(page);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderHead(Guid pfid, CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct) || !await access.CanAsync("PreFactor", "View", pfid, ct)) return Forbid();
        ViewBag.passType = 1;
        return PartialView(await dispatcher.QueryAsync(new GetOrderHeadsByPreFactorQuery(pfid), ct));
    }

    public async Task<IActionResult> GetPrefactorOp(Guid id, CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        return Ok((await dispatcher.QueryAsync(new GetOrderPreFactorOptionsQuery(id), ct)).Select(o => new { id = o.Id, text = o.Text }));
    }
    public async Task<IActionResult> GetFactorOp(Guid id, CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        return Ok((await dispatcher.QueryAsync(new GetOrderFactorOptionsQuery(id), ct)).Select(o => new { id = o.Id, text = o.Text }));
    }
    public async Task<IActionResult> GetCustomersOpId(CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        return Ok((await dispatcher.QueryAsync(new GetOrderCustomerOptionsQuery(), ct)).Select(o => new { id = o.Id, text = o.Text }));
    }
    [HttpPost]
    public async Task<IActionResult> UpHeadDescription(Guid Id, string Description, CancellationToken ct)
    {
        if (!await CanOrder("Edit", Id, ct)) return Forbid();
        return StatusCode(await dispatcher.CommandAsync(new UpdateOrderHeadDescriptionCommand(Id, Description), ct));
    }
    public async Task<IActionResult> PreviewPrint(Guid id, CancellationToken ct)
    {
        if (!await CanOrder("View", id, ct)) return Forbid();
        var data = await dispatcher.QueryAsync(new GetOrderPrintQuery(id), ct);
        if (data.OrderHead == null)
        {
            HttpContext.Items["ErrorMessage"] = "حواله تولیدی یافت نشد!";
            return NotFound();
        }

        if (data.PreFactor == null)
            return NotFound("پیش فاکتور حذف شده است");
        return View(new OrderPrintViewModel { Company = data.Company, OrderHead = data.OrderHead, OfficialType = data.PreFactor.OfficialType, SalesExpert = data.PreFactor.UserCreator, Logo = data.Company?.CompanyFiles?.FirstOrDefault(f => f.Filename == "logo")?.FileUrl });
    }

    public async Task<IActionResult> DownloadOrderPdf(Guid id, CancellationToken ct)
    {
        if (!await CanOrder("View", id, ct)) return Forbid();
        try
        {
            return File(await dispatcher.QueryAsync(new GetOrderDocumentQuery(id, false), ct), "application/pdf", "Order.pdf");
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> DownloadOrderImage(Guid id, CancellationToken ct)
    {
        if (!await CanOrder("View", id, ct)) return Forbid();
        try
        {
            return File(await dispatcher.QueryAsync(new GetOrderDocumentQuery(id, true), ct), "image/png", "Order.png");
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ChangeStatusHd(Guid id, int status, bool sendSms, CancellationToken ct)
    {
        if (!await CanOrder("ChangeStatus", id, ct)) return Forbid();
        try
        {
            return StatusCode(await dispatcher.CommandAsync(new ChangeOrderHeadStatusCommand(id, status, sendSms), ct));
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetAllHeadOrdersPartial([FromBody] FilterVModel model, [FromQuery] int fStatus = -1, CancellationToken ct = default)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        try
        {
            return Ok(await dispatcher.QueryAsync(new GetOrderBoardQuery(model.ToApplicationFilter(), fStatus), ct));
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetAllDealOrdersPartial(Guid dealId, CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetDealOrderBoardQuery(dealId), ct));
    }
    [HttpPost]
    public async Task<IActionResult> getAllOrders(Guid id, CancellationToken ct)
    {
        if (!await CanOrder("View", id, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetOrderItemsTableQuery(id, await ReadTableAsync(ct)), ct));
    }
    [HttpPost]
    public async Task<IActionResult> addOrder([FromBody] Order model, CancellationToken ct)
    {
        if (access.IsEnforced("OrderHead") && (model == null || model.Id != Guid.Empty || !model.OrderHeadId.HasValue || !await CanOrder("Edit", model.OrderHeadId.Value, ct))) return Forbid();
        return StatusCode(await dispatcher.CommandAsync(new CreateOrderItemCommand(model), ct));
    }
    [HttpGet]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        if (!await CanItem("View", id, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetOrderItemQuery(id), ct));
    }
    public async Task<IActionResult> CompareOrdersAndPfItems(Guid pfId, CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct) || !await access.CanAsync("PreFactor", "View", pfId, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetMissingOrderItemsQuery(pfId), ct));
    }
    public async Task<IActionResult> GetHeadOrder(Guid id, CancellationToken ct)
    {
        if (!await CanOrder("View", id, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetOrderHeadDetailsQuery(id), ct));
    }
    public async Task<IActionResult> GetHeadOrdersByPfId(Guid pfId, CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct) || !await access.CanAsync("PreFactor", "View", pfId, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetOrderPreFactorHeadsQuery(pfId), ct));
    }
    [Authorize, HttpGet]
    public async Task<IActionResult> GetUndeliveredOrderHeads(Guid pfId, CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct) || !await access.CanAsync("PreFactor", "View", pfId, ct)) return Forbid();
        ViewBag.passType = 2;
        return PartialView("GetOrderHead", await dispatcher.QueryAsync(new GetOrderHeadsByPreFactorQuery(pfId, true), ct));
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> GetAllOrderHeadsByCurrentTamin(CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetVisibleOrderHeadIdsQuery(), ct));
    }
    [Authorize, HttpPost]
    public async Task<IActionResult> GetAllOrderHeads([FromBody] GetAllBySelectedVModel model, CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        var result = await dispatcher.QueryAsync(new SelectOrderHeadsQuery(new HeadSelection(model.FilterIds, model.SelectedIds ?? [], model.Search), model.PassType), ct);
        ViewBag.MultiSelect = result.MultiSelect;
        ViewBag.PassType = result.PassType;
        return PartialView("GetOrderHead", result.Heads);
    }

    [HttpPost]
    public async Task<IActionResult> UpOrder(Order model, CancellationToken ct)
    {
        if (access.IsEnforced("OrderHead"))
        {
            if (model == null || !await CanItem("Edit", model.Id, ct)) return Forbid();
            var old = await db.Orders.AsNoTracking().SingleAsync(x => x.Id == model.Id, ct);
            model.OrderHeadId = old.OrderHeadId;
            model.OrderHead = null;
            model.PreFactorItemId = old.PreFactorItemId;
            model.Status = old.Status;
            model.StatusTxt = old.StatusTxt;
            model.Delivered = old.Delivered;
            model.DeliveryDate = old.DeliveryDate;
        }
        try
        {
            return StatusCode(await dispatcher.CommandAsync(new UpdateOrderItemCommand(model), ct));
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpHeadOrder([FromBody] OrderHead model, CancellationToken ct)
    {
        if (!await ValidateHead(model, ct)) return Forbid();
        try
        {
            return StatusCode(await dispatcher.CommandAsync(new UpdateOrderHeadCommand(model), ct));
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return NoContent();
        }
    }

    [HttpPost]
    public async Task<IActionResult> getAllHeadOrders(CancellationToken ct)
    {
        if (!await access.HasViewAsync("OrderHead", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetOrderHeadsTableQuery(await ReadTableAsync(ct)), ct));
    }
    [HttpGet]
    public async Task<IActionResult> DelOrder(Guid id, CancellationToken ct)
    {
        if (!await CanItem("Edit", id, ct)) return Forbid();
        try
        {
            return StatusCode(await dispatcher.CommandAsync(new DeleteOrderItemCommand(id), ct));
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Ok();
        }
    }

    [HttpGet]
    public async Task<IActionResult> DelOrderHead(Guid id, CancellationToken ct)
    {
        if (!await CanOrder("Delete", id, ct)) return Forbid();
        try
        {
            return StatusCode(await dispatcher.CommandAsync(new DeleteOrderHeadCommand(id), ct));
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddHeadOrder([FromBody] List<OrderHead> models, CancellationToken ct)
    {
        if (access.IsEnforced("OrderHead"))
        {
            if (models == null || models.Count == 0) return BadRequest();
            foreach (var head in models)
                if (!await ValidateHead(head, ct)) return Forbid();
        }
        try
        {
            return StatusCode(await dispatcher.CommandAsync(new SaveOrderHeadsCommand(models), ct));
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return BadRequest();
        }
    }

    private async Task<TableRequest> ReadTableAsync(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        int.TryParse(form["start"], out var start);
        int.TryParse(form["length"], out var length);
        var column = form["order[0][column]"].FirstOrDefault();
        return new(form["draw"].FirstOrDefault(), start, length, form[$"columns[{column}][name]"].FirstOrDefault(), form["order[0][dir]"].FirstOrDefault(), form["search[value]"].FirstOrDefault());
    }
}
