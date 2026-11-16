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

namespace CRM.EndPoint.FrontEnd.Services;

public sealed class SupplyFinancialIntegration(ISupplyFinancialRepository repository,
    Sepidar1Service first, Sepidar2Service second, UserManager<AppUser> users,
    IHttpContextAccessor http) : ISupplyFinancialIntegration
{
    private const string PendingPrefix = "pending:";
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

    public async Task<SupplyResult> GetHistoryAsync(Guid sourceId, bool receipt, CancellationToken ct)
    {
        var source = await GetSourceAsync(sourceId, ct);
        var maps = await repository.GetMappingsAsync(sourceId, ct);
        var results = new List<object>();
        foreach (var software in (await repository.GetDestinationsAsync(ct)).Where(x => x.Id == FirstId || x.Id == SecondId))
        {
            var purchase = maps.SingleOrDefault(x => x.ExternalSoftwareId == software.Id
                && x.MapType == ExternalMappingTypeEnum.فاکتور_خرید);
            var mapping = receipt ? maps.SingleOrDefault(x => x.ExternalSoftwareId == software.Id
                && x.MapType == ExternalMappingTypeEnum.رسید_انبار) : purchase;
            var service = Resolve(software.Id);
            var document = mapping != null
                ? await service.FindPurchaseDocumentAsync(receipt, ReadId(mapping), ct) : null;
            var purchaseDocument = receipt && purchase != null
                ? await service.FindPurchaseDocumentAsync(false, ReadId(purchase), ct) : null;
            var existingReceipt = receipt && document == null && purchaseDocument != null
                ? await service.FindReceiptForPurchaseAsync(purchaseDocument.Id, ct) : null;
            var recorded = document ?? existingReceipt;
            var missing = mapping != null && document == null && !IsPending(mapping);
            var needsRecovery = document != null && IsPending(mapping);
            results.Add(new {
                software.Id, software.Title,
                Document = recorded,
                DateText = recorded == null ? null : BaseServices.GetDatePersian(recorded.Date),
                HasMapping = mapping != null,
                Pending = IsPending(mapping) && existingReceipt == null,
                Missing = missing,
                IsDisabled = (!needsRecovery && recorded != null) || missing
                    || (receipt && (purchaseDocument == null || IsPending(purchase)))
            });
        }
        return SupplyResult.Success(new { source.SerialNumber, Softwares = results });
    }

    public async Task<SupplyResult> PrepareAsync(SupplyFinancialRequest request, CancellationToken ct)
    {
        var service = await GetDestinationAsync(request.ExternalSoftwareId, ct);
        var source = await GetSourceAsync(request.SupplyFactorId, ct);
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
            var selected = vendor != null && int.TryParse(vendor.ExEntityId, out var vendorId)
                ? (await service.SearchPurchaseVendorsAsync(null, vendorId, ct)).SingleOrDefault() : null;
            return SupplyResult.Success(new {
                source.SerialNumber, source.SupplierTxt, source.Description, Vendor = selected,
                Items = source.Items.Select(x => new { x.Id, x.Product, x.UnitTxt, x.WeightCount, x.UnitAmount, x.TotalAmount })
            });
        }

        var purchaseId = await GetPurchaseIdAsync(source.Id, request.ExternalSoftwareId, ct);
        var receiptSource = await service.GetFullPurchaseReceiptSourceAsync(purchaseId, ct);
        var stocks = await service.GetPurchaseReceiptStocksAsync(purchaseId, ct);
        var labels = await service.GetPurchaseItemLabelsAsync(receiptSource.Items.Select(x => x.ItemRef).ToArray(), ct);
        return SupplyResult.Success(new {
            Number = receiptSource.Invoice.Number, Date = receiptSource.Invoice.Date.ToString("yyyy-MM-dd"),
            Stocks = stocks, source.SupplierTxt, source.SerialNumber,
            Items = receiptSource.Items.Select(x => new {
                Id = x.InventoryPurchaseInvoiceItemID, Product = labels.GetValueOrDefault(x.ItemRef),
                WeightCount = x.Quantity, TotalAmount = x.NetPrice, x.Description
            })
        });
    }

    public async Task<SupplyResult> SearchAsync(SupplyFinancialRequest request, bool items, CancellationToken ct)
    {
        var service = await GetDestinationAsync(request.ExternalSoftwareId, ct);
        var search = (request.Search ?? "").Trim();
        if (search.Length > 100)
            return SupplyResult.Invalid("عبارت جست‌وجو بیش از حد طولانی است.");
        return SupplyResult.Success(items
            ? await service.SearchPurchaseItemsAsync(search, ct)
            : await service.SearchPurchaseVendorsAsync(search, null, ct));
    }

    public async Task<SupplyResult> SaveVendorAsync(SupplyFinancialRequest request, CancellationToken ct)
    {
        var service = await GetDestinationAsync(request.ExternalSoftwareId, ct);
        var source = await GetSourceAsync(request.SupplyFactorId, ct);
        var vendor = (await service.SearchPurchaseVendorsAsync(null, request.VendorId, ct)).SingleOrDefault();
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
            var purchaseId = await GetPurchaseIdAsync(source.Id, request.ExternalSoftwareId, ct);
            var draft = await service.PreparePurchaseReceiptDraftAsync(purchaseId, request.StockId,
                request.Date, request.Description, request.RowDescriptions, ct);
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
        var vendor = (await service.SearchPurchaseVendorsAsync(null, request.VendorId, ct)).Single();
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
            mapping.ExEntityId = PendingPrefix + reservedId.ToString(CultureInfo.InvariantCulture);
            await repository.SaveMappingAsync(mapping, ct);
        }

        var id = ReadId(mapping);
        var document = request.Receipt
            ? await service.AddSupplyReceiptAsync(id, creator,
                await GetPurchaseIdAsync(source.Id, request.ExternalSoftwareId, ct), request.StockId,
                request.Date, request.Description, request.RowDescriptions, request.PreviewToken, ct)
            : await service.AddSupplyPurchaseAsync(id, creator, source, request.VendorId, Selections(request),
                request.Description, request.PreviewToken, ct);
        await CompleteAsync(mapping, document.Id);
        return SupplyResult.Success(document);
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

    private async Task<int> GetPurchaseIdAsync(Guid sourceId, Guid externalSoftwareId, CancellationToken ct)
    {
        var mapping = await repository.GetMappingAsync(sourceId, externalSoftwareId, ExternalMappingTypeEnum.فاکتور_خرید, ct);
        if (mapping == null || IsPending(mapping))
            throw new InvalidOperationException("ابتدا ارتباط فاکتور خرید را در همین مقصد تکمیل کنید.");
        return ReadId(mapping);
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
        mapping?.ExEntityId?.StartsWith(PendingPrefix, StringComparison.Ordinal) == true;

    private static int ReadId(ExternalMapping mapping)
    {
        var value = IsPending(mapping) ? mapping.ExEntityId[PendingPrefix.Length..] : mapping.ExEntityId;
        return int.TryParse(value, out var id) && id > 0 ? id
            : throw new InvalidOperationException("شناسه سند مالی ذخیره‌شده معتبر نیست.");
    }

    private static ExternalMapping NewMapping(Guid sourceId, Guid destinationId, ExternalMappingTypeEnum type,
        string sourceEntity, string targetEntity, AppUser user) => new() {
        EntityId = sourceId, ExternalSoftwareId = destinationId, MapType = type, MapTypeTxt = type.ToString(),
        EntityName = sourceEntity, ExEntityName = targetEntity, UserCreatorID = user.Id, UserCreator = user.FullName
    };
}
