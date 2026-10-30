using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Reports.Commands.AddReport;
using CRM.Core.ApplicationServices.Reports.Commands.SaveReport;
using CRM.Core.ApplicationServices.Reports.Commands.DeleteReport;
using CRM.Core.ApplicationServices.Reports.Queries.GetReport;
using CRM.Core.ApplicationServices.Reports.Queries.GetReportBaseItems;
using CRM.Core.ApplicationServices.Reports.Queries.GetReportPartial;
using CRM.Core.ApplicationServices.Reports.Queries.GetCallReports;
using CRM.Core.ApplicationServices.Reports.Queries.GetCallAudio;
using CRM.Core.ApplicationServices.Reports.Queries.ExportReports;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Models;
using CRM.EndPoint.FrontEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public class ReportController(ICqrsDispatcher requests) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await requests.QueryAsync(new GetReportBaseItemsQuery(), cancellationToken));

    public async Task<IActionResult> GetBaseItemsReport(CancellationToken cancellationToken)
    {
        try { return Ok(await requests.QueryAsync(new GetReportBaseItemsQuery(), cancellationToken)); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return BadRequest(); }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ExportExcelReport([FromBody] ReportFilterVModel model, CancellationToken cancellationToken)
    {
        try
        {
            var file = await requests.QueryAsync(new ExportReportsQuery(model.ToReportSearch()), cancellationToken);
            return File(file.Contents, file.ContentType, file.FileName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return BadRequest(); }
    }

    public async Task<IActionResult> DelReport(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await requests.CommandAsync(new DeleteReportCommand(id), cancellationToken);
            if (result == DeleteReportResult.Deleted) return Ok();
            return result == DeleteReportResult.Forbidden ? BadRequest("شما مجاز به حذف این گزارش نیستید!") : BadRequest();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return BadRequest(); }
    }

    [HttpGet]
    public async Task<IActionResult> GetReport(Guid Id, CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetReportQuery(Id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> GetAllReportPartial([FromBody] ReportFilterVModel model, [FromQuery] Guid? dealId = null, CancellationToken cancellationToken = default)
    {
        var result = await requests.QueryAsync(new GetReportPartialQuery(dealId.HasValue ? new ReportSearch() : model.ToReportSearch(), dealId), cancellationToken);
        return Ok(new { count = result.Count, html = result.Html });
    }

    public async Task<IActionResult> GetCallReport(string mobileNumber, CancellationToken cancellationToken)
    {
        var result = await requests.QueryAsync(new GetCallReportsQuery(mobileNumber), cancellationToken);
        return Ok(new { count = result.Count, html = result.Html });
    }

    [HttpPost]
    public async Task<IActionResult> AddReport(Report model, CancellationToken cancellationToken)
    {
        try
        {
            var result = await requests.CommandAsync(new SaveReportCommand(model), cancellationToken);
            if (result == AddReportResult.NotFound) return BadRequest();
            if (result == AddReportResult.Forbidden)
                return Ok(new { status = 201, message = "شما مجاز به ویرایش این گزارش نیستید", data = "" });
            return Ok(new { status = 200, message = result == AddReportResult.Created ? "گزارش جدید ثبت شد" : "گزارش ویرایش شد", data = "" });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return BadRequest(); }
    }

    [HttpGet]
    public async Task<IActionResult> GetCallAudio(Guid callId, CancellationToken cancellationToken)
    {
        var file = await requests.QueryAsync(new GetCallAudioQuery(callId), cancellationToken);
        return file is null ? NotFound("رکوردی یافت نشد!") : File(file.Stream, "audio/wav", file.FileName);
    }
}
