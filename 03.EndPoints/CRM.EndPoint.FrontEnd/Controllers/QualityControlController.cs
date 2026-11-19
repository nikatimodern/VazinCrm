using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.QualityControls.Commands.AddQualityControlCustomerNote;
using CRM.Core.ApplicationServices.QualityControls.Commands.ChangeQualityControlContactHistory;
using CRM.Core.ApplicationServices.QualityControls.Commands.SaveQualityControlItems;
using CRM.Core.ApplicationServices.QualityControls.Commands.SetQualityControlCustomerContact;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlContactHistory;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomerNotes;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomers;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomerForm;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.ApplicationServices.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = QualityControlAccessPolicy.Roles)]
public sealed class QualityControlController(ICqrsDispatcher requests) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await requests.QueryAsync(new GetWorkspacePageQuery("QualityControl"), ct));

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] QualityControlCustomerListRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request.Start < 0 || request.Length is < 1 or > 100
            || (request.Status.HasValue && request.Status is not (0 or 1 or 2 or 3 or 4 or 5))
            || request.Sort is not ("pending" or "discrepancy" or "reviewed" or "invoices" or "orders" or "name" or "code"))
            return BadRequest(new { message = "فیلتر یا صفحه‌بندی معتبر نیست." });
        return Ok(await requests.QueryAsync(new GetQualityControlCustomersQuery(request), ct));
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerForm(Guid customerId, CancellationToken ct)
    {
        if (!ModelState.IsValid || customerId == Guid.Empty)
            return BadRequest(new { message = "مشتری معتبر نیست." });
        var result = await requests.QueryAsync(new GetQualityControlCustomerFormQuery(customerId), ct);
        return result == null ? NotFound(new { message = "مشتری یا فاکتورهای قابل بررسی یافت نشد." }) : Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerNotes(
        Guid customerId, int start = 0, int length = 10, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || customerId == Guid.Empty || start < 0 || length is < 1 or > 50)
            return BadRequest(new { message = "مشتری یا صفحه‌بندی یادداشت‌ها معتبر نیست." });
        var result = await requests.QueryAsync(
            new GetQualityControlCustomerNotesQuery(customerId, start, length), ct);
        return result == null
            ? NotFound(new { message = "مشتری واجد شرایط یافت نشد." })
            : Ok(result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCustomerNote(
        [FromBody] AddQualityControlCustomerNoteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null || request.CustomerId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 4000)
            return BadRequest(new { message = "یادداشت مشتری معتبر نیست." });
        var result = await requests.CommandAsync(new AddQualityControlCustomerNoteCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCustomerContactStatus([FromBody] SetQualityControlCustomerContactRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null)
            return BadRequest(new { message = "اطلاعات وضعیت تماس معتبر نیست." });
        var result = await requests.CommandAsync(new SetQualityControlCustomerContactCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message, status = result.Status,
            retryAfterSeconds = result.RetryAfterSeconds });
    }

    [HttpGet]
    public async Task<IActionResult> GetContactHistory(
        Guid customerId, bool includeDeleted = false, int start = 0, int length = 20,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid || customerId == Guid.Empty || start < 0 || length is < 1 or > 100)
            return BadRequest(new { message = "شناسه مشتری یا صفحه‌بندی تاریخچه معتبر نیست." });
        var data = await requests.QueryAsync(
            new GetQualityControlContactHistoryQuery(customerId, includeDeleted, start, length), ct);
        return data == null
            ? NotFound(new { message = "مشتری واجد شرایط یافت نشد." })
            : Ok(data);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ChangeContactHistory([FromBody] ChangeQualityControlContactHistoryRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null || request.CustomerId == Guid.Empty
            || request.HistoryId == Guid.Empty || request.RowVersion == null)
            return BadRequest(new { message = "اطلاعات تاریخچه تماس معتبر نیست." });
        var result = await requests.CommandAsync(
            new ChangeQualityControlContactHistoryCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItems([FromBody] SaveQualityControlItemsRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null)
            return BadRequest(new { message = "اطلاعات کنترل کیفیت معتبر نیست." });
        var result = await requests.CommandAsync(new SaveQualityControlItemsCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message, versions = result.Versions });
    }
}
