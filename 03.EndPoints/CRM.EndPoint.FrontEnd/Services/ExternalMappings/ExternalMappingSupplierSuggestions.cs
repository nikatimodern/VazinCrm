using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Sepidar.Parties.Entities;
using CRM.Core.Domain.Sepidar.Purchases.Entities;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings;

public sealed partial class ExternalMappingService
{
    // Derive vendor identity strictly from a mapped or unique proposed purchase invoice.
    // This builds review suggestions only; it never writes an ExternalMapping.
    private async Task<List<MappingReportEntry>> BuildSupplierSuggestionsAsync(
        Guid softwareId, SepidarContextDbBase sepidar,
        IReadOnlyCollection<SupplySuggestionSource> supplyFactors,
        IReadOnlyDictionary<Guid, int> purchaseIdsBySupplyFactor,
        CancellationToken ct)
    {
        var supplierIds = supplyFactors.Select(x => x.SupplierId)
            .Where(x => x != Guid.Empty).Distinct().ToArray();
        if (supplierIds.Length == 0) return [];

        var validSuppliers = new HashSet<Guid>();
        foreach (var batch in supplierIds.Chunk(350))
        {
            var ids = await db.Customers.AsNoTracking()
                .Where(x => batch.Contains(x.Id))
                .Select(x => x.Id).ToListAsync(ct);
            validSuppliers.UnionWith(ids);
        }

        // Confirm persisted/proposed purchase IDs against the selected Sepidar database.
        var purchases = new Dictionary<int, (int VendorDl, int Number)>();
        foreach (var batch in purchaseIdsBySupplyFactor.Values.Where(x => x > 0).Distinct().Chunk(350))
        {
            var rows = await sepidar.InventoryPurchaseInvoices.AsNoTracking()
                .Where(x => batch.Contains(x.InventoryPurchaseInvoiceID) && x.Type == 1)
                .Select(x => new {
                    x.InventoryPurchaseInvoiceID, x.VendorDLRef, x.Number
                }).ToListAsync(ct);
            foreach (var row in rows)
                purchases[row.InventoryPurchaseInvoiceID] = (row.VendorDLRef, row.Number);
        }

        var vendorDls = purchases.Values.Select(x => x.VendorDl)
            .Where(x => x > 0).Distinct().ToArray();
        var partiesByDl = new Dictionary<int, List<int>>();
        foreach (var batch in vendorDls.Chunk(350))
        {
            var rows = await sepidar.Parties.AsNoTracking()
                .Where(x => x.IsVendor && x.Dl != null && batch.Contains(x.DLRef))
                .Select(x => new { x.PartyId, x.DLRef }).ToListAsync(ct);
            foreach (var row in rows)
            {
                if (!partiesByDl.TryGetValue(row.DLRef, out var partyIds))
                    partiesByDl[row.DLRef] = partyIds = [];
                partyIds.Add(row.PartyId);
            }
        }

        var saved = await db.ExternalMappings.AsNoTracking()
            .Where(x => x.ExternalSoftwareId == softwareId
                && x.MapType == ExternalMappingTypeEnum.تامین_کننده)
            .Select(x => new { x.EntityId, x.EntityName, x.ExEntityName, x.ExEntityId })
            .ToListAsync(ct);
        var savedBySupplier = saved
            .Where(x => x.EntityName == nameof(Customer))
            .GroupBy(x => x.EntityId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var savedPartyOwners = saved
            .Where(x => x.ExEntityName == nameof(Party)
                && int.TryParse(x.ExEntityId, out _))
            .GroupBy(x => int.Parse(x.ExEntityId))
            .ToDictionary(x => x.Key, x => x.Select(y => y.EntityId).Distinct().ToHashSet());

        var candidates = new Dictionary<Guid, int[]>();
        var evidence = new Dictionary<Guid, string[]>();
        foreach (var group in supplyFactors
                     .Where(x => validSuppliers.Contains(x.SupplierId))
                     .GroupBy(x => x.SupplierId))
        {
            var documents = new List<string>();
            var partyIds = new HashSet<int>();
            foreach (var source in group)
            {
                if (!purchaseIdsBySupplyFactor.TryGetValue(source.Id, out var purchaseId)
                    || !purchases.TryGetValue(purchaseId, out var purchase))
                    continue;
                if (!partiesByDl.TryGetValue(purchase.VendorDl, out var validParties))
                {
                    documents.Add($"فاکتور تأمین {source.SerialNumber} ← فاکتور خرید سپیدار {purchase.Number} (کد تفصیلی تأمین‌کننده: {purchase.VendorDl}، شخص معتبر پیدا نشد)");
                    continue;
                }
                foreach (var partyId in validParties) partyIds.Add(partyId);
                documents.Add($"فاکتور تأمین {source.SerialNumber} ← فاکتور خرید سپیدار {purchase.Number} (شناسه {purchaseId}، کد تفصیلی: {purchase.VendorDl})");
            }
            candidates[group.Key] = partyIds.OrderBy(x => x).ToArray();
            evidence[group.Key] = documents.Distinct().ToArray();
        }

        // Multiple CRM suppliers must never receive one Sepidar vendor as an automatic suggestion.
        var contestedParties = candidates.Where(x => x.Value.Length == 1
                && !savedBySupplier.ContainsKey(x.Key))
            .GroupBy(x => x.Value[0])
            .Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet();

        var result = new List<MappingReportEntry>(candidates.Count);
        foreach (var (supplierId, partyIds) in candidates)
        {
            var sources = evidence[supplierId];
            if (savedBySupplier.TryGetValue(supplierId, out var savedMappings))
            {
                // Existing links are not overwritten. Contradictory purchase evidence is visible.
                var known = savedMappings.Where(x => x.ExEntityName == nameof(Party)
                        && int.TryParse(x.ExEntityId, out _))
                    .Select(x => int.Parse(x.ExEntityId)).Distinct().ToArray();
                if (sources.Length == 0 || known.Length == 1
                    && partyIds.Length == 1 && known[0] == partyIds[0])
                    continue;
                result.Add(new MappingReportEntry(
                    $"supplier-{supplierId}", nameof(Customer), supplierId,
                    "blocked", "SupplierExistingMappingMismatch",
                    "ارتباط ثبت‌شده تأمین‌کننده را با فاکتورهای خرید بررسی کنید.",
                    nameof(Party), null, sources, (int)ExternalMappingTypeEnum.تامین_کننده,
                    null, partyIds));
                continue;
            }

            var status = "notMatched";
            var reason = "SupplierPurchaseNotMatched";
            int? targetId = null;
            if (partyIds.Length > 1)
            {
                status = "ambiguous";
                reason = "SupplierMultipleParties";
            }
            else if (partyIds.Length == 1)
            {
                var candidate = partyIds[0];
                if (contestedParties.Contains(candidate))
                {
                    status = "ambiguous";
                    reason = "SupplierPartyUsedByMultipleCrmSuppliers";
                }
                else if (savedPartyOwners.TryGetValue(candidate, out var owners)
                         && owners.Any(x => x != supplierId))
                {
                    status = "blocked";
                    reason = "SupplierPartyAlreadyMapped";
                }
                else
                {
                    status = "pending";
                    reason = "ReadyForReview";
                    targetId = candidate;
                }
            }
            else if (sources.Length > 0)
            {
                reason = "SupplierVendorNotFound";
            }

            result.Add(new MappingReportEntry(
                $"supplier-{supplierId}", nameof(Customer), supplierId,
                status, reason, "", nameof(Party), targetId, sources,
                (int)ExternalMappingTypeEnum.تامین_کننده, targetId?.ToString(),
                partyIds));
        }
        return result;
    }
}
