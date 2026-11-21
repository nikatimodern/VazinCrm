using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.QualityControls.Commands.AddQualityControlCustomerNote;
using CRM.Core.ApplicationServices.QualityControls.Commands.UpdateQualityControlCustomerNote;
using CRM.Core.ApplicationServices.QualityControls.Commands.DeleteQualityControlCustomerNote;
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
            || request.Sort is not ("pending" or "discrepancy" or "reviewed" or "latest" or "invoices" or "orders" or "name" or "code")
            || !TryPersianDate(request.FromDate, out var from)
            || !TryPersianDate(request.ToDate, out var to)
            || (from.HasValue && to.HasValue && from > to))
            return BadRequest(new { message = "فیلتر یا بازه تاریخ سفارش معتبر نیست." });

        request.FromGregorian = from;
        request.ToGregorianExclusive = to?.AddDays(1);
        return Ok(await requests.QueryAsync(new GetQualityControlCustomersQuery(request), ct));
    }

    private static bool TryPersianDate(string? raw, out DateTime? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(raw)) return true;
        var ascii = new string(raw.Trim().Select(ch =>
            ch is >= '۰' and <= '۹' ? (char)('0' + ch - '۰')
                : ch is >= '٠' and <= '٩' ? (char)('0' + ch - '٠') : ch).ToArray());
        var parts = ascii.Split('/');
        if (parts.Length != 3 || parts[0].Length != 4 || parts[1].Length != 2
            || parts[2].Length != 2
            || !int.TryParse(parts[0], out var year)
            || !int.TryParse(parts[1], out var month)
            || !int.TryParse(parts[2], out var day)
            || year is < 1300 or > 1600)
            return false;
        try
        {
            parsed = new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerForm(
        Guid customerId, [FromQuery] QualityControlItemPageRequest page, CancellationToken ct)
    {
        if (!ModelState.IsValid || customerId == Guid.Empty || page.Start < 0
            || page.Length is not (0 or 5 or 10 or 25 or 50)
            || (page.Status.HasValue && page.Status is not (0 or 1 or 2))
            || page.Sort is not ("order" or "invoice" or "product" or "quantity" or "supplier" or "expert" or "status" or "row")
            || page.Search?.Length > 200)
            return BadRequest(new { message = "فیلتر یا صفحه‌بندی اقلام معتبر نیست." });
        var result = await requests.QueryAsync(new GetQualityControlCustomerFormQuery(customerId, page), ct);
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
    public async Task<IActionResult> UpdateCustomerNote(
        [FromBody] UpdateQualityControlCustomerNoteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null)
            return BadRequest(new { message = "اطلاعات ویرایش یادداشت معتبر نیست." });
        var result = await requests.CommandAsync(new UpdateQualityControlCustomerNoteCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCustomerNote(
        [FromBody] DeleteQualityControlCustomerNoteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null)
            return BadRequest(new { message = "اطلاعات حذف یادداشت معتبر نیست." });
        var result = await requests.CommandAsync(new DeleteQualityControlCustomerNoteCommand(request), ct);
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
