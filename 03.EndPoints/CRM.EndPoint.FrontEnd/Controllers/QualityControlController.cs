using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.QualityControls.Commands.SaveQualityControlItems;
using CRM.Core.ApplicationServices.QualityControls.Commands.SetQualityControlCustomerContact;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomers;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomerForm;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.Domain.QualityControls.Models;
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
            || (request.Status.HasValue && request.Status is not (0 or 1 or 2 or 3 or 4)))
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCustomerContactStatus([FromBody] SetQualityControlCustomerContactRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null)
            return BadRequest(new { message = "اطلاعات وضعیت تماس معتبر نیست." });
        var result = await requests.CommandAsync(new SetQualityControlCustomerContactCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message, status = result.Status });
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
