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
public class OrderController(ICqrsDispatcher dispatcher) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) => View(await dispatcher.QueryAsync(new GetWorkspacePageQuery("Order"), ct));
    public async Task<IActionResult> Orders(Guid id, CancellationToken ct)
    {
        var page = await dispatcher.QueryAsync(new GetWorkspacePageQuery("Order/Orders"), ct);
        ViewBag.headOrId = id;
        return View(page);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderHead(Guid pfid, CancellationToken ct)
    {
        ViewBag.passType = 1;
        return PartialView(await dispatcher.QueryAsync(new GetOrderHeadsByPreFactorQuery(pfid), ct));
    }

    public async Task<IActionResult> GetPrefactorOp(Guid id, CancellationToken ct) => Ok((await dispatcher.QueryAsync(new GetOrderPreFactorOptionsQuery(id), ct)).Select(o => new { id = o.Id, text = o.Text }));
    public async Task<IActionResult> GetFactorOp(Guid id, CancellationToken ct) => Ok((await dispatcher.QueryAsync(new GetOrderFactorOptionsQuery(id), ct)).Select(o => new { id = o.Id, text = o.Text }));
    public async Task<IActionResult> GetCustomersOpId(CancellationToken ct) => Ok((await dispatcher.QueryAsync(new GetOrderCustomerOptionsQuery(), ct)).Select(o => new { id = o.Id, text = o.Text }));
    [HttpPost]
    public async Task<IActionResult> UpHeadDescription(Guid Id, string Description, CancellationToken ct) => StatusCode(await dispatcher.CommandAsync(new UpdateOrderHeadDescriptionCommand(Id, Description), ct));
    public async Task<IActionResult> PreviewPrint(Guid id, CancellationToken ct)
    {
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

    public async Task<IActionResult> GetAllDealOrdersPartial(Guid dealId, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetDealOrderBoardQuery(dealId), ct));
    [HttpPost]
    public async Task<IActionResult> getAllOrders(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetOrderItemsTableQuery(id, await ReadTableAsync(ct)), ct));
    [HttpPost]
    public async Task<IActionResult> addOrder([FromBody] Order model, CancellationToken ct) => StatusCode(await dispatcher.CommandAsync(new CreateOrderItemCommand(model), ct));
    [HttpGet]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetOrderItemQuery(id), ct));
    public Task<List<PreFactorItem>> CompareOrdersAndPfItems(Guid pfId, CancellationToken ct) => dispatcher.QueryAsync(new GetMissingOrderItemsQuery(pfId), ct);
    public async Task<IActionResult> GetHeadOrder(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetOrderHeadDetailsQuery(id), ct));
    public async Task<IActionResult> GetHeadOrdersByPfId(Guid pfId, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetOrderPreFactorHeadsQuery(pfId), ct));
    [Authorize, HttpGet]
    public async Task<IActionResult> GetUndeliveredOrderHeads(Guid pfId, CancellationToken ct)
    {
        ViewBag.passType = 2;
        return PartialView("GetOrderHead", await dispatcher.QueryAsync(new GetOrderHeadsByPreFactorQuery(pfId, true), ct));
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> GetAllOrderHeadsByCurrentTamin(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetVisibleOrderHeadIdsQuery(), ct));
    [Authorize, HttpPost]
    public async Task<IActionResult> GetAllOrderHeads([FromBody] GetAllBySelectedVModel model, CancellationToken ct)
    {
        var result = await dispatcher.QueryAsync(new SelectOrderHeadsQuery(new HeadSelection(model.FilterIds, model.SelectedIds ?? [], model.Search), model.PassType), ct);
        ViewBag.MultiSelect = result.MultiSelect;
        ViewBag.PassType = result.PassType;
        return PartialView("GetOrderHead", result.Heads);
    }

    [HttpPost]
    public async Task<IActionResult> UpOrder(Order model, CancellationToken ct)
    {
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
    public async Task<IActionResult> getAllHeadOrders(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetOrderHeadsTableQuery(await ReadTableAsync(ct)), ct));
    [HttpGet]
    public async Task<IActionResult> DelOrder(Guid id, CancellationToken ct)
    {
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
