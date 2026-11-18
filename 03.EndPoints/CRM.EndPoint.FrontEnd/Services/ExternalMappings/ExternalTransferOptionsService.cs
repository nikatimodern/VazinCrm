using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
    private static readonly string[] FinancialRoles = ["مدیر ارشد", "مدیر عامل", "مدیر مالی", "کارشناس مالی"];

    public static IReadOnlyDictionary<ExternalMappingTypeEnum, TransferRule> Rules { get; } =
        new Dictionary<ExternalMappingTypeEnum, TransferRule> {
            [ExternalMappingTypeEnum.تنخواه] =
                new(nameof(Cash), CleanupMissingCashDocuments: true),
            [ExternalMappingTypeEnum.پیش_فاکتور] = new(nameof(PreFactor), AllowedRoles: FinancialRoles),
            [ExternalMappingTypeEnum.فاکتور] = new(nameof(Factor), AllowedRoles: FinancialRoles),
            [ExternalMappingTypeEnum.فاکتور_خرید] = new(nameof(SupplyFactor),
                AllowedRoles: FinancialRoles,
                SupportedDestinationsOnly: true),
            [ExternalMappingTypeEnum.رسید_انبار] = new(nameof(SupplyFactor),
                AllowedRoles: FinancialRoles,
                SupportedDestinationsOnly: true,
                Prerequisite: ExternalMappingTypeEnum.فاکتور_خرید,
                PrerequisiteMessage: "برای ثبت رسید انبار، ابتدا فاکتور خرید را در نرم‌افزار مالی ثبت کنید.",
                HasRelatedDocument: async (service, purchaseId, ct) =>
                    await service.FindReceiptForPurchaseAsync(purchaseId, ct) != null)
        };

    public bool TryGetRule(ExternalMappingTypeEnum mapType, out TransferRule rule) =>
        Rules.TryGetValue(mapType, out rule);

    public async Task<IReadOnlyList<DestinationOption>> GetAllExternalSoftwareAsync(
        IReadOnlyCollection<Guid> sourceIds, ExternalMappingTypeEnum mapType,
        ClaimsPrincipal user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sourceIds);
        ArgumentNullException.ThrowIfNull(user);
        if (!TryGetRule(mapType, out var rule))
            throw new ArgumentOutOfRangeException(nameof(mapType));
        if (rule.AllowedRoles != null && !rule.AllowedRoles.Any(user.IsInRole))
            throw new UnauthorizedAccessException();

        var ids = sourceIds.Distinct().ToArray();
        if (ids.Length == 0 || ids.Any(id => id == Guid.Empty))
            throw new ArgumentException("Source IDs must be nonempty.", nameof(sourceIds));

        var mappings = await LoadMappingsAsync(ids, mapType, rule, ct);
        var existence = await CheckExternalRecordsAsync(mappings, ct);
        await CleanupMissingCashDocumentsAsync(mappings, existence, rule, ct);

        var byDestination = mappings.ToLookup(m =>
            (m.ExternalSoftwareId, m.EntityId, m.MapType));
        var softwares = await db.ExternalSoftwares.AsNoTracking()
            .OrderBy(x => x.Title)
            .Select(x => new { x.Id, x.Title })
            .ToListAsync(ct);

        var options = new List<DestinationOption>(softwares.Count);
        foreach (var software in softwares)
        {
            ct.ThrowIfCancellationRequested();
            var service = Resolve(software.Id);
            if (rule.SupportedDestinationsOnly && service == null)
                continue;

            options.Add(await BuildOptionAsync(software.Id, software.Title, service,
                ids, mapType, rule, byDestination, existence, ct));
        }
        return options;
    }

    private Task<List<ExternalMapping>> LoadMappingsAsync(
        Guid[] ids, ExternalMappingTypeEnum mapType, TransferRule rule, CancellationToken ct)
    {
        var prerequisiteEntity = rule.Prerequisite is { } type
            ? Rules[type].SourceEntity : null;
        return db.ExternalMappings.AsNoTracking()
            .Where(x => ids.Contains(x.EntityId) &&
                ((x.EntityName == rule.SourceEntity && x.MapType == mapType) ||
                 (x.EntityName == prerequisiteEntity && x.MapType == rule.Prerequisite)))
            .ToListAsync(ct);
    }

    private async Task<Dictionary<Guid, bool?>> CheckExternalRecordsAsync(
        IReadOnlyCollection<ExternalMapping> mappings, CancellationToken ct)
    {
        // A destination record may have several CRM mappings. Check each target once.
        var checkedRecords = new Dictionary<(Guid SoftwareId, string EntityName, string EntityId), bool?>();
        var existence = new Dictionary<Guid, bool?>();
        foreach (var mapping in mappings.Where(x => !x.HasPendingExternalId()))
        {
            ct.ThrowIfCancellationRequested();
            var service = Resolve(mapping.ExternalSoftwareId);
            if (service == null)
            {
                existence[mapping.Id] = null; // Unknown is never proof of deletion.
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
        return existence;
    }

    private async Task CleanupMissingCashDocumentsAsync(
        List<ExternalMapping> mappings, IReadOnlyDictionary<Guid, bool?> existence,
        TransferRule rule, CancellationToken ct)
    {
        if (!rule.CleanupMissingCashDocuments) return;

        // Cash has no financial-history modal. This exception must not spread to
        // quotations, sales invoices, purchase invoices, receipts or new types.
        var missingCash = mappings.Where(m =>
            m.MapType == ExternalMappingTypeEnum.تنخواه &&
            m.EntityName == nameof(Cash) &&
            (m.ExEntityName == nameof(PaymentHeader) || m.ExEntityName == nameof(Voucher)) &&
            existence.GetValueOrDefault(m.Id) == false).ToArray();
        if (missingCash.Length == 0) return;

        db.ExternalMappings.RemoveRange(missingCash);
        await db.SaveChangesAsync(ct);
        var removedIds = missingCash.Select(m => m.Id).ToHashSet();
        mappings.RemoveAll(m => removedIds.Contains(m.Id));
    }

    private static async Task<DestinationOption> BuildOptionAsync(
        Guid softwareId, string title, SepidarService service, IReadOnlyList<Guid> ids,
        ExternalMappingTypeEnum mapType, TransferRule rule,
        ILookup<(Guid SoftwareId, Guid SourceId, ExternalMappingTypeEnum MapType), ExternalMapping> mappings,
        IReadOnlyDictionary<Guid, bool?> existence, CancellationToken ct)
    {
        var eligible = 0;
        var registered = 0;
        var missing = 0;
        var pending = 0;

        foreach (var sourceId in ids)
        {
            ExternalMapping dependency = null;
            if (rule.Prerequisite is { } requiredType)
            {
                dependency = mappings[(softwareId, sourceId, requiredType)]
                    .FirstOrDefault(m => existence.GetValueOrDefault(m.Id) == true);
                if (dependency == null) continue;
            }

            eligible++;
            var current = mappings[(softwareId, sourceId, mapType)].ToArray();
            if (current.Any(m => m.HasPendingExternalId()))
            {
                pending++;
                continue;
            }
            if (current.Any(m => existence.GetValueOrDefault(m.Id) != false))
                registered++;
            else if (current.Length > 0)
                missing++;
            else if (dependency != null && rule.HasRelatedDocument != null &&
                     service != null && int.TryParse(dependency.ExEntityId, out var parentId) &&
                     await rule.HasRelatedDocument(service, parentId, ct))
                registered++;
        }

        var prerequisiteMissing = rule.Prerequisite.HasValue && eligible == 0;
        var fullyBlocked = eligible > 0 && registered + missing + pending == eligible;
        return new DestinationOption(
            softwareId, title,
            prerequisiteMissing || fullyBlocked,
            registered == ids.Count,
            prerequisiteMissing,
            prerequisiteMissing ? rule.PrerequisiteMessage : null,
            prerequisiteMissing ? "پیش‌نیاز ثبت در مقصد موجود نیست" :
                missing > 0 && fullyBlocked ? "سند مقصد پیدا نشد" :
                pending > 0 && fullyBlocked ? "در حال ثبت در نرم‌افزار مالی" : null);
    }

    private SepidarService Resolve(Guid softwareId) =>
        softwareId == FirstSoftwareId ? first :
        softwareId == SecondSoftwareId ? second : null;
}
