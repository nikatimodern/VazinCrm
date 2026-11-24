using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings;
public sealed partial class ExternalMappingService
{
    private sealed record SupplySuggestionSource(
        Guid Id, Guid SupplierId, string SerialNumber, string Date, DateTime DateMiladi,
        decimal PaymentAmount);

    private sealed record SupplyPurchaseCandidate(
        int Id, int Number, int FiscalYearRef, int VendorDlRef, decimal? Amount);

    private sealed record SupplyReceiptCandidate(int Id, int BasePurchaseId);

    // Only creates review rows. No records are inserted into CRM or Sepidar.
    private async Task<List<MappingReportEntry>> BuildSupplyFactorSuggestionsAsync(
        Guid softwareId, CancellationToken ct)
    {
        var context = softwareId == Sepidar1SoftwareId
            ? (SepidarContextDbBase)sdb : (SepidarContextDbBase)s2Db;
        var sources = await db.SupplyFactors.AsNoTracking()
            .Select(x => new SupplySuggestionSource(
                x.Id, x.SupplierId, x.SerialNumber, x.Date, x.DateMiladi, x.PaymentAmount))
            .ToListAsync(ct);
        if (sources.Count == 0) return [];

        var mappings = await db.ExternalMappings.AsNoTracking()
            .Where(x => x.ExternalSoftwareId == softwareId &&
                (x.MapType == ExternalMappingTypeEnum.تامین_کننده
                 || x.MapType == ExternalMappingTypeEnum.فاکتور_خرید
                 || x.MapType == ExternalMappingTypeEnum.رسید_انبار))
            .Select(x => new { x.EntityId, x.EntityName, x.MapType, x.ExEntityName, x.ExEntityId })
            .ToListAsync(ct);
        var mappedPurchases = mappings.Where(x =>
                x.MapType == ExternalMappingTypeEnum.فاکتور_خرید
                && x.EntityName == nameof(SupplyFactor))
            .GroupBy(x => x.EntityId).ToDictionary(x => x.Key, x => x.ToArray());
        var mappedReceipts = mappings.Where(x =>
                x.MapType == ExternalMappingTypeEnum.رسید_انبار
                && x.EntityName == nameof(SupplyFactor))
            .Select(x => x.EntityId).ToHashSet();
        var usedPurchases = mappings.Where(x =>
                x.MapType == ExternalMappingTypeEnum.فاکتور_خرید
                && x.ExEntityName == nameof(InventoryPurchaseInvoice))
            .Select(x => x.ExEntityId).ToHashSet(StringComparer.Ordinal);
        var usedReceipts = mappings.Where(x =>
                x.MapType == ExternalMappingTypeEnum.رسید_انبار
                && x.ExEntityName == nameof(InventoryReceipt))
            .Select(x => x.ExEntityId).ToHashSet(StringComparer.Ordinal);

        var supplierPartyIds = mappings.Where(x =>
                x.MapType == ExternalMappingTypeEnum.تامین_کننده
                && x.ExEntityName == "Party" && int.TryParse(x.ExEntityId, out _))
            .Select(x => int.Parse(x.ExEntityId)).Distinct().ToArray();
        var supplierDlByParty = new Dictionary<int, int>();
        foreach (var batch in supplierPartyIds.Chunk(350))
        {
            var parties = await context.Parties.AsNoTracking()
                .Where(x => batch.Contains(x.PartyId) && x.IsVendor && x.Dl != null)
                .Select(x => new { x.PartyId, x.DLRef }).ToListAsync(ct);
            foreach (var party in parties)
                supplierDlByParty[party.PartyId] = party.DLRef;
        }
        var supplierDls = new Dictionary<Guid, int>();
        foreach (var mapping in mappings.Where(x =>
                     x.MapType == ExternalMappingTypeEnum.تامین_کننده
                     && x.ExEntityName == "Party"
                     && x.EntityName == "Customer"))
            if (int.TryParse(mapping.ExEntityId, out var partyId)
                && supplierDlByParty.TryGetValue(partyId, out var dl))
                supplierDls[mapping.EntityId] = dl;

        var fiscalRows = await context.FiscalYears.AsNoTracking()
            .Select(x => new { x.FiscalYearId, x.Title, x.StartDate, x.EndDate })
            .ToListAsync(ct);
        var fiscalYears = fiscalRows
            .Where(x => GetLegacyFiscalYear(x.Title).HasValue)
            .ToDictionary(x => x.FiscalYearId,
                x => new LegacyFiscalYearRow(GetLegacyFiscalYear(x.Title)!.Value, x.StartDate, x.EndDate));

        static int? SupplierInvoiceNumber(string value)
        {
            var normalized = TextService.ToEnglishNumber(value ?? "").Trim();
            return int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                && number > 0 ? number : null;
        }

        var invoiceNumbers = sources.Select(x => SupplierInvoiceNumber(x.SerialNumber))
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var purchasesByNumber = new Dictionary<int, List<SupplyPurchaseCandidate>>();
        foreach (var batch in invoiceNumbers.Chunk(350))
        {
            var rows = await context.InventoryPurchaseInvoices.AsNoTracking()
                .Where(x => x.Type == 1 && x.InvoiceNumber.HasValue
                    && batch.Contains(x.InvoiceNumber.Value))
                .Select(x => new {
                    x.InventoryPurchaseInvoiceID, x.InvoiceNumber, x.Number, x.FiscalYearRef,
                    x.VendorDLRef, x.TotalNetPriceInBaseCurrency, x.TotalNetPrice
                }).ToListAsync(ct);
            foreach (var x in rows)
            {
                var number = x.InvoiceNumber!.Value;
                if (!purchasesByNumber.TryGetValue(number, out var list))
                    purchasesByNumber[number] = list = [];
                list.Add(new SupplyPurchaseCandidate(x.InventoryPurchaseInvoiceID,
                    x.Number, x.FiscalYearRef, x.VendorDLRef,
                    x.TotalNetPriceInBaseCurrency ?? x.TotalNetPrice));
            }
        }

        var matchesBySource = new Dictionary<Guid, List<SupplyPurchaseCandidate>>();
        foreach (var source in sources)
        {
            var number = SupplierInvoiceNumber(source.SerialNumber);
            var possible = number.HasValue
                ? purchasesByNumber.GetValueOrDefault(number.Value) ?? []
                : [];
            matchesBySource[source.Id] = possible.Where(purchase =>
                purchase.Amount == source.PaymentAmount
                && fiscalYears.TryGetValue(purchase.FiscalYearRef, out var year)
                && IsInLegacyFiscalYear(source.Date, source.DateMiladi, year)
                && (!supplierDls.TryGetValue(source.SupplierId, out var dl)
                    || purchase.VendorDlRef == dl)).ToList();
        }
        // Never recommend one purchase invoice to several CRM supply factors automatically.
        var contestedIds = matchesBySource
            .Where(x => x.Value.Count == 1 && !mappedPurchases.ContainsKey(x.Key))
            .GroupBy(x => x.Value[0].Id)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet();

        var result = new List<MappingReportEntry>();
        var linkedPurchaseIds = new Dictionary<Guid, int>();
        var prospectivePurchases = new HashSet<Guid>();
        void Add(SupplySuggestionSource source, ExternalMappingTypeEnum type,
            string status, string reason, string targetEntity, int? targetId,
            IEnumerable<int> options, params string[] notes)
        {
            result.Add(new MappingReportEntry(
                $"supply-{(int)type}-{source.Id}", nameof(SupplyFactor), source.Id,
                status, reason, "", targetEntity, targetId, notes, (int)type,
                targetId?.ToString(), options.Distinct().Where(x => x > 0).ToArray()));
        }

        foreach (var source in sources)
        {
            if (mappedPurchases.TryGetValue(source.Id, out var existing))
            {
                if (existing.Length == 1
                    && existing[0].ExEntityName == nameof(InventoryPurchaseInvoice)
                    && int.TryParse(existing[0].ExEntityId, out var mappedId) && mappedId > 0)
                    linkedPurchaseIds[source.Id] = mappedId;
                continue;
            }

            var candidates = matchesBySource[source.Id];
            var number = SupplierInvoiceNumber(source.SerialNumber);
            var note = $"شماره تأمین‌کننده: {source.SerialNumber} | مبلغ: {source.PaymentAmount:N0}";
            if (!number.HasValue)
            {
                Add(source, ExternalMappingTypeEnum.فاکتور_خرید, "notMatched",
                    "SupplyInvoiceNumberNotNumeric", nameof(InventoryPurchaseInvoice),
                    null, [], note);
                continue;
            }
            if (candidates.Count == 0)
            {
                var near = purchasesByNumber.GetValueOrDefault(number.Value) ?? [];
                Add(source, ExternalMappingTypeEnum.فاکتور_خرید, "notMatched",
                    "SupplyPurchaseNoMatch", nameof(InventoryPurchaseInvoice),
                    null, near.Select(x => x.Id), note);
                continue;
            }
            if (candidates.Count > 1 || contestedIds.Contains(candidates[0].Id))
            {
                Add(source, ExternalMappingTypeEnum.فاکتور_خرید, "ambiguous",
                    "SupplyPurchaseAmbiguous", nameof(InventoryPurchaseInvoice),
                    null, candidates.Select(x => x.Id), note);
                continue;
            }
            var purchaseId = candidates[0].Id;
            if (usedPurchases.Contains(purchaseId.ToString()))
            {
                Add(source, ExternalMappingTypeEnum.فاکتور_خرید, "blocked",
                    "SupplyPurchaseAlreadyMapped", nameof(InventoryPurchaseInvoice),
                    purchaseId, [purchaseId], note);
                continue;
            }
            linkedPurchaseIds[source.Id] = purchaseId;
            prospectivePurchases.Add(source.Id);
            Add(source, ExternalMappingTypeEnum.فاکتور_خرید, "pending",
                "ReadyForReview", nameof(InventoryPurchaseInvoice),
                purchaseId, [purchaseId], note,
                supplierDls.ContainsKey(source.SupplierId)
                    ? "تأمین‌کننده، مبلغ، سال مالی و شماره فاکتور تطبیق دارند."
                    : "شماره، مبلغ و سال مالی تطبیق دارند؛ ارتباط تأمین‌کننده هنوز تأیید نشده است.");
        }

        var receiptsByPurchase = new Dictionary<int, List<SupplyReceiptCandidate>>();
        foreach (var batch in linkedPurchaseIds.Values.Distinct().Chunk(350))
        {
            var rows = await context.InventoryReceipts.AsNoTracking()
                .Where(x => !x.IsReturn && x.BasePurchaseInvoiceRef.HasValue
                    && batch.Contains(x.BasePurchaseInvoiceRef.Value))
                .Select(x => new {
                    x.InventoryReceiptID, x.BasePurchaseInvoiceRef
                }).ToListAsync(ct);
            foreach (var row in rows)
            {
                var purchaseId = row.BasePurchaseInvoiceRef!.Value;
                if (!receiptsByPurchase.TryGetValue(purchaseId, out var list))
                    receiptsByPurchase[purchaseId] = list = [];
                list.Add(new SupplyReceiptCandidate(row.InventoryReceiptID, purchaseId));
            }
        }

        foreach (var source in sources.Where(x => !mappedReceipts.Contains(x.Id)))
        {
            if (!linkedPurchaseIds.TryGetValue(source.Id, out var purchaseId))
            {
                Add(source, ExternalMappingTypeEnum.رسید_انبار, "blocked",
                    "SupplyReceiptPurchaseMissing", nameof(InventoryReceipt), null, [],
                    "ابتدا معادل فاکتور خرید این فاکتور تأمین را مشخص کنید.");
                continue;
            }
            var relatedReceipts = receiptsByPurchase.GetValueOrDefault(purchaseId) ?? [];
            var receipts = relatedReceipts
                .Where(x => !usedReceipts.Contains(x.Id.ToString())).ToList();
            if (relatedReceipts.Count > 0 && receipts.Count == 0)
            {
                Add(source, ExternalMappingTypeEnum.رسید_انبار, "blocked",
                    "SupplyReceiptAlreadyMapped", nameof(InventoryReceipt), null,
                    relatedReceipts.Select(x => x.Id),
                    $"رسیدهای فاکتور خرید با شناسه {purchaseId} قبلاً ارتباط داده شده‌اند.");
                continue;
            }
            if (receipts.Count == 0)
            {
                Add(source, ExternalMappingTypeEnum.رسید_انبار, "notMatched",
                    "SupplyReceiptNotFound", nameof(InventoryReceipt), null, [],
                    $"برای فاکتور خرید شماره شناسه {purchaseId} رسید انبار ثبت نشده است.");
                continue;
            }
            var isProposedPurchase = prospectivePurchases.Contains(source.Id);
            var reason = isProposedPurchase ? "SupplyReceiptPurchasePending" : "ReadyForReview";
            if (receipts.Count == 1)
            {
                Add(source, ExternalMappingTypeEnum.رسید_انبار, "pending",
                    reason, nameof(InventoryReceipt), receipts[0].Id, [receipts[0].Id],
                    $"رسید به فاکتور خرید با شناسه {purchaseId} متصل است.",
                    isProposedPurchase ? "هنگام ثبت گروهی، ابتدا ارتباط فاکتور خرید ثبت می‌شود." : "");
            }
            else
            {
                Add(source, ExternalMappingTypeEnum.رسید_انبار, "ambiguous",
                    "SupplyReceiptAmbiguous", nameof(InventoryReceipt), null,
                    receipts.Select(x => x.Id),
                    $"برای فاکتور خرید با شناسه {purchaseId} چند رسید انبار وجود دارد.");
            }
        }

        result.AddRange(await BuildSupplierSuggestionsAsync(
            softwareId, context, sources, linkedPurchaseIds, ct));
        return result;
    }
}
