using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.Framework;

namespace CRM.EndPoint.FrontEnd.Services;

public sealed partial class SupplyFinancialIntegration
{
    public async Task<SupplyResult> GetReceiptEditAsync(SupplyDestinationKey key, CancellationToken ct)
    {
        var (service, document, _) = await ResolveMaintenanceAsync(key, true, ct);
        await EnsureDestinationSyncedAsync(document, key.SourceId, ct);
        var state = await maintenance.GetReceiptEditAsync(document, ct);
        var itemIds = state.Rows.Select(row => row.ItemId).Distinct().ToArray();
        var items = await service.GetReceiptItemDisplaysAsync(itemIds, ct);
        return SupplyResult.Success(new {
            state.Impact, state.StockId, state.StockTitle, state.Description,
            DateText = BaseServices.GetDatePersian(state.Date),
            Stocks = await service.GetReceiptStocksForItemsAsync(itemIds, ct),
            AutoVoucher = await service.GetReceiptEditAutoVoucherAsync(ct),
            Rows = state.Rows.Select(row => new {
                row.Id, row.Quantity, row.Description, ItemLabel = items.GetValueOrDefault(row.ItemId)?.Label,
                Unit = items.GetValueOrDefault(row.ItemId)?.UnitTitle
            })
        });
    }

    public async Task<SupplyResult> EditReceiptAsync(SupplyReceiptEditRequest request, CancellationToken ct)
    {
        if (request?.Key == null || request.Key.SourceId == Guid.Empty || request.Key.ReceiptId is not > 0 ||
            request.StockId <= 0 || string.IsNullOrWhiteSpace(request.Token) || request.RowDescriptions == null)
            return SupplyResult.Invalid("اطلاعات ویرایش رسید انبار معتبر نیست.");

        var date = GetReceiptDate(new SupplyFinancialRequest { DateText = request.DateText });
        await using var guard = await repository.LockAsync(request.Key.SourceId, ct);
        var (service, document, _) = await ResolveMaintenanceAsync(request.Key, true, ct);
        await EnsureDestinationSyncedAsync(document, request.Key.SourceId, ct);
        var modifier = await sepidarUsers.GetCurrentExternalUserIdAsync(request.Key.SoftwareId, service, ct);
        await maintenance.EditReceiptAsync(document, request.Token, modifier,
            new(date, request.StockId, request.Description, request.RowDescriptions),
            (posting, token) => service.PostEditedReceiptAsync(posting, request.AutoVoucher, token), ct);
        return SupplyResult.Success(new { Saved = true });
    }

    public async Task<SupplyResult> GetPurchaseEditAsync(SupplyDestinationKey key, CancellationToken ct)
    {
        var (service, document, _) = await ResolveMaintenanceAsync(key, false, ct);
        await EnsureDestinationSyncedAsync(document, key.SourceId, ct);
        var state = await maintenance.GetPurchaseAsync(document, ct);
        var canChangeLookups = state.Restriction == null && !state.HasLinkedReceipt;
        var vendors = canChangeLookups ? await service.GetPurchaseVendorsAsync(null, ct) : [];
        var items = canChangeLookups ? await service.GetPurchaseItemsAsync(ct) : [];
        var current = await service.GetReceiptItemDisplaysAsync(state.Rows.Select(x => x.ItemId).Distinct().ToArray(), ct);
        return SupplyResult.Success(new {
            state.Number, state.Token, state.VendorId, state.VendorName, state.Description, state.Restriction, state.HasLinkedReceipt,
            DateText = BaseServices.GetDatePersian(state.Date), Vendors = vendors, Items = items,
            Rows = state.Rows.Select(row => new {
                row.RowNumber, row.ItemId, row.Quantity, row.Fee, row.Description,
                ItemLabel = current.GetValueOrDefault(row.ItemId)?.Label,
                Unit = current.GetValueOrDefault(row.ItemId)?.UnitTitle
            })
        });
    }

    public async Task<SupplyResult> EditPurchaseAsync(SupplyDestinationEditRequest request, CancellationToken ct)
    {
        if (request?.Key == null || request.Key.SourceId == Guid.Empty || request.Items == null || request.VendorId is <= 0 ||
            request.Description?.Length > 400 || request.Items.Any(x => x == null || x.ItemId is <= 0 || x.Description?.Length > 500) ||
            request.Items.Select(x => x.RowNumber).Distinct().Count() != request.Items.Count)
            return SupplyResult.Invalid("اطلاعات ویرایش فاکتور خرید معتبر نیست.");

        await using var guard = await repository.LockAsync(request.Key.SourceId, ct);
        var (service, document, _) = await ResolveMaintenanceAsync(request.Key, false, ct);
        await EnsureDestinationSyncedAsync(document, request.Key.SourceId, ct);
        var state = await maintenance.GetPurchaseAsync(document, ct);
        if (state.Restriction != null)
            return SupplyResult.Invalid(state.Restriction);
        if (state.Token != request.Token || state.Rows.Count == 0 || state.Rows.Count != request.Items.Count)
            return SupplyResult.Invalid("اطلاعات فاکتور تغییر کرده است. فرم ویرایش را دوباره باز کنید.");

        var submitted = request.Items.ToDictionary(x => x.RowNumber);
        if (state.Rows.Any(row => !submitted.ContainsKey(row.RowNumber)))
            return SupplyResult.Invalid("ردیف‌های فاکتور خرید معتبر نیست.");
        if (!state.HasLinkedReceipt && (!request.VendorId.HasValue || request.Items.Any(row => !row.ItemId.HasValue)))
            return SupplyResult.Invalid("تأمین‌کننده و کالای معادل همه ردیف‌ها را مشخص کنید.");
        var modifier = await sepidarUsers.GetCurrentExternalUserIdAsync(request.Key.SoftwareId, service, ct);
        var rows = state.Rows.Select(row => row with {
            ItemId = submitted[row.RowNumber].ItemId ?? row.ItemId, Description = submitted[row.RowNumber].Description
        }).ToArray();
        await maintenance.EditPurchaseAsync(document, request.Token, modifier,
            new(request.VendorId, request.Description, rows), ct);
        return SupplyResult.Success(new { Saved = true });
    }

    public async Task<SupplyResult> PreviewDestinationDeletionAsync(SupplyDestinationKey key, CancellationToken ct)
    {
        var (_, document, _) = await ResolveMaintenanceAsync(key, true, ct);
        return SupplyResult.Success(await maintenance.PreviewDeletionAsync(document, ct));
    }

    public async Task<SupplyResult> DeleteDestinationAsync(SupplyDestinationKey key, string token, CancellationToken ct)
    {
        if (key == null || key.SourceId == Guid.Empty || string.IsNullOrEmpty(token))
            return SupplyResult.Invalid("اطلاعات حذف سند معتبر نیست.");

        await using var guard = await repository.LockAsync(key.SourceId, ct);
        var (_, document, mapping) = await ResolveMaintenanceAsync(key, true, ct);
        await maintenance.DeleteAsync(document, token, ct);
        if (mapping != null)
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await repository.DeleteDestinationMappingAsync(mapping, cleanup.Token);
        }
        return SupplyResult.Success(new { Deleted = true });
    }

    private async Task<(SepidarService Service, SupplyDestinationDocument Document, ExternalMapping Mapping)>
        ResolveMaintenanceAsync(SupplyDestinationKey key, bool allowReceipt, CancellationToken ct)
    {
        if (key == null || key.SourceId == Guid.Empty || key.PurchaseMappingId == Guid.Empty ||
            (!allowReceipt && key.ReceiptId.HasValue) || key.ReceiptId is <= 0 ||
            (key.ReceiptMappingId.HasValue && !key.ReceiptId.HasValue))
            throw new InvalidOperationException("سابقه ارسال معتبر نیست.");

        var service = await GetDestinationAsync(key.SoftwareId, ct);
        var purchase = await repository.GetMappingAsync(key.SourceId, key.SoftwareId, MapType(false), ct);
        if (purchase == null || purchase.Id != key.PurchaseMappingId || purchase.ExEntityName != nameof(InventoryPurchaseInvoice))
            throw new InvalidOperationException("ارتباط فاکتور خرید با نرم‌افزار مقصد پیدا نشد.");

        var purchaseId = ReadId(purchase);
        if (await repository.HasOtherDocumentMappingAsync(key.SoftwareId, purchase.ExEntityName, purchaseId, purchase.Id, ct))
            throw new InvalidOperationException("فاکتور خرید در بیش از یک سابقهٔ ERP استفاده شده است.");

        var document = new SupplyDestinationDocument(key.SoftwareId, purchaseId, key.ReceiptId);
        if (key.ReceiptId is not int receiptId)
            return (service, document, purchase);

        var receipt = await repository.GetMappingAsync(key.SourceId, key.SoftwareId, MapType(true), ct);
        var matchingReceipt = receipt?.ExEntityName == nameof(InventoryReceipt) && ReadId(receipt) == receiptId ? receipt : null;
        if (matchingReceipt?.Id != key.ReceiptMappingId ||
            await repository.HasOtherDocumentMappingAsync(key.SoftwareId, nameof(InventoryReceipt), receiptId, matchingReceipt?.Id, ct))
            throw new InvalidOperationException("ارتباط رسید انبار با نرم‌افزار مقصد تغییر کرده است.");
        return (service, document, matchingReceipt);
    }
}
