using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.QualityControls.Commands.SaveQualityControl;
using CRM.Core.ApplicationServices.QualityControls.Commands.DeleteQualityControl;
using CRM.Core.ApplicationServices.QualityControls.Commands.MarkQualityControlNoResponse;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlDeals;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlForm;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlQuestions;
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
    public async Task<IActionResult> GetDeals([FromQuery] QualityControlListRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request.Draw < 0 || request.Start < 0 || request.Length is < 1 or > 100
            || (request.FinancialStatus.HasValue && request.FinancialStatus is not (0 or 1))
            || (request.Status.HasValue && !Enum.IsDefined(request.Status.Value)))
            return BadRequest(new { message = "فیلتر یا صفحه‌بندی معتبر نیست." });
        return Ok(await requests.QueryAsync(new GetQualityControlDealsQuery(request), ct));
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions(CancellationToken ct)
        => Ok(await requests.QueryAsync(new GetQualityControlQuestionsQuery(), ct));

    [HttpGet]
    public async Task<IActionResult> GetForm(Guid dealId, CancellationToken ct)
    {
        if (!ModelState.IsValid || dealId == Guid.Empty)
            return BadRequest(new { message = "سفارش معتبر نیست." });
        var form = await requests.QueryAsync(new GetQualityControlFormQuery(dealId), ct);
        return form == null ? NotFound(new { message = "سفارش یافت نشد یا به آن دسترسی ندارید." }) : Ok(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] DeleteQualityControlRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null)
            return BadRequest(new { message = "اطلاعات حذف معتبر نیست." });
        var result = await requests.CommandAsync(new DeleteQualityControlCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message, status = result.Status });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNoResponse([FromBody] MarkQualityControlNoResponseRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null)
            return BadRequest(new { message = "اطلاعات درخواست معتبر نیست." });
        var result = await requests.CommandAsync(new MarkQualityControlNoResponseCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message, status = result.Status, rowVersion = result.RowVersion });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] SaveQualityControlRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid || request == null)
            return BadRequest(new { message = "اطلاعات فرم معتبر نیست." });
        var result = await requests.CommandAsync(new SaveQualityControlCommand(request), ct);
        return StatusCode(result.StatusCode, new { message = result.Message, status = result.Status, rowVersion = result.RowVersion });
    }
}
