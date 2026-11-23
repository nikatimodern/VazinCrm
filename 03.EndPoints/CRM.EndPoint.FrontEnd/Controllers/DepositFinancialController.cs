using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services.Accountancies;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Route("DepositFinancial/[action]")]
[Authorize(Roles = "مدیر مالی,کارشناس مالی,مدیر ارشد,مدیر عامل")]
public sealed class DepositFinancialController(DepositFinancialService service, UserManager<AppUser> users,
    ILogger<DepositFinancialController> logger) : Controller
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
            Id = await service.SaveCustomerMappingAsync(request, await GetActorAsync(), ct)
        });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SaveBankMapping([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(async () => new {
            Id = await service.SaveBankMappingAsync(request, await GetActorAsync(), ct)
        });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Submit([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() :
            RespondAsync(async () => await service.SubmitAsync(request, await GetActorAsync(), ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PrepareChequeOperation([FromBody] DepositChequeOperationRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(() => service.PrepareChequeOperationAsync(request, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PreviewChequeOperation([FromBody] DepositChequeOperationRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(() => service.PreviewChequeOperationAsync(request, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> SubmitChequeOperation([FromBody] DepositChequeOperationRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() :
            RespondAsync(async () => await service.SubmitChequeOperationAsync(request, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteMissing([FromBody] DepositTransferRequest request, CancellationToken ct) =>
        request == null ? InvalidRequest() : RespondAsync(async () => {
            await service.DeleteMissingAsync(request.Id, request.SoftwareId, ct);
            return new { Deleted = true };
        });

    private async Task<AppUser> GetActorAsync() =>
        await users.GetUserAsync(User)
        ?? throw new SepidarTransferValidationException("کاربر جاری شناسایی نشد.");

    private Task<IActionResult> InvalidRequest() =>
        Task.FromResult<IActionResult>(BadRequest("اطلاعات ارسال واریزی معتبر نیست."));

    private async Task<IActionResult> RespondAsync(Func<Task<object>> action)
    {
        if (!ModelState.IsValid) return BadRequest("اطلاعات ارسال واریزی معتبر نیست.");
        try { return Ok(await action()); }
        catch (SepidarTransferValidationException exception) { return BadRequest(exception.Message); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError("Deposit financial operation {Action} failed ({TraceId}). {ExceptionType}\n{StackTrace}",
                ControllerContext.ActionDescriptor?.ActionName, HttpContext.TraceIdentifier,
                exception.GetType().FullName, exception.StackTrace);
            return StatusCode(500, "انجام عملیات ارتباط مالی ممکن نشد. دوباره تلاش کنید.");
        }
    }
}
