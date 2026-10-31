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
public class RequestAmountController(ICqrsDispatcher dispatcher) : Controller
{
    [Authorize(Roles = "مدیر تامین ,کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی, تنخواه دار")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await dispatcher.QueryAsync(new GetWorkspacePageQuery("RequestAmount"), ct));
    [HttpPost]
    public async Task<IActionResult> AddRequestAmount([FromBody] RequestAmount model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new AddRequestAmountCommand(model), ct));
    [HttpPost]
    public async Task<IActionResult> UpdateRequestAmount([FromBody] RequestAmount model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new UpdateRequestAmountCommand(model), ct));
    [HttpPost]
    public async Task<IActionResult> ChangeDocTypeRequestAmount([FromBody] RequestAmount model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new ChangeDocTypeRequestAmountCommand(model), ct));
    [HttpPost]
    public async Task<IActionResult> SetAcceptedRequestAmount([FromBody] RequestAmount model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new SetAcceptedRequestAmountCommand(model), ct));
    public async Task<IActionResult> ChangeStatusRequestAmount(Guid id, RequestStatusEnum status, CancellationToken ct) => Map(await dispatcher.CommandAsync(new ChangeStatusRequestAmountCommand(id, status), ct));
    [HttpPost]
    [Authorize(Roles = "کارشناس مالی, مدیر مالی, مدیر ارشد")]
    public async Task<IActionResult> AddPaymentByRequestAmount([FromBody] Payment model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new AddPaymentByRequestAmountCommand(model), ct));
    public async Task<IActionResult> DeletePayment(Guid id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new DeletePaymentCommand(id), ct));
    public async Task<IActionResult> DeleteRequestAmount(Guid id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new DeleteRequestAmountCommand(id), ct));
    [HttpPost]
    public async Task<IActionResult> GetAllPaymentsByRequestAmount([FromQuery] Guid id, CancellationToken ct) => Json(await dispatcher.QueryAsync(new GetRequestPaymentsTableQuery(id, await ReadTableAsync(ct)), ct));
    public async Task<IActionResult> GetAllRequestAmountsPartial(int type = -1, string search = "", Guid? orderHeadId = null, CancellationToken ct = default) => Ok(await dispatcher.QueryAsync(new GetRequestAmountListQuery(type, search, orderHeadId), ct));
    public async Task<IActionResult> GetRequestAmount(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetRequestAmountDetailsQuery(id), ct));
    [HttpPost]
    public async Task<IActionResult> GetRequestsAmountByFilteredIds([FromBody] GetAllBySelectedVModel model, CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetSelectedRequestsQuery(model.FilterIds ?? []), ct);
        return data.Error == null ? Ok(data.Data) : BadRequest(data.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetRequestAmountsWithPaidAmountByPayment(Guid paymentId, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetRequestsByPaymentQuery(paymentId), ct));
    public async Task<IActionResult> GetAllRequestAmountsDt(CancellationToken ct) => Json(await dispatcher.QueryAsync(new GetRequestAmountsTableQuery(await ReadTableAsync(ct)), ct));
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
