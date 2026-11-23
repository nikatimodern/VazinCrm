using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.PreFactors.Repositories;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = "مدیر ارشد,مدیر عامل,مدیر مالی,کارشناس مالی")]
public sealed class PreFactorDestinationController(IPreFactorDestinationRepository repository, Sepidar1Service first,
    Sepidar2Service second, SepidarUserResolver users, DateTimeServices dates,
    ILogger<PreFactorDestinationController> logger) : Controller
{
    [HttpGet]
    public Task<IActionResult> RelatedInvoices([FromQuery] PreFactorDestinationKey key, CancellationToken ct) =>
        RespondAsync(async () => Ok((await repository.GetRelatedInvoicesAsync(key, ct)).Select(invoice => new {
            invoice.Number,
            Date = dates.GetDatePersian(invoice.Date),
            invoice.CustomerName,
            RegisteredAt = invoice.RegisteredAt ?? dates.GetDateTimePersian(invoice.CreationDate),
            invoice.RegisteredBy,
            ViewUrl = Url.Action("PreviewFactorSepidarPrint", "Factor", new { id = invoice.Id, softwareId = key.ExternalSoftwareId })
        })));

    [HttpGet]
    public Task<IActionResult> Details([FromQuery] PreFactorDestinationKey key, bool editing, CancellationToken ct) =>
        RespondAsync(async () =>
        {
            var document = await repository.GetAsync(key, ct);
            if (document == null)
                return NotFound("پیش‌فاکتور در نرم‌افزار مقصد پیدا نشد.");
            if (!editing || document.Restriction != null)
                return Ok(new { document });

            var destination = Resolve(key.ExternalSoftwareId);
            var saleTypes = await destination.GetSaleTypes(ct);
            var customers = document.CanChangeCustomer ? await destination.GetSepidarCustomersAsync(ct, activeOnly: true) : [];
            var items = await destination.GetSepidarItemsAsync(ct);
            var stocks = await destination.GetStocksByItemIds(document.Items.Select(x => x.ItemId).Distinct().ToList(), ct);
            return Ok(new { document, saleTypes, customers, items, stocks });
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Save([FromBody] PreFactorDestinationEdit request, CancellationToken ct) =>
        RespondAsync(async () =>
        {
            if (request?.Key == null || !ModelState.IsValid)
                return BadRequest("اطلاعات ویرایش پیش‌فاکتور معتبر نیست.");
            var user = await users.GetCurrentExternalUserIdAsync(request.Key.ExternalSoftwareId,
                Resolve(request.Key.ExternalSoftwareId), ct);
            await repository.SaveAsync(request, user, ct);
            return Ok(new { saved = true });
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete([FromBody] PreFactorDestinationDelete request, CancellationToken ct) =>
        RespondAsync(async () =>
        {
            if (request?.Key == null || !ModelState.IsValid)
                return BadRequest("اطلاعات حذف پیش‌فاکتور معتبر نیست.");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Forbid();
            await users.GetCurrentExternalUserIdAsync(request.Key.ExternalSoftwareId,
                Resolve(request.Key.ExternalSoftwareId), ct);
            await repository.DeleteAsync(request, userId, User.Identity?.Name ?? userId, ct);
            return Ok(new { deleted = true });
        });

    private SepidarService Resolve(Guid id) => id == Sepidar1SoftwareId ? first : id == Sepidar2SoftwareId ? second :
        throw new PreFactorDestinationException("نرم‌افزار مقصد معتبر نیست.");

    private async Task<IActionResult> RespondAsync(Func<Task<IActionResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (PreFactorDestinationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (SepidarTransferValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Sepidar pre-factor maintenance failed ({ErrorType}).", ex.GetType().Name);
            return StatusCode(500, "نتیجه عملیات قابل تأیید نیست. سوابق ارسال را دوباره بررسی کنید.");
        }
    }
}
