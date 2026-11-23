using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Factors.Repositories;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.Framework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Controllers;

public partial class FactorController
{
    public sealed record ExternalFactorEditRequest(SalesInvoiceDestinationKey Key, string Token, string DateText,
        int Number, int? CustomerId, int? SaleTypeId, bool IncludeBroker, SalesInvoiceEditSelection[] Items,
        bool AutoVoucher, bool AutoDelivery);

    [HttpGet, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> GetExternalFactorEdit([FromQuery] SalesInvoiceDestinationKey key,
        [FromServices] ISalesInvoiceDestinationRepository repository, CancellationToken ct) =>
        RespondToFinancialEditAsync(async () =>
        {
            if (!ModelState.IsValid || key == null || key.SourceId == Guid.Empty)
                return BadRequest("سابقه فاکتور فروش معتبر نیست.");
            var service = GetSepidarService(key.ExternalSoftwareId)
                ?? throw new InvalidOperationException("نرم‌افزار مالی مقصد معتبر نیست.");
            var state = await repository.GetEditAsync(key, ct);
            var canEdit = state.Impact.Blockers.Count == 0;
            var direct = !state.QuotationId.HasValue && canEdit;
            var customers = direct ? await service.GetSepidarCustomersAsync(ct, activeOnly: true) : [];
            var saleTypes = direct ? await service.GetSaleTypes(ct) : [];
            var settings = canEdit ? await service.GetSalesEditPostingSettingsAsync(ct) : null;
            return Ok(new {
                state.Impact, state.QuotationId, state.CustomerId, state.CustomerName, state.SaleTypeId,
                state.Rows, DateText = BaseServices.GetDatePersian(state.Date), IncludeBroker = state.BrokerPartyId.HasValue,
                AutoVoucher = settings?.AutoVoucher ?? false, AutoDelivery = settings?.AutoDelivery ?? false,
                Customers = customers.Select(x => new { x.Id, Code = x.DlCode, Title = x.FullName }),
                SaleTypes = saleTypes.Select(x => new { Id = x.SaleTypeId, Code = x.Number, x.Title }),
                Items = canEdit ? await service.GetSepidarItemsAsync(ct) : []
            });
        });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> EditExternalFactor([FromBody] ExternalFactorEditRequest request,
        [FromServices] ISalesInvoiceDestinationRepository repository, CancellationToken ct) =>
        RespondToFinancialEditAsync(async () =>
        {
            if (!ModelState.IsValid || request?.Key == null || request.Key.SourceId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.Token) || request.Items is not { Length: > 0 } ||
                request.Items.Any(row => row == null || row.Id <= 0 || row.ItemId <= 0 || row.StockId <= 0) ||
                request.Items.Select(row => row.Id).Distinct().Count() != request.Items.Length || request.Number <= 0)
                return BadRequest("اطلاعات ویرایش فاکتور فروش معتبر نیست.");
            var text = TextService.ToEnglishNumber(request.DateText?.Trim() ?? "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"\A[0-9]{4}/[0-9]{2}/[0-9]{2}\z") ||
                dtService.GetDateMiladi(text) is not DateTime date)
                return BadRequest("تاریخ فاکتور معتبر نیست.");

            var service = GetSepidarService(request.Key.ExternalSoftwareId)
                ?? throw new InvalidOperationException("نرم‌افزار مالی مقصد معتبر نیست.");
            var state = await repository.GetEditAsync(request.Key, ct);
            if (state.Impact.Token != request.Token)
                return Conflict("فاکتور یا اسناد وابسته تغییر کرده‌اند. فرم را دوباره باز کنید.");
            int? brokerId = null;
            if (request.IncludeBroker)
            {
                brokerId = state.BrokerPartyId;
                if (!brokerId.HasValue)
                {
                    var factor = await db.Factors.AsNoTracking().SingleAsync(x => x.Id == request.Key.SourceId, ct);
                    brokerId = await ResolveExternalBrokerPartyIdAsync(factor, request.Key.ExternalSoftwareId, true, service, ct);
                }
            }
            var modifier = await sepidarUsers.GetCurrentExternalUserIdAsync(request.Key.ExternalSoftwareId, service, ct);
            var settings = new SepidarService.SalesPostingSettings(request.AutoVoucher, request.AutoDelivery);
            await repository.EditAsync(request.Key, request.Token, modifier,
                new(date, request.Number, request.CustomerId, request.SaleTypeId, brokerId, request.Items,
                    request.AutoVoucher, request.AutoDelivery),
                service.AcquireSepidarInvoiceExportLockAsync,
                (posting, token) => service.PostEditedSalesInvoiceAsync(posting, settings, token), ct);
            return Ok(new { saved = true });
        });

    private async Task<IActionResult> RespondToFinancialEditAsync(Func<Task<IActionResult>> operation)
    {
        try { return await operation(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Sepidar sales invoice edit failed ({ErrorType}).", ex.GetType().Name);
            return StatusCode(500, "ویرایش فاکتور فروش تکمیل نشد. سوابق ارسال را دوباره بررسی کنید.");
        }
    }
}
