using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Factors.Repositories;
using CRM.EndPoint.FrontEnd.Models.Sepidar;
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
    [HttpGet, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> GetExternalFactorSync([FromQuery] SalesInvoiceDestinationKey key,
        [FromServices] ISalesInvoiceDestinationRepository repository, CancellationToken ct) =>
        RespondToFinancialEditAsync(async () =>
        {
            if (!ModelState.IsValid || key == null || key.SourceId == Guid.Empty)
                return BadRequest("سابقه فاکتور فروش معتبر نیست.");
            var service = GetSepidarService(key.ExternalSoftwareId)
                ?? throw new InvalidOperationException("نرم‌افزار مالی مقصد معتبر نیست.");
            var preview = await repository.GetSyncPreviewAsync(key, ct);
            var allowed = preview.Impact.Blockers.Count == 0;
            return Ok(new { preview, items = allowed ? await service.GetSepidarItemsAsync(ct) : [],
                settings = allowed ? await service.GetSalesEditPostingSettingsAsync(ct) : null });
        });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> SynchronizeExternalFactor([FromBody] SalesInvoiceSyncRequest request,
        [FromServices] ISalesInvoiceDestinationRepository repository, CancellationToken ct) =>
        RespondToFinancialEditAsync(async () =>
        {
            if (!ModelState.IsValid || request?.Key == null || request.Key.SourceId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("اطلاعات همگام‌سازی فاکتور معتبر نیست.");
            var service = GetSepidarService(request.Key.ExternalSoftwareId)
                ?? throw new InvalidOperationException("نرم‌افزار مالی مقصد معتبر نیست.");
            var modifier = await sepidarUsers.GetCurrentExternalUserIdAsync(request.Key.ExternalSoftwareId, service, ct);
            var settings = new SepidarService.SalesPostingSettings(request.AutoVoucher, request.AutoDelivery);
            await repository.SynchronizeAsync(request, modifier, service.AcquireSepidarInvoiceExportLockAsync,
                service.GenerateInvoiceItemsAsync, (posting, token) => service.PostEditedSalesInvoiceAsync(posting, settings, token), ct);
            return Ok(new { saved = true });
        });

    public sealed record ExternalFactorEditRequest(SalesInvoiceDestinationKey Key, string Token, string DateText,
        int Number, int? CustomerId, int? SaleTypeId, bool IncludeBroker, SalesInvoiceEditSelection[] Items,
        bool AutoVoucher, bool AutoDelivery, int? PreviewBrokerPartyId);

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
            if (state.RequiresSynchronization)
                return Conflict("ابتدا فاکتور را با ERP همگام‌سازی کنید.");
            var canEdit = state.Impact.Blockers.Count == 0;
            var direct = !state.QuotationId.HasValue && canEdit;
            var customers = direct ? await service.GetSepidarCustomersAsync(ct, activeOnly: true) : [];
            var saleTypes = direct ? await service.GetSaleTypes(ct) : [];
            var settings = canEdit ? await service.GetSalesEditPostingSettingsAsync(ct) : null;
            var factor = await db.Factors.AsNoTracking().SingleAsync(x => x.Id == key.SourceId, ct);
            return Ok(new {
                state.Impact, state.QuotationId, state.CustomerId, state.CustomerName, state.SaleTypeId,
                state.Rows, DateText = BaseServices.GetDatePersian(state.Date), IncludeBroker = state.BrokerPartyId.HasValue,
                SalespersonName = await GetFactorSalespersonNameAsync(factor, ct),
                AutoVoucher = settings?.AutoVoucher ?? false, AutoDelivery = settings?.AutoDelivery ?? false,
                Customers = customers.Select(x => new { x.Id, Code = x.DlCode, Title = x.FullName }),
                SaleTypes = saleTypes.Select(x => new { Id = x.SaleTypeId, Code = x.Number, x.Title }),
                Items = canEdit ? await service.GetSepidarItemsAsync(ct) : []
            });
        });

    [HttpGet, Authorize(Roles = FinancialExportRoles)]
    public Task<IActionResult> GetExternalFactorEditBroker([FromQuery] SalesInvoiceDestinationKey key, string token,
        [FromServices] ISalesInvoiceDestinationRepository repository, CancellationToken ct) =>
        RespondToFinancialEditAsync(async () =>
        {
            if (!ModelState.IsValid || key == null || key.SourceId == Guid.Empty || string.IsNullOrWhiteSpace(token))
                return BadRequest("اطلاعات بررسی واسط معتبر نیست.");
            var service = GetSepidarService(key.ExternalSoftwareId)
                ?? throw new InvalidOperationException("نرم‌افزار مالی مقصد معتبر نیست.");
            var state = await repository.GetEditAsync(key, ct);
            if (state.Impact.Token != token)
                return Conflict("فاکتور یا اسناد وابسته تغییر کرده‌اند. فرم را دوباره باز کنید.");
            if (state.Impact.Blockers.Count > 0)
                return Conflict(string.Join(" ", state.Impact.Blockers));
            var broker = await GetExternalFactorEditBrokerAsync(key, state, service, ct);
            return Ok(new { broker });
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
            var broker = request.IncludeBroker ? await GetExternalFactorEditBrokerAsync(request.Key, state, service, ct) : null;
            if (broker?.PartyId != request.PreviewBrokerPartyId)
                return Conflict("اطلاعات واسط فروش تغییر کرده است. واسط را دوباره بررسی کنید.");
            var modifier = await sepidarUsers.GetCurrentExternalUserIdAsync(request.Key.ExternalSoftwareId, service, ct);
            var settings = new SepidarService.SalesPostingSettings(request.AutoVoucher, request.AutoDelivery);
            await repository.EditAsync(request.Key, request.Token, modifier,
                new(date, request.Number, request.CustomerId, request.SaleTypeId, broker?.PartyId, request.Items,
                    request.AutoVoucher, request.AutoDelivery),
                service.AcquireSepidarInvoiceExportLockAsync,
                (posting, token) => service.PostEditedSalesInvoiceAsync(posting, settings, token), ct);
            return Ok(new { saved = true });
        });

    private async Task<SepidarInvoiceBrokerPreviewDto> GetExternalFactorEditBrokerAsync(SalesInvoiceDestinationKey key,
        SalesInvoiceEditState state, SepidarService service, CancellationToken ct)
    {
        var partyId = state.BrokerPartyId;
        if (!partyId.HasValue)
        {
            var factor = await db.Factors.AsNoTracking().SingleAsync(x => x.Id == key.SourceId, ct);
            partyId = await ResolveExternalBrokerPartyIdAsync(factor, key.ExternalSoftwareId, true, service, ct);
        }
        if (partyId is not > 0)
            throw new InvalidOperationException("واسط فروش در سپیدار مشخص نشده است.");
        return await service.CalculateInvoiceBrokerAsync(partyId.Value, state.Price, state.Discount, state.Rate, ct);
    }

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
