using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;

namespace CRM.EndPoint.FrontEnd.Services;

public sealed partial class SupplyFinancialIntegration
{
    public async Task<SupplyResult> GetSyncAsync(SupplyDestinationKey key, bool receiptOnly, CancellationToken ct)
    {
        var (service, document) = await ResolveSyncAsync(key, receiptOnly, ct);
        var source = await GetSourceAsync(key.SourceId, ct);
        var preview = await maintenance.GetSyncPreviewAsync(document, source, receiptOnly, ct);
        var allowed = preview.Blockers.Count == 0;
        return SupplyResult.Success(new {
            Preview = preview,
            Items = allowed && !receiptOnly ? await service.GetPurchaseItemsAsync(ct) : [],
            AutoVoucher = allowed && preview.ReceiptNumber.HasValue && await service.GetReceiptEditAutoVoucherAsync(ct)
        });
    }

    public async Task<SupplyResult> SynchronizeAsync(SupplyDestinationSyncRequest request, CancellationToken ct)
    {
        if (request?.Key == null || request.Key.SourceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Token) ||
            request.Items == null || (request.ReceiptOnly && request.Items.Count != 0))
            return SupplyResult.Invalid("اطلاعات همگام‌سازی فاکتور تأمین معتبر نیست.");

        await using var guard = await repository.LockAsync(request.Key.SourceId, ct);
        var (service, document) = await ResolveSyncAsync(request.Key, request.ReceiptOnly, ct);
        var source = await GetSourceAsync(request.Key.SourceId, ct);
        var modifier = await sepidarUsers.GetCurrentExternalUserIdAsync(request.Key.SoftwareId, service, ct);
        await maintenance.SynchronizeAsync(document, source, request.ReceiptOnly, request.Token, request.AutoVoucher, modifier,
            PrepareRowsAsync, service.ReserveSupplySyncRowIdsAsync,
            (posting, cancellationToken) => service.PostEditedReceiptAsync(posting, request.AutoVoucher, cancellationToken), ct);
        return SupplyResult.Success(new { Saved = true });

        async Task<IReadOnlyList<InventoryPurchaseInvoiceItem>> PrepareRowsAsync(SupplySyncPreview preview, CancellationToken cancellationToken)
        {
            var descriptions = preview.SourceRows.ToDictionary(row => row.Id);
            var selections = request.Items.Select(row =>
            {
                if (row == null || !descriptions.TryGetValue(row.SupplyFactorItemId, out var candidate))
                    throw new InvalidOperationException("یکی از اقلام انتخاب‌شده در فاکتور تأمین وجود ندارد.");
                return new SepidarService.PurchaseItemSelection(row.SupplyFactorItemId, row.ItemId,
                    candidate.SuggestedItemId == row.ItemId ? candidate.Description : null);
            }).ToArray();
            var draft = await service.PreparePurchaseDraftAsync(source, preview.VendorId, selections, preview.Description, cancellationToken);
            return draft.Items;
        }
    }

    private async Task<(SepidarService Service, SupplyDestinationDocument Document)> ResolveSyncAsync(
        SupplyDestinationKey key, bool receiptOnly, CancellationToken ct)
    {
        if (key == null || receiptOnly != key.ReceiptId.HasValue || (receiptOnly && !key.ReceiptMappingId.HasValue))
            throw new InvalidOperationException("ارتباط فاکتور خرید یا رسید انبار با ERP معتبر نیست.");
        var (service, document, mapping) = await ResolveMaintenanceAsync(key, receiptOnly, ct);
        var receipt = await repository.GetMappingAsync(key.SourceId, key.SoftwareId, MapType(true), ct);
        if (mapping == null || mapping.HasPendingExternalId() || receipt?.HasPendingExternalId() == true)
            throw new InvalidOperationException("ابتدا انتقال مالی در حال انجام را تکمیل کنید.");
        if (receipt != null)
        {
            var receiptKey = key with { ReceiptId = ReadId(receipt), ReceiptMappingId = receipt.Id };
            (_, document, _) = await ResolveMaintenanceAsync(receiptKey, true, ct);
            var saved = await service.FindPurchaseDocumentAsync(true, receiptKey.ReceiptId!.Value, ct);
            if (saved?.BasePurchaseId != document.PurchaseId)
                throw new InvalidOperationException("ارتباط رسید انبار معتبر نیست. سابقه رسید را در سوابق بررسی کنید.");
        }
        return (service, document);
    }

    private async Task EnsureDestinationSyncedAsync(SupplyDestinationDocument document, Guid sourceId, CancellationToken ct)
    {
        var status = await maintenance.GetSyncStatusAsync(document, await GetSourceAsync(sourceId, ct), ct);
        if (status.PurchaseChanged)
            throw new InvalidOperationException("ابتدا فاکتور خرید را همگام‌سازی کنید.");
        if (status.ReceiptChanged)
            throw new InvalidOperationException("ابتدا رسید انبار را همگام‌سازی کنید.");
    }

    private async Task EnsurePurchaseSyncedAsync(SupplyDestinationDocument document, SupplyFactor source, CancellationToken ct)
    {
        if ((await maintenance.GetSyncStatusAsync(document, source, ct)).PurchaseChanged)
            throw new InvalidOperationException("ابتدا فاکتور خرید را همگام‌سازی کنید، سپس رسید انبار را ثبت کنید.");
    }
}
