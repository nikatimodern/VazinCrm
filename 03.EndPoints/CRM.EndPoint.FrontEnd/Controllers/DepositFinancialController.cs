using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Services.Accountancies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Route("DepositFinancial/[action]")]
[Authorize(Roles = "مدیر مالی,کارشناس مالی,مدیر ارشد,مدیر عامل")]
public sealed class DepositFinancialController(DepositFinancialService service) : Controller
{
    [HttpGet]
    public Task<IActionResult> History(Guid id, CancellationToken ct) =>
        RespondAsync(() => service.HistoryAsync(id, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Prepare([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(() => service.PrepareAsync(request, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Preview([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(() => service.PreviewAsync(request, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SaveCustomerMapping([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(async () => new {
            Id = await service.SaveCustomerMappingAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier), ct)
        });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SaveBankMapping([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(async () => new {
            Id = await service.SaveBankMappingAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier), ct)
        });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Submit([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() :
            RespondAsync(async () => await service.SubmitAsync(request, User.FindFirstValue(ClaimTypes.NameIdentifier), ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteMissing([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(async () => {
            await service.DeleteMissingAsync(request.Id, request.SoftwareId, ct);
            return new { Deleted = true };
        });

    private Task<IActionResult> InvalidRequest() =>
        Task.FromResult<IActionResult>(BadRequest("اطلاعات ارسال واریزی معتبر نیست."));

    private async Task<IActionResult> RespondAsync(Func<Task<object>> action)
    {
        if (!ModelState.IsValid) return BadRequest("اطلاعات ارسال واریزی معتبر نیست.");
        try { return Ok(await action()); }
        catch (InvalidOperationException exception) { return BadRequest(exception.Message); }
    }
}
