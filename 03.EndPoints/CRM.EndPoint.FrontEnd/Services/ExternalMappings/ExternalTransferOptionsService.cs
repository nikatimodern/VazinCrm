using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.Core.Domain.Supplies.Entities;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings;

/// <summary>
/// Discovers destinations for any configured transfer type. A missing external document
/// does not delete its CRM mapping; history views handle explicit user deletion.
/// Cash is the temporary exception until it has a financial-history UI.
/// </summary>
public sealed class ExternalTransferOptionsService(
    CRMContextDb db, Sepidar1Service first, Sepidar2Service second)
{
    private static readonly Guid FirstSoftwareId =
        Guid.Parse("2f3d8024-6c2c-491f-c921-08df26b00138");
    private static readonly Guid SecondSoftwareId =
        Guid.Parse("d6c3e8a7-1435-47d5-0489-08df26c5e2ca");

    public sealed record TransferRule(
        string SourceEntity,
        string[] AllowedRoles = null,
        bool SupportedDestinationsOnly = false,
        ExternalMappingTypeEnum? Prerequisite = null,
        string PrerequisiteMessage = null,
        Func<SepidarService, int, CancellationToken, Task<bool>> HasRelatedDocument = null,
        bool CleanupMissingCashDocuments = false);

    public sealed record DestinationOption(
        Guid Id, string Title, bool IsDisabled, bool IsAlreadySent,
        bool PrerequisiteMissing, string PrerequisiteMessage, string DisabledReason);

    // Adding a transfer type requires one configuration entry, not another controller branch.
    // Never enable automatic cleanup for a type that has a manual history/delete UI.
    public static IReadOnlyDictionary<ExternalMappingTypeEnum, TransferRule> Rules { get; } =
        new Dictionary<ExternalMappingTypeEnum, TransferRule> {
            [ExternalMappingTypeEnum.تنخواه] =
                new(nameof(Cash), CleanupMissingCashDocuments: true),
            [ExternalMappingTypeEnum.پیش_فاکتور] = new(nameof(PreFactor)),
            [ExternalMappingTypeEnum.فاکتور] = new(nameof(Factor)),
            [ExternalMappingTypeEnum.فاکتور_خرید] = new(nameof(SupplyFactor),
                AllowedRoles: ["مدیر ارشد", "مدیر مالی", "کارشناس مالی"],
                SupportedDestinationsOnly: true),
            [ExternalMappingTypeEnum.رسید_انبار] = new(nameof(SupplyFactor),
                AllowedRoles: ["مدیر ارشد", "مدیر مالی", "کارشناس مالی"],
                SupportedDestinationsOnly: true,
                Prerequisite: ExternalMappingTypeEnum.فاکتور_خرید,
                PrerequisiteMessage: "برای ثبت رسید انبار، ابتدا فاکتور خرید را در نرم‌افزار مالی ثبت کنید.",
                HasRelatedDocument: async (service, purchaseId, ct) =>
                    await service.FindReceiptForPurchaseAsync(purchaseId, ct) != null)
        };

    public bool TryGetRule(ExternalMappingTypeEnum mapType, out TransferRule rule) =>
        Rules.TryGetValue(mapType, out rule);

    public async Task<IReadOnlyList<DestinationOption>> GetAllExternalSoftwareAsync(
        IReadOnlyCollection<Guid> sourceIds, ExternalMappingTypeEnum mapType, CancellationToken ct)
    {
        if (!TryGetRule(mapType, out var rule))
            throw new ArgumentOutOfRangeException(nameof(mapType));

        var ids = sourceIds.Distinct().ToArray();
        var prerequisiteEntity = rule.Prerequisite is { } prerequisite
            ? Rules[prerequisite].SourceEntity : null;

        var mappings = await db.ExternalMappings.AsNoTracking()
            .Where(x => ids.Contains(x.EntityId) &&
                ((x.EntityName == rule.SourceEntity && x.MapType == mapType) ||
                 (x.EntityName == prerequisiteEntity && x.MapType == rule.Prerequisite)))
            .ToListAsync(ct);

        // A single external record may be linked to several CRM entities.
        // Cache existence checks per destination/record to avoid repeated lookups.
        var existence = new Dictionary<Guid, bool?>();
        var checkedRecords = new Dictionary<(Guid SoftwareId, string EntityName, string EntityId), bool?>();
        foreach (var mapping in mappings.Where(x => !x.HasPendingExternalId()))
        {
            var service = Resolve(mapping.ExternalSoftwareId);
            if (service == null)
            {
                existence[mapping.Id] = null;
                continue;
            }

            var key = (mapping.ExternalSoftwareId, mapping.ExEntityName, mapping.ExEntityId);
            if (!checkedRecords.TryGetValue(key, out var found))
            {
                found = await service.EntityExistsAsync(mapping.ExEntityName, mapping.ExEntityId);
                checkedRecords[key] = found;
            }
            existence[mapping.Id] = found;
        }

        // Temporary legacy exception: Cash does not have its own missing-history action.
        // Only PaymentHeader/Voucher mappings proven missing are removed automatically.
        if (rule.CleanupMissingCashDocuments)
        {
            var missingCash = mappings.Where(m =>
                m.MapType == ExternalMappingTypeEnum.تنخواه &&
                m.EntityName == nameof(Cash) &&
                (m.ExEntityName == nameof(PaymentHeader) || m.ExEntityName == nameof(Voucher)) &&
                existence.GetValueOrDefault(m.Id) == false).ToArray();
            if (missingCash.Length > 0)
            {
                db.ExternalMappings.RemoveRange(missingCash);
                await db.SaveChangesAsync(ct);
                var removedIds = missingCash.Select(m => m.Id).ToHashSet();
                mappings.RemoveAll(m => removedIds.Contains(m.Id));
            }
        }

        var mappingLookup = mappings.ToLookup(m => (m.ExternalSoftwareId, m.EntityId, m.MapType));
        var softwares = await db.ExternalSoftwares.AsNoTracking()
            .OrderBy(s => s.Title)
            .Select(s => new { s.Id, s.Title })
            .ToListAsync(ct);

        var options = new List<DestinationOption>(softwares.Count);
        foreach (var software in softwares)
        {
            var service = Resolve(software.Id);
            if (rule.SupportedDestinationsOnly && service == null)
                continue;

            var eligible = 0;
            var registered = 0;
            var missing = 0;
            var pending = 0;

            foreach (var sourceId in ids)
            {
                ExternalMapping dependency = null;
                if (rule.Prerequisite is { } requiredType)
                {
                    dependency = mappingLookup[(software.Id, sourceId, requiredType)]
                        .FirstOrDefault(m => existence.GetValueOrDefault(m.Id) == true);
                    if (dependency == null)
                        continue;
                }
                eligible++;

                var saved = mappingLookup[(software.Id, sourceId, mapType)].ToArray();
                if (saved.Any(m => m.HasPendingExternalId()))
                {
                    pending++;
                    continue;
                }

                if (saved.Any(m => existence.GetValueOrDefault(m.Id) != false))
                    registered++;
                else if (saved.Length > 0)
                    missing++;
                else if (dependency != null && rule.HasRelatedDocument != null &&
                         service != null && int.TryParse(dependency.ExEntityId, out var parentId) &&
                         await rule.HasRelatedDocument(service, parentId, ct))
                    registered++;
            }

            var prerequisiteMissing = rule.Prerequisite.HasValue && eligible == 0;
            var fullyBlocked = eligible > 0 && registered + missing + pending == eligible;
            options.Add(new DestinationOption(
                software.Id, software.Title,
                prerequisiteMissing || fullyBlocked,
                registered == ids.Length,
                prerequisiteMissing,
                prerequisiteMissing ? rule.PrerequisiteMessage : null,
                prerequisiteMissing ? "پیش‌نیاز ثبت در مقصد موجود نیست" :
                    missing > 0 && fullyBlocked ? "سند مقصد پیدا نشد" :
                    pending > 0 && fullyBlocked ? "در حال ثبت در نرم‌افزار مالی" : null));
        }

        return options;
    }

    private SepidarService Resolve(Guid softwareId) =>
        softwareId == FirstSoftwareId ? first :
        softwareId == SecondSoftwareId ? second : null;
}
