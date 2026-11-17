using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.Sepidar.Parties.Entities;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services;

public sealed class SupplyFinancialIntegration(ISupplyFinancialRepository repository,
    Sepidar1Service first, Sepidar2Service second, UserManager<AppUser> users,
    IHttpContextAccessor http, DateTimeServices dates) : ISupplyFinancialIntegration
{
    private static readonly Guid FirstId = new("2f3d8024-6c2c-491f-c921-08df26b00138");
    private static readonly Guid SecondId = new("d6c3e8a7-1435-47d5-0489-08df26c5e2ca");
    private sealed record FinancialPreview(string PreviewToken, string DateText, string SupplierTxt,
        string Stock, string SerialNumber, string Description, decimal? TotalPrice,
        decimal? TotalTax, decimal? Discount, decimal? Total, bool Voucher, object Items);

    private SepidarService Resolve(Guid id) => id == FirstId ? first : id == SecondId ? second
        : throw new InvalidOperationException("نرم‌افزار مالی انتخاب‌شده برای انتقال تأمین پشتیبانی نمی‌شود.");

    private async Task<SepidarService> GetDestinationAsync(Guid id, CancellationToken ct)
    {
        var service = Resolve(id);
        if (!(await repository.GetDestinationsAsync(ct)).Any(x => x.Id == id))
            throw new InvalidOperationException("نرم‌افزار مالی پیدا نشد.");
        return service;
    }

    private async Task<SupplyFactor> GetSourceAsync(Guid id, CancellationToken ct) =>
        await repository.GetSourceAsync(id, ct)
        ?? throw new InvalidOperationException("فاکتور تأمین یا تأمین‌کننده آن پیدا نشد.");

    public async Task<SupplyResult> GetHistoryAsync(Guid sourceId, CancellationToken ct)
    {
        var source = await GetSourceAsync(sourceId, ct);
        var maps = await repository.GetMappingsAsync(sourceId, ct);
        var results = new List<SupplyFinancialDestination>();
        foreach (var software in (await repository.GetDestinationsAsync(ct)).Where(x => x.Id == FirstId || x.Id == SecondId))
        {
            var service = Resolve(software.Id);
            var purchaseMapping = maps.SingleOrDefault(x => x.ExternalSoftwareId == software.Id
                && x.MapType == MapType(false) && !IsPending(x));
            var receiptMapping = maps.SingleOrDefault(x => x.ExternalSoftwareId == software.Id
                && x.MapType == MapType(true) && !IsPending(x));
            var purchase = purchaseMapping == null ? null
                : await service.FindPurchaseDocumentAsync(false, ReadId(purchaseMapping), ct);
            var receipt = receiptMapping != null ? await service.FindPurchaseDocumentAsync(true, ReadId(receiptMapping), ct)
                : purchase != null ? await service.FindReceiptForPurchaseAsync(purchase.Id, ct) : null;
            results.Add(new SupplyFinancialDestination(software.Id, software.Title,
                ToHistory(purchase, purchaseMapping), ToHistory(receipt, receiptMapping)));
        }
        return SupplyResult.Success(new SupplyFinancialHistory(source.SerialNumber, results));

        static SupplyFinancialDocumentHistory? ToHistory(SepidarService.PurchaseDocument? document, ExternalMapping? mapping) =>
            document == null && mapping == null ? null : new SupplyFinancialDocumentHistory(document,
                document == null ? null : BaseServices.GetDatePersian(document.Date), mapping?.Id);
    }

    public async Task<SupplyResult> DeleteMissingMappingAsync(Guid sourceId, Guid softwareId, Guid mappingId,
        bool receipt, CancellationToken ct)
    {
        if (sourceId == Guid.Empty || softwareId == Guid.Empty || mappingId == Guid.Empty)
            return SupplyResult.Invalid("سابقه ارسال معتبر نیست.");

        var service = await GetDestinationAsync(softwareId, ct);
        await using var guard = await repository.LockAsync(sourceId, ct);
        var mapping = await repository.GetMappingAsync(sourceId, softwareId, MapType(receipt), ct);
        if (mapping == null || mapping.Id != mappingId)
            return SupplyResult.Missing("سابقه ارسال پیدا نشد.");
        if (IsPending(mapping) || await service.PurchaseDocumentExistsAsync(receipt, ReadId(mapping), ct))
            return SupplyResult.Invalid("این سابقه قابل حذف نیست. فهرست سوابق را دوباره بررسی کنید.");

        await repository.DeleteMappingAsync(mapping, ct);
        return SupplyResult.Success(new { Deleted = true });
    }

    public async Task<SupplyResult> PrepareAsync(SupplyFinancialRequest request, CancellationToken ct)
    {
        var service = await GetDestinationAsync(request.ExternalSoftwareId, ct);
        var source = await GetSourceAsync(request.SupplyFactorId, ct);
        var purchaseId = request.Receipt ? await GetPurchaseIdAsync(request, ct) : 0;
        var mapping = await repository.GetMappingAsync(source.Id, request.ExternalSoftwareId, MapType(request.Receipt), ct);
        if (mapping != null)
        {
            var document = await service.FindPurchaseDocumentAsync(request.Receipt, ReadId(mapping), ct);
            if (document != null || !IsPending(mapping))
                return SupplyResult.Success(new { Document = document, Recover = document != null && IsPending(mapping) });
        }

        if (!request.Receipt)
        {
            var vendor = await repository.GetMappingAsync(source.SupplierId, request.ExternalSoftwareId,
                ExternalMappingTypeEnum.تامین_کننده, ct);
            var vendorOptions = await service.GetPurchaseVendorsAsync(null, ct);
            var selected = vendor != null && int.TryParse(vendor.ExEntityId, out var vendorId)
                ? vendorOptions.SingleOrDefault(x => x.Id == vendorId) : null;
            return SupplyResult.Success(new {
                source.SerialNumber, source.SupplierTxt, source.Description, Vendor = selected, VendorOptions = vendorOptions,
                Items = source.Items.Select(x => new { x.Id, x.Product, x.UnitTxt, x.WeightCount, x.UnitAmount, x.TotalAmount })
            });
        }

        var receiptSource = await service.GetFullPurchaseReceiptSourceAsync(purchaseId, ct);
        var stocks = await service.GetPurchaseReceiptStocksAsync(purchaseId, ct);
        var itemDisplays = await service.GetReceiptItemDisplaysAsync(
            receiptSource.Items.Select(x => x.ItemRef).Distinct().ToArray(), ct);
        return SupplyResult.Success(new {
            Number = receiptSource.Invoice.Number, Date = receiptSource.Invoice.Date.ToString("yyyy-MM-dd"),
            DateText = BaseServices.GetDatePersian(receiptSource.Invoice.Date),
            Stocks = stocks, source.SupplierTxt, source.SerialNumber,
            Items = receiptSource.Items.Select(x => {
                var display = itemDisplays.GetValueOrDefault(x.ItemRef);
                return new {
                    Id = x.InventoryPurchaseInvoiceItemID, Product = display?.Label, UnitTxt = display?.UnitTitle?.Trim(),
                    WeightCount = x.Quantity, TotalAmount = x.NetPrice, x.Description
                };
            })
        });
    }

    public async Task<SupplyResult> SearchAsync(SupplyFinancialRequest request, bool items, CancellationToken ct)
    {
        var service = await GetDestinationAsync(request.ExternalSoftwareId, ct);
        if (items)
            return SupplyResult.Success(await service.GetPurchaseItemsAsync(ct));

        return SupplyResult.Success(await service.GetPurchaseVendorsAsync(null, ct));
    }

    public async Task<SupplyResult> SaveVendorAsync(SupplyFinancialRequest request, CancellationToken ct)
    {
        var service = await GetDestinationAsync(request.ExternalSoftwareId, ct);
        var source = await GetSourceAsync(request.SupplyFactorId, ct);
        var vendor = (await service.GetPurchaseVendorsAsync(request.VendorId, ct)).SingleOrDefault();
        if (vendor == null)
            return SupplyResult.Invalid("تأمین‌کننده معتبر در نرم‌افزار مقصد پیدا نشد.");

        var user = await GetUserAsync();
        await using var guard = await repository.LockAsync(source.SupplierId, ct);
        var mapping = await repository.GetMappingAsync(source.SupplierId, request.ExternalSoftwareId,
            ExternalMappingTypeEnum.تامین_کننده, ct)
            ?? NewMapping(source.SupplierId, request.ExternalSoftwareId, ExternalMappingTypeEnum.تامین_کننده,
                nameof(Customer), nameof(Party), user);
        mapping.ExEntityId = vendor.Id.ToString(CultureInfo.InvariantCulture);
        await repository.SaveMappingAsync(mapping, ct);
        return SupplyResult.Success(vendor);
    }

    public async Task<SupplyResult> PreviewAsync(SupplyFinancialRequest request, CancellationToken ct)
    {
        var service = await GetDestinationAsync(request.ExternalSoftwareId, ct);
        var source = await GetSourceAsync(request.SupplyFactorId, ct);
        if (request.Receipt)
        {
            var purchaseId = await GetPurchaseIdAsync(request, ct);
            var draft = await service.PreparePurchaseReceiptDraftAsync(purchaseId, request.StockId,
                GetReceiptDate(request), request.Description, request.RowDescriptions, ct);
            var labels = await service.GetPurchaseItemLabelsAsync(draft.Items.Select(x => x.ItemRef).ToArray(), ct);
            var stock = (await service.GetPurchaseReceiptStocksAsync(purchaseId, ct)).Single(x => x.Id == request.StockId);
            return SupplyResult.Success(new FinancialPreview(
                SepidarService.GetPurchasePreviewToken(draft), BaseServices.GetDatePersian(draft.Receipt.Date),
                source.SupplierTxt, stock.Title, source.SerialNumber, draft.Receipt.Description,
                draft.Receipt.TotalPrice, draft.Receipt.TotalTax, 0m, draft.Receipt.TotalNetPrice,
                draft.VoucherLines.Count != 0, draft.Items.Select(x => new {
                    Product = labels.GetValueOrDefault(x.ItemRef), x.Quantity, x.Price, x.Tax, x.Description
                })));
        }

        await ValidateVendorMappingAsync(source, request, ct);
        var purchase = await service.PreparePurchaseDraftAsync(source, request.VendorId,
            Selections(request), request.Description, ct);
        var purchaseLabels = await service.GetPurchaseItemLabelsAsync(purchase.Items.Select(x => x.ItemRef).ToArray(), ct);
        var vendor = (await service.GetPurchaseVendorsAsync(request.VendorId, ct)).Single();
        return SupplyResult.Success(new FinancialPreview(
            SepidarService.GetPurchasePreviewToken(purchase), BaseServices.GetDatePersian(purchase.Invoice.Date),
            vendor.Title, null, source.SerialNumber, purchase.Invoice.Description,
            purchase.Invoice.TotalPrice, purchase.Invoice.TotalTax, purchase.Items.Sum(x => x.Discount),
            purchase.Invoice.TotalNetPrice, false, purchase.Items.Select(x => new {
                Product = purchaseLabels.GetValueOrDefault(x.ItemRef), x.Quantity, x.Price, x.Discount, x.Tax, x.Description
            })));
    }

    public async Task<SupplyResult> SubmitAsync(SupplyFinancialRequest request, CancellationToken ct)
    {
        var service = await GetDestinationAsync(request.ExternalSoftwareId, ct);
        var user = await GetUserAsync();
        await using var guard = await repository.LockAsync(request.SupplyFactorId, ct);
        var source = await GetSourceAsync(request.SupplyFactorId, ct);
        var purchaseId = request.Receipt ? await GetPurchaseIdAsync(request, ct) : 0;
        await using var vendorGuard = request.Receipt || source.SupplierId == source.Id
            ? null : await repository.LockAsync(source.SupplierId, ct);
        var type = MapType(request.Receipt);
        var mapping = await repository.GetMappingAsync(source.Id, request.ExternalSoftwareId, type, ct);
        if (mapping != null)
        {
            var saved = await service.FindPurchaseDocumentAsync(request.Receipt, ReadId(mapping), ct);
            if (saved != null)
            {
                if (IsPending(mapping))
                    await CompleteAsync(mapping, saved.Id);
                return SupplyResult.Success(saved);
            }
            if (!IsPending(mapping))
                return SupplyResult.Invalid("سند مرتبط در سپیدار پیدا نشد. ارتباط مالی را بررسی کنید.");
        }

        if (string.IsNullOrEmpty(request.PreviewToken))
            return SupplyResult.Invalid("ابتدا پیش‌نمایش سند را تأیید کنید.");
        var preview = await PreviewAsync(request, ct);
        if (preview.Kind != SupplyResultKind.Succeeded)
            return preview;
        if (preview.Data is not FinancialPreview prepared || prepared.PreviewToken != request.PreviewToken)
            return SupplyResult.Invalid("اطلاعات سند تغییر کرده است. پیش‌نمایش جدید را بررسی کنید.");
        if (!int.TryParse(await repository.GetCreatorIdAsync(request.ExternalSoftwareId, ct), out var creator) || creator <= 0)
            return SupplyResult.Invalid("شناسه کاربر ثبت خرید برای نرم‌افزار مقصد تنظیم نشده است.");

        if (mapping == null)
        {
            var reservedId = await service.ReservePurchaseDocumentIdAsync(request.Receipt, ct);
            mapping = NewMapping(source.Id, request.ExternalSoftwareId, type, nameof(SupplyFactor),
                request.Receipt ? nameof(InventoryReceipt) : nameof(InventoryPurchaseInvoice), user);
            mapping.ExEntityId = ExternalMapping.PendingIdPrefix + reservedId.ToString(CultureInfo.InvariantCulture);
            await repository.SaveMappingAsync(mapping, ct);
        }

        var id = ReadId(mapping);
        SepidarService.PurchaseDocument document;
        try
        {
            document = request.Receipt
                ? await service.AddSupplyReceiptAsync(id, creator,
                    purchaseId, request.StockId,
                    GetReceiptDate(request), request.Description, request.RowDescriptions, request.PreviewToken, ct)
                : await service.AddSupplyPurchaseAsync(id, creator, source, request.VendorId, Selections(request),
                    request.Description, request.PreviewToken, ct);
        }
        catch (Exception failure) when (!ct.IsCancellationRequested && failure is InvalidOperationException or DbUpdateException)
        {
            await DiscardUncommittedReservationAsync(service, mapping, request.Receipt, id);
            throw;
        }
        await CompleteAsync(mapping, document.Id);
        return SupplyResult.Success(document);
    }

    private async Task DiscardUncommittedReservationAsync(SepidarService service, ExternalMapping mapping, bool receipt, int id)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            if (!await service.PurchaseDocumentExistsAsync(receipt, id, timeout.Token))
                await repository.DeleteMappingAsync(mapping, timeout.Token);
        }
        catch (Exception failure) when (failure is System.Data.Common.DbException or DbUpdateException
            or OperationCanceledException or InvalidOperationException)
        {
            // Keep the reservation for recovery when cleanup cannot establish the outcome.
        }
    }

    private DateTime GetReceiptDate(SupplyFinancialRequest request)
    {
        if (request.DateText == null && request.Date != default)
            return request.Date;
        var text = TextService.ToEnglishNumber(request.DateText?.Trim() ?? "");
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\A[0-9]{4}/[0-9]{2}/[0-9]{2}\z")
            && dates.GetDateMiladi(text) is DateTime date)
            return date;
        throw new InvalidOperationException("تاریخ رسید انبار معتبر نیست.");
    }

    private async Task CompleteAsync(ExternalMapping mapping, int id)
    {
        mapping.ExEntityId = id.ToString(CultureInfo.InvariantCulture);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await repository.SaveMappingAsync(mapping, timeout.Token);
    }

    private async Task ValidateVendorMappingAsync(SupplyFactor source, SupplyFinancialRequest request, CancellationToken ct)
    {
        var mapping = await repository.GetMappingAsync(source.SupplierId, request.ExternalSoftwareId,
            ExternalMappingTypeEnum.تامین_کننده, ct);
        if (mapping == null || !int.TryParse(mapping.ExEntityId, out var vendorId) || vendorId != request.VendorId)
            throw new InvalidOperationException("ارتباط تأمین‌کننده تغییر کرده است. مرحله تطبیق را دوباره انجام دهید.");
    }

    private async Task<int> GetPurchaseIdAsync(SupplyFinancialRequest request, CancellationToken ct)
    {
        var mapping = await repository.GetMappingAsync(request.SupplyFactorId, request.ExternalSoftwareId,
            ExternalMappingTypeEnum.فاکتور_خرید, ct);
        if (mapping == null || IsPending(mapping))
            throw new InvalidOperationException("ابتدا ارتباط فاکتور خرید را در همین مقصد تکمیل کنید.");
        var purchaseId = ReadId(mapping);
        if (request.PurchaseId.HasValue && request.PurchaseId.Value != purchaseId)
            throw new InvalidOperationException("ارتباط فاکتور خرید تغییر کرده است. سوابق را دوباره بررسی کنید.");
        return purchaseId;
    }

    private async Task<AppUser> GetUserAsync() =>
        await users.GetUserAsync(http.HttpContext!.User)
        ?? throw new InvalidOperationException("کاربر ثبت‌کننده پیدا نشد.");

    private static SepidarService.PurchaseItemSelection[] Selections(SupplyFinancialRequest request) =>
        request.Items?.Select(x => x == null ? null :
            new SepidarService.PurchaseItemSelection(x.SupplyFactorItemId, x.ItemId, x.Description)).ToArray() ?? [];

    private static ExternalMappingTypeEnum MapType(bool receipt) =>
        receipt ? ExternalMappingTypeEnum.رسید_انبار : ExternalMappingTypeEnum.فاکتور_خرید;

    private static bool IsPending(ExternalMapping mapping) =>
        mapping?.HasPendingExternalId() == true;

    private static int ReadId(ExternalMapping mapping)
    {
        var value = IsPending(mapping) ? mapping.ExEntityId[ExternalMapping.PendingIdPrefix.Length..] : mapping.ExEntityId;
        return int.TryParse(value, out var id) && id > 0 ? id
            : throw new InvalidOperationException("شناسه سند مالی ذخیره‌شده معتبر نیست.");
    }

    private static ExternalMapping NewMapping(Guid sourceId, Guid destinationId, ExternalMappingTypeEnum type,
        string sourceEntity, string targetEntity, AppUser user) => new() {
        EntityId = sourceId, ExternalSoftwareId = destinationId, MapType = type, MapTypeTxt = type.ToString(),
        EntityName = sourceEntity, ExEntityName = targetEntity, UserCreatorID = user.Id, UserCreator = user.FullName
    };
}
