using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyFinancialHistory;
using CRM.Core.ApplicationServices.Supplies.Queries.PrepareSupplyFinancial;
using CRM.Core.ApplicationServices.Supplies.Queries.SearchSupplyFinancialOptions;
using CRM.Core.ApplicationServices.Supplies.Queries.PreviewSupplyFinancial;
using CRM.Core.ApplicationServices.Supplies.Commands.SaveSupplyVendorMapping;
using CRM.Core.ApplicationServices.Supplies.Commands.SubmitSupplyFinancial;
using CRM.Core.Domain.Supplies.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

public partial class SupplyController
{
    private const string FinancialExportRoles = "مدیر ارشد,مدیر مالی,کارشناس مالی";

    [HttpGet, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> FinancialHistory(Guid sourceId, bool receipt, CancellationToken ct) =>
        FinancialResponseAsync(() => requests.QueryAsync(new GetSupplyFinancialHistoryQuery(sourceId, receipt), ct));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> PrepareFinancial([FromBody] SupplyFinancialRequest input, CancellationToken ct) =>
        FinancialInputAsync(input, () => requests.QueryAsync(new PrepareSupplyFinancialQuery(input), ct));

    [HttpGet, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> FinancialOptions(Guid externalSoftwareId, string search, bool items, CancellationToken ct) =>
        FinancialResponseAsync(() => requests.QueryAsync(new SearchSupplyFinancialOptionsQuery(
            new SupplyFinancialRequest { ExternalSoftwareId = externalSoftwareId, Search = search }, items), ct));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> SaveFinancialVendor([FromBody] SupplyFinancialRequest input, CancellationToken ct) =>
        FinancialInputAsync(input, () => requests.CommandAsync(new SaveSupplyVendorMappingCommand(input), ct));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> PreviewFinancial([FromBody] SupplyFinancialRequest input, CancellationToken ct) =>
        FinancialInputAsync(input, () => requests.QueryAsync(new PreviewSupplyFinancialQuery(input), ct));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> SubmitFinancial([FromBody] SupplyFinancialRequest input, CancellationToken ct) =>
        FinancialInputAsync(input, () => requests.CommandAsync(new SubmitSupplyFinancialCommand(input), ct));

    private Task<IActionResult> FinancialInputAsync(SupplyFinancialRequest input, Func<Task<SupplyResult>> run)
    {
        if (input == null || input.SupplyFactorId == Guid.Empty || input.ExternalSoftwareId == Guid.Empty)
            return Task.FromResult<IActionResult>(BadRequest("فاکتور تأمین یا نرم‌افزار مقصد معتبر نیست."));
        return FinancialResponseAsync(run);
    }

    private async Task<IActionResult> FinancialResponseAsync(Func<Task<SupplyResult>> run)
    {
        if (!ModelState.IsValid)
            return BadRequest("اطلاعات انتقال معتبر نیست.");
        try
        {
            var result = await run();
            return result.Kind switch {
                SupplyResultKind.Succeeded => Ok(result.Data),
                SupplyResultKind.Missing => NotFound(result.Error),
                SupplyResultKind.Unauthorized => Unauthorized(),
                _ => BadRequest(result.Error)
            };
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException)
        {
            return Conflict("ثبت سند یا ارتباط آن تکمیل نشد. وضعیت انتقال را دوباره بررسی کنید.");
        }
        catch (System.Data.Common.DbException)
        {
            return StatusCode(503, "ارتباط با پایگاه داده مالی تکمیل نشد. وضعیت انتقال را دوباره بررسی کنید.");
        }
    }
}
