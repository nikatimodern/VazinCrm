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
using CRM.EndPoint.FrontEnd.Security;
namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public class ReportController(ICqrsDispatcher requests, DocumentAccessPolicy access) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await access.HasViewAsync("Report", cancellationToken)) return Forbid();
        return View(await requests.QueryAsync(new GetReportBaseItemsQuery(), cancellationToken));
    }

    public async Task<IActionResult> GetBaseItemsReport(CancellationToken cancellationToken)
    {
        if (!await access.HasViewAsync("Report", cancellationToken)) return Forbid();
        try { return Ok(await requests.QueryAsync(new GetReportBaseItemsQuery(), cancellationToken)); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return BadRequest(); }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ExportExcelReport([FromBody] ReportFilterVModel model, CancellationToken cancellationToken)
    {
        if (!await access.HasViewAsync("Report", cancellationToken) || !await access.HasAnyAsync("Report", "Export", cancellationToken)) return Forbid();
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
        if (!await access.CanAsync("Report", "Delete", id, cancellationToken)) return Forbid();
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
    public async Task<IActionResult> GetReport(Guid Id, CancellationToken cancellationToken)
    {
        if (!await access.CanAsync("Report", "View", Id, cancellationToken)) return Forbid();
        return Ok(await requests.QueryAsync(new GetReportQuery(Id), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> GetAllReportPartial([FromBody] ReportFilterVModel model, [FromQuery] Guid? dealId = null, CancellationToken cancellationToken = default)
    {
        if (!await access.HasViewAsync("Report", cancellationToken)) return Forbid();
        var result = await requests.QueryAsync(new GetReportPartialQuery(dealId.HasValue ? new ReportSearch() : model.ToReportSearch(), dealId), cancellationToken);
        return Ok(new { count = result.Count, html = result.Html });
    }

    public async Task<IActionResult> GetCallReport(string mobileNumber, CancellationToken cancellationToken)
    {
        if (!await access.HasViewAsync("Report", cancellationToken) || !await access.HasViewAsync("CallRecording", cancellationToken)) return Forbid();
        var result = await requests.QueryAsync(new GetCallReportsQuery(mobileNumber), cancellationToken);
        return Ok(new { count = result.Count, html = result.Html });
    }

    [HttpPost]
    public async Task<IActionResult> AddReport(Report model, CancellationToken cancellationToken)
    {
        if (model == null) return BadRequest();
        if (model.Id == Guid.Empty ? !await access.HasAsync("Report", "Create", cancellationToken) :
            !await access.CanAsync("Report", "Edit", model.Id, cancellationToken)) return Forbid();
        if (model.CallRecordingId.HasValue && !await access.CanAsync("CallRecording", "View", model.CallRecordingId.Value, cancellationToken)) return Forbid();
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
        if (!await access.CanAsync("CallRecording", "Listen", callId, cancellationToken)) return Forbid();
        var file = await requests.QueryAsync(new GetCallAudioQuery(callId), cancellationToken);
        return file is null ? NotFound("رکوردی یافت نشد!") : File(file.Stream, "audio/wav", file.FileName);
    }
}
