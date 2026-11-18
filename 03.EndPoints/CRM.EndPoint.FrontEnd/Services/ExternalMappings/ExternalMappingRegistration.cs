using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Parties.Entities;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings
{
    public sealed partial class ExternalMappingService
    {
        public sealed class SelectedExternalMapping
        {
            public Guid EntityId { get; set; }
            public ExternalMappingTypeEnum MapType { get; set; }
            public string ExEntityId { get; set; }
            public string EntityName { get; set; }
            public string ExEntityName { get; set; }
        }

        public sealed class SaveExternalMappingsRequest
        {
            public Guid ExternalSoftwareId { get; set; }
            public List<SelectedExternalMapping> Items { get; set; } = new();
        }

        public sealed class UpdateExternalMappingRequest
        {
            public Guid ExternalSoftwareId { get; set; }
            public Guid MappingId { get; set; }
            public SelectedExternalMapping Item { get; set; }
            public Guid OriginalEntityId { get; set; }
            public string OriginalEntityName { get; set; }
            public string OriginalExEntityId { get; set; }
            public string OriginalExEntityName { get; set; }
        }

        public async Task<object> GetExternalMappingWorkspaceOptions(CancellationToken ct)
        {
            var software = await db.ExternalSoftwares.AsNoTracking()
                .OrderBy(x => x.Title)
                .Select(x => new { id = x.Id, title = x.Title })
                .ToListAsync(ct);

            // Older type=1 rows are retained in the database but retired from this workspace.
            var counts = await db.ExternalMappings.AsNoTracking()
                .Where(x => (int)x.MapType != 1 && !x.ExEntityId.StartsWith(ExternalMapping.PendingIdPrefix))
                .GroupBy(x => new { x.ExternalSoftwareId, x.MapType })
                .Select(x => new { x.Key.ExternalSoftwareId, x.Key.MapType, Count = x.Count() })
                .ToListAsync(ct);

            var mapTypes = Enum.GetValues<ExternalMappingTypeEnum>()
                .Select(x => new {
                    value = (int)x, title = x.ToString().Replace("_", " "),
                    saved = counts.Where(c => c.MapType == x).Sum(c => c.Count)
                }).ToArray();

            return new {
                software,
                mapTypes,
                counts = counts.Select(x => new {
                    softwareId = x.ExternalSoftwareId,
                    mapType = (int)x.MapType,
                    count = x.Count
                }),
                supportedAutomaticSoftwareIds = new[] { Sepidar1SoftwareId, Sepidar2SoftwareId },
                supportedManualSoftwareIds = new[] { Sepidar1SoftwareId, Sepidar2SoftwareId }
            };
        }

        // Use the same entity pairs for search, preview, creation, editing and reports.
        public async Task<object> GetExternalMappingTargetDetails(
            Guid softwareId, int mapType, int targetId, CancellationToken ct,
            string targetEntity = null)
        {
            if (targetId <= 0 || !Enum.IsDefined(typeof(ExternalMappingTypeEnum), mapType)
                || softwareId != Sepidar1SoftwareId && softwareId != Sepidar2SoftwareId
                || !await db.ExternalSoftwares.AsNoTracking().AnyAsync(x => x.Id == softwareId, ct))
                throw new ArgumentException("نرم‌افزار، نوع ارتباط یا شناسه معتبر نیست.");

            var type = (ExternalMappingTypeEnum)mapType;
            var (source, target) = GetMappingEntityNames(type, targetEntity: targetEntity);
            var sepidar = softwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb : s2Db;

            var exists = target switch {
                ExternalUserEntityName
                    => await sepidar.SepidarUsers.AsNoTracking()
                        .AnyAsync(x => x.UserID == targetId && !x.IsDeleted, ct),
                nameof(Party) when type == ExternalMappingTypeEnum.مشتری
                    => await sepidar.Parties.AsNoTracking()
                        .AnyAsync(x => x.PartyId == targetId && x.IsCustomer, ct),
                nameof(Party)
                    => await sepidar.Parties.AsNoTracking()
                        .AnyAsync(x => x.PartyId == targetId, ct),
                nameof(Quotation)
                    => await sepidar.Quotations.AsNoTracking()
                        .AnyAsync(x => x.QuotationId == targetId, ct),
                nameof(Invoice)
                    => await sepidar.SalesInvoices.AsNoTracking()
                        .AnyAsync(x => x.InvoiceId == targetId, ct),
                nameof(DL)
                    => await sepidar.DLs.AsNoTracking()
                        .AnyAsync(x => x.DLId == targetId && x.IsActive, ct),
                nameof(PaymentHeader)
                    => await sepidar.PaymentHeaders.AsNoTracking()
                        .AnyAsync(x => x.PaymentHeaderId == targetId, ct),
                nameof(Voucher)
                    => await sepidar.Vouchers.AsNoTracking()
                        .AnyAsync(x => x.VoucherId == targetId, ct),
                _ => false
            };
            if (!exists)
                throw new KeyNotFoundException("معادل انتخاب‌شده در نرم‌افزار خارجی پیدا نشد.");

            var entry = new MappingReportEntry(
                "manual-preview", source, Guid.Empty, "pending", "ReadyForReview",
                "", target, targetId, Array.Empty<string>(), mapType,
                targetId.ToString(), new[] { targetId });
            var rows = await EnrichMappingEntriesAsync(new List<MappingReportEntry> { entry }, softwareId, ct);
            return new { target = rows[0].Sepidar };
        }

        // Resolve the current record for editing rather than trusting a stale card.
        public async Task<object> GetExternalMappingForEditAsync(
            Guid softwareId, Guid mappingId, CancellationToken ct)
        {
            if (mappingId == Guid.Empty || softwareId == Guid.Empty)
                throw new ArgumentException("ارتباط انتخاب‌شده معتبر نیست.");

            var mapping = await db.ExternalMappings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == mappingId
                    && x.ExternalSoftwareId == softwareId, ct);
            if (mapping == null)
                throw new KeyNotFoundException("این ارتباط پیدا نشد؛ فهرست را تازه‌سازی کنید.");

            var pair = GetMappingEntityNames(mapping.MapType, mapping.EntityName, mapping.ExEntityName);
            if (pair.Source != mapping.EntityName || pair.Target != mapping.ExEntityName
                || !int.TryParse(mapping.ExEntityId, out var externalId) || externalId <= 0)
                throw new ArgumentException("نوع یا اطلاعات این ارتباط برای ویرایش پشتیبانی نمی‌شود.");

            var entry = new MappingReportEntry("edit-" + mapping.Id, mapping.EntityName,
                mapping.EntityId, "saved", "AlreadyMapped", "", mapping.ExEntityName,
                externalId, Array.Empty<string>(), (int)mapping.MapType,
                mapping.ExEntityId, null, mapping.Id);
            var row = (await EnrichMappingEntriesAsync(
                new List<MappingReportEntry> { entry }, softwareId, ct))[0];

            var crmDisplay = string.IsNullOrWhiteSpace(row.Crm.Customer)
                ? row.Crm.Title : row.Crm.Customer;
            var targetDisplay = string.IsNullOrWhiteSpace(row.Sepidar.Customer)
                ? row.Sepidar.Title : row.Sepidar.Customer;
            return new {
                mappingId = mapping.Id, externalSoftwareId = softwareId,
                mapType = (int)mapping.MapType,
                entityName = mapping.EntityName, entityId = mapping.EntityId,
                exEntityName = mapping.ExEntityName, exEntityId = mapping.ExEntityId,
                crmOption = new {
                    id = mapping.EntityId.ToString(),
                    title = FormatManualOption(row.Crm.Number, crmDisplay)
                },
                externalOption = new {
                    id = mapping.ExEntityId,
                    title = FormatManualOption(row.Sepidar.Number, targetDisplay),
                    isCustomer = row.Sepidar.IsCustomer ?? false,
                    isEmployee = row.Sepidar.IsEmployee ?? false,
                    isVendor = row.Sepidar.IsVendor ?? false,
                    isBroker = row.Sepidar.IsBroker ?? false
                }
            };
        }

        private async Task<bool> HasSameExternalFiscalYearAsync(
            SepidarContextDbBase context, int fiscalYearRef, string crmDate,
            DateTime? crmMiladiDate, CancellationToken ct)
        {
            var year = await context.FiscalYears.AsNoTracking()
                .Where(x => x.FiscalYearId == fiscalYearRef)
                .Select(x => new { x.Title, x.StartDate, x.EndDate })
                .SingleOrDefaultAsync(ct);
            var persianYear = year == null ? null : GetLegacyFiscalYear(year.Title);
            return year != null && persianYear.HasValue
                && IsInLegacyFiscalYear(crmDate, crmMiladiDate,
                    new LegacyFiscalYearRow(persianYear.Value, year.StartDate, year.EndDate));
        }

        // Delete only the CRM ExternalMapping row, never the external software record.
        // Keep existing invoice/quotation mappings consistent with their customer mappings.
        public async Task DeleteExternalMappingAsync(
            Guid softwareId, Guid mappingId, CancellationToken ct)
        {
            if (softwareId == Guid.Empty || mappingId == Guid.Empty)
                throw new ArgumentException("شناسه نرم‌افزار یا ارتباط معتبر نیست.");

            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

            var mapping = await db.ExternalMappings.SingleOrDefaultAsync(
                x => x.Id == mappingId && x.ExternalSoftwareId == softwareId, ct);
            if (mapping == null)
                throw new KeyNotFoundException("ارتباط انتخاب‌شده پیدا نشد یا قبلاً حذف شده است.");

            if (mapping.MapType is ExternalMappingTypeEnum.تامین_کننده
                or ExternalMappingTypeEnum.فاکتور_خرید or ExternalMappingTypeEnum.رسید_انبار)
                throw new MappingConflictException("این ارتباط از گردش انتقال مالی فاکتور تأمین مدیریت می‌شود.");

            if (mapping.MapType == ExternalMappingTypeEnum.مشتری
                && mapping.EntityName == nameof(Customer))
            {
                var hasMappedPreFactor = await db.PreFactors.AsNoTracking()
                    .AnyAsync(p => p.CustomerId == mapping.EntityId
                        && db.ExternalMappings.Any(m =>
                            m.ExternalSoftwareId == softwareId
                            && m.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                            && m.EntityId == p.Id), ct);

                var hasMappedFactor = await db.Factors.AsNoTracking()
                    .AnyAsync(f =>
                        (f.CustomerId == mapping.EntityId ||
                            db.PreFactors.Any(p => p.Id == f.PfactorID
                                && p.CustomerId == mapping.EntityId))
                        && db.ExternalMappings.Any(m =>
                            m.ExternalSoftwareId == softwareId
                            && m.MapType == ExternalMappingTypeEnum.فاکتور
                            && m.EntityId == f.Id), ct);

                if (hasMappedPreFactor || hasMappedFactor)
                    throw new MappingConflictException(
                        "برای این مشتری ارتباط پیش‌فاکتور یا فاکتور ثبت‌شده وجود دارد. " +
                        "ابتدا ارتباط پیش‌فاکتورها و فاکتورهای مربوط را حذف کنید.");
            }

            if (mapping.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                && mapping.EntityName == nameof(PreFactor))
            {
                var hasMappedFactor = await db.Factors.AsNoTracking()
                    .AnyAsync(f => f.PfactorID == mapping.EntityId
                        && db.ExternalMappings.Any(m =>
                            m.ExternalSoftwareId == softwareId
                            && m.MapType == ExternalMappingTypeEnum.فاکتور
                            && m.EntityId == f.Id), ct);
                if (hasMappedFactor)
                    throw new MappingConflictException(
                        "برای این پیش‌فاکتور، ارتباط فاکتور ثبت‌شده وجود دارد. " +
                        "ابتدا ارتباط فاکتور مربوط را حذف کنید.");
            }

            db.ExternalMappings.Remove(mapping);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        // Only selected rows are submitted; no automatic migration runs from page visits.
        // On success each record becomes a persisted ExternalMapping, never an external INSERT.


        public Task<int> SaveSelectedExternalMappings(
            SaveExternalMappingsRequest request, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct)
            => SaveExternalMappingsCoreAsync(request, principal, null, ct);

        // Updates the existing CRM row in place; no DELETE/INSERT and no external write.
        public Task<int> UpdateExternalMappingAsync(
            UpdateExternalMappingRequest request, System.Security.Claims.ClaimsPrincipal principal,
            CancellationToken ct)
        {
            if (request?.Item == null || request.MappingId == Guid.Empty
                || request.OriginalEntityId == Guid.Empty)
                throw new ArgumentException("ارتباط انتخاب‌شده برای ویرایش معتبر نیست.");

            return SaveExternalMappingsCoreAsync(new SaveExternalMappingsRequest {
                ExternalSoftwareId = request.ExternalSoftwareId,
                Items = new List<SelectedExternalMapping> { request.Item }
            }, principal, request, ct);
        }

        private async Task<int> SaveExternalMappingsCoreAsync(
            SaveExternalMappingsRequest request, System.Security.Claims.ClaimsPrincipal principal,
            UpdateExternalMappingRequest edit, CancellationToken ct)
        {
            if (request == null || request.Items == null || request.Items.Count == 0 || request.Items.Count > 100)
                throw new ArgumentException("بین ۱ تا ۱۰۰ ارتباط را برای ثبت انتخاب کنید.");

            if (request.ExternalSoftwareId != Sepidar1SoftwareId
                && request.ExternalSoftwareId != Sepidar2SoftwareId)
                throw new ArgumentException("برای این نرم‌افزار، ثبت دستی هنوز پشتیبانی نمی‌شود.");

            if (!await db.ExternalSoftwares.AsNoTracking()
                    .AnyAsync(x => x.Id == request.ExternalSoftwareId, ct))
                throw new ArgumentException("نرم‌افزار انتخاب‌شده ثبت نشده است.");

            if (request.Items.Any(x => x == null
                || !Enum.IsDefined(typeof(ExternalMappingTypeEnum), x.MapType)
                || x.EntityId == Guid.Empty
                || !int.TryParse(x.ExEntityId, out var id) || id <= 0))
                throw new ArgumentException("نوع ارتباط یا شناسه انتخاب‌شده معتبر نیست.");

            var repeatedSources = request.Items.GroupBy(x => {
                var pair = GetMappingEntityNames(x.MapType, x.EntityName, x.ExEntityName);
                return (x.MapType, pair.Source, x.EntityId);
            }).Any(x => x.Count() > 1);
            if (repeatedSources)
                throw new ArgumentException("برای هر رکورد CRM فقط یک معادل انتخاب کنید.");

            var hasCustomer = request.Items.Any(x => x.MapType == ExternalMappingTypeEnum.مشتری);
            var hasOther = request.Items.Any(x => x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                || x.MapType == ExternalMappingTypeEnum.فاکتور);
            if (hasCustomer && hasOther)
                throw new ArgumentException("ابتدا ارتباط مشتریان را ثبت کنید؛ سپس پیش‌فاکتور یا فاکتور را انتخاب کنید.");

            // Force users to resolve customer conflicts explicitly rather than allowing
            // a bulk click to select an arbitrary Party for several CRM customers.
            var crmUser = await userManager.GetUserAsync(principal);
            if (crmUser == null) throw new UnauthorizedAccessException("کاربر جاری شناسایی نشد.");

            var targetDb = request.ExternalSoftwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb : s2Db;

            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

            try
            {
                var existing = await db.ExternalMappings.AsNoTracking()
                    .Where(x => x.ExternalSoftwareId == request.ExternalSoftwareId)
                    .ToListAsync(ct);

                ExternalMapping edited = null;
                if (edit != null)
                {
                    edited = await db.ExternalMappings.SingleOrDefaultAsync(
                        x => x.Id == edit.MappingId
                            && x.ExternalSoftwareId == request.ExternalSoftwareId, ct);
                    if (edited == null)
                        throw new KeyNotFoundException("این ارتباط دیگر وجود ندارد؛ فهرست را تازه‌سازی کنید.");

                    if (edited.MapType != request.Items[0].MapType)
                        throw new ArgumentException("برای تغییر نوع ارتباط، ارتباط جدیدی ثبت کنید.");

                    if (edited.EntityId != edit.OriginalEntityId
                        || edited.EntityName != edit.OriginalEntityName
                        || edited.ExEntityName != edit.OriginalExEntityName
                        || edited.ExEntityId != edit.OriginalExEntityId)
                        throw new MappingConflictException(
                            "این ارتباط قبلاً تغییر کرده است؛ فهرست را تازه‌سازی کنید.");
                }

                var duplicatesInRequest = request.Items.GroupBy(x => {
                    var pair = GetMappingEntityNames(x.MapType, x.EntityName, x.ExEntityName);
                    return (x.MapType, pair.Target, int.Parse(x.ExEntityId.Trim()));
                }).Any(x => x.Count() > 1);
                if (duplicatesInRequest)
                    throw new ArgumentException("چند رکورد CRM نمی‌توانند به یک معادل خارجی متصل شوند.");

                var confirmedCustomerMappings = existing
                    .Where(x => x.MapType == ExternalMappingTypeEnum.مشتری
                        && x.EntityName == nameof(Customer))
                    .GroupBy(x => x.EntityId)
                    .ToDictionary(x => x.Key, x => x.ToList());

                var newMappings = new List<ExternalMapping>();
                foreach (var item in request.Items)
                {
                    var targetId = int.Parse(item.ExEntityId.Trim());
                    var (entityName, externalName) = GetMappingEntityNames(
                        item.MapType, item.EntityName, item.ExEntityName);

                    if (existing.Any(x => x.Id != edited?.Id
                        && x.EntityName == entityName
                        && x.EntityId == item.EntityId && x.MapType == item.MapType))
                        throw new MappingConflictException($"برای {item.MapType} در نرم‌افزار انتخاب‌شده قبلاً ارتباط ثبت شده است.");

                    if (existing.Any(x => x.Id != edited?.Id
                        && x.MapType == item.MapType
                        && x.ExEntityName == externalName
                        && x.ExEntityId == targetId.ToString()))
                        throw new MappingConflictException($"معادل خارجی شماره {targetId} قبلاً به رکورد CRM دیگری وصل شده است.");

                    // Do not invalidate downstream documents by changing an existing
                    // customer/quotation relation that those documents still rely on.
                    if (edited != null && (edited.EntityId != item.EntityId
                        || edited.EntityName != entityName
                        || edited.ExEntityId != targetId.ToString()
                        || edited.ExEntityName != externalName))
                    {
                        if (edited.MapType == ExternalMappingTypeEnum.مشتری)
                        {
                            var oldCustomerId = edited.EntityId;
                            var linkedQuotation = await db.PreFactors.AsNoTracking()
                                .AnyAsync(p => p.CustomerId == oldCustomerId
                                    && db.ExternalMappings.Any(m =>
                                        m.ExternalSoftwareId == request.ExternalSoftwareId
                                        && m.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                                        && m.EntityId == p.Id), ct);
                            var linkedInvoice = await db.Factors.AsNoTracking()
                                .AnyAsync(f => (f.CustomerId == oldCustomerId
                                    || db.PreFactors.Any(p =>
                                        p.Id == f.PfactorID && p.CustomerId == oldCustomerId))
                                    && db.ExternalMappings.Any(m =>
                                        m.ExternalSoftwareId == request.ExternalSoftwareId
                                        && m.MapType == ExternalMappingTypeEnum.فاکتور
                                        && m.EntityId == f.Id), ct);
                            if (linkedQuotation || linkedInvoice)
                                throw new MappingConflictException(
                                    "برای این مشتری پیش‌فاکتور یا فاکتور ثبت‌شده وجود دارد؛ ابتدا ارتباط اسناد مربوط را بررسی کنید.");
                        }
                        else if (edited.MapType == ExternalMappingTypeEnum.پیش_فاکتور)
                        {
                            var linkedInvoice = await db.Factors.AsNoTracking()
                                .AnyAsync(f => f.PfactorID == edited.EntityId
                                    && db.ExternalMappings.Any(m =>
                                        m.ExternalSoftwareId == request.ExternalSoftwareId
                                        && m.MapType == ExternalMappingTypeEnum.فاکتور
                                        && m.EntityId == f.Id), ct);
                            if (linkedInvoice)
                                throw new MappingConflictException(
                                    "این پیش‌فاکتور فاکتور ثبت‌شده دارد و ارتباط آن قابل تغییر نیست.");
                        }
                    }

                    if (item.MapType == ExternalMappingTypeEnum.مشتری)
                    {
                        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == item.EntityId, ct))
                            throw new ArgumentException("مشتری انتخاب‌شده در CRM یافت نشد.");
                        if (!await targetDb.Parties.AsNoTracking()
                                .AnyAsync(x => x.PartyId == targetId && x.IsCustomer, ct))
                            throw new ArgumentException($"مشتری با شناسه {targetId} در نرم‌افزار مقصد معتبر نیست.");
                    }
                    else if (item.MapType == ExternalMappingTypeEnum.کارشناسان)
                    {
                        if (!await userManager.Users.AsNoTracking().AnyAsync(x =>
                                x.Id == item.EntityId.ToString() && x.IsEmployee, ct))
                            throw new ArgumentException("کارمند انتخاب‌شده در CRM پیدا نشد.");
                        // An expert mapping only requires a valid Party. Being a broker
                        // is a separate, invoice-specific eligibility requirement.
                        if (!await targetDb.Parties.AsNoTracking().AnyAsync(x =>
                                x.PartyId == targetId, ct))
                            throw new ArgumentException("شخص متناظر با کارشناس CRM در نرم‌افزار مالی پیدا نشد.");
                    }
                    else if (item.MapType == ExternalMappingTypeEnum.کاربران)
                    {
                        if (!await userManager.Users.AsNoTracking()
                                .AnyAsync(x => x.Id == item.EntityId.ToString(), ct))
                            throw new ArgumentException("کاربر انتخاب‌شده در CRM پیدا نشد.");
                        if (!await targetDb.SepidarUsers.AsNoTracking()
                                .AnyAsync(x => x.UserID == targetId && !x.IsDeleted, ct))
                            throw new ArgumentException("کاربر انتخاب‌شده در نرم‌افزار مقصد پیدا نشد یا حذف شده است.");
                    }
                    else if (item.MapType == ExternalMappingTypeEnum.مرکز_هزینه)
                    {
                        if (!await db.BaseCenterCashes.AsNoTracking().AnyAsync(x => x.Id == item.EntityId, ct))
                            throw new ArgumentException("مرکز هزینه انتخاب‌شده در CRM پیدا نشد.");
                        if (!await targetDb.DLs.AsNoTracking().AnyAsync(x => x.DLId == targetId && x.IsActive, ct))
                            throw new ArgumentException("حساب تفصیلی انتخاب‌شده در نرم‌افزار مالی فعال نیست.");
                    }
                    else if (item.MapType == ExternalMappingTypeEnum.تنخواه)
                    {
                        if (!await db.Cashes.AsNoTracking().AnyAsync(x => x.Id == item.EntityId, ct))
                            throw new ArgumentException("تنخواه انتخاب‌شده در CRM پیدا نشد.");
                        var exists = externalName == nameof(Voucher)
                            ? await targetDb.Vouchers.AsNoTracking().AnyAsync(x => x.VoucherId == targetId, ct)
                            : await targetDb.PaymentHeaders.AsNoTracking()
                                .AnyAsync(x => x.PaymentHeaderId == targetId, ct);
                        if (!exists) throw new ArgumentException("معادل تنخواه در نرم‌افزار مالی پیدا نشد.");
                    }
                    else
                    {
                        Guid? customerId;
                        int partyId;
                        if (item.MapType == ExternalMappingTypeEnum.پیش_فاکتور)
                        {
                            var pf = await db.PreFactors.AsNoTracking()
                                .Where(x => x.Id == item.EntityId)
                                .Select(x => new { x.CustomerId, x.Date, x.DateMiladi })
                                .SingleOrDefaultAsync(ct);
                            if (pf == null || !pf.CustomerId.HasValue)
                                throw new ArgumentException("پیش‌فاکتور CRM مشتری مشخصی ندارد.");
                            customerId = pf.CustomerId;
                            var external = await targetDb.Quotations.AsNoTracking()
                                .Where(x => x.QuotationId == targetId)
                                .Select(x => new { x.CustomerPartyRef, x.FiscalYearRef })
                                .SingleOrDefaultAsync(ct);
                            if (external == null)
                                throw new ArgumentException($"پیش‌فاکتور با شناسه {targetId} در نرم‌افزار مقصد پیدا نشد.");
                            if (!await HasSameExternalFiscalYearAsync(targetDb, external.FiscalYearRef,
                                    pf.Date, pf.DateMiladi, ct))
                                throw new ArgumentException("سال مالی پیش‌فاکتور CRM و نرم‌افزار انتخاب‌شده یکسان نیست.");
                            partyId = external.CustomerPartyRef;
                        }
                        else
                        {
                            var factor = await db.Factors.AsNoTracking()
                                .Where(x => x.Id == item.EntityId)
                                .Select(x => new { x.CustomerId, x.PfactorID, x.status,
                                    x.ParentReturnedId, x.Date, x.DateMiladi })
                                .SingleOrDefaultAsync(ct);
                            if (factor == null || factor.status != FactorStatus.تایید_شده
                                || factor.ParentReturnedId.HasValue)
                                throw new ArgumentException("فقط می‌توان برای فاکتور تأییدشده و غیربرگشتی ارتباط ثبت کرد.");

                            var pfCustomerId = await db.PreFactors.AsNoTracking()
                                .Where(x => x.Id == factor.PfactorID)
                                .Select(x => x.CustomerId)
                                .FirstOrDefaultAsync(ct);
                            if (factor.CustomerId.HasValue && pfCustomerId.HasValue
                                && factor.CustomerId != pfCustomerId)
                                throw new ArgumentException("مشتری فاکتور و پیش‌فاکتور مرتبط متفاوت است.");
                            customerId = factor.CustomerId ?? pfCustomerId;
                            if (!customerId.HasValue)
                                throw new ArgumentException("مشتری فاکتور مشخص نیست.");

                            var external = await targetDb.SalesInvoices.AsNoTracking()
                                .Where(x => x.InvoiceId == targetId)
                                .Select(x => new { x.CustomerPartyRef, x.QuotationRef, x.FiscalYearRef })
                                .SingleOrDefaultAsync(ct);
                            if (external == null)
                                throw new ArgumentException($"فاکتور با شناسه {targetId} در نرم‌افزار مقصد پیدا نشد.");
                            if (!await HasSameExternalFiscalYearAsync(targetDb, external.FiscalYearRef,
                                    factor.Date, factor.DateMiladi, ct))
                                throw new ArgumentException("سال مالی فاکتور CRM و نرم‌افزار انتخاب‌شده یکسان نیست.");
                            partyId = external.CustomerPartyRef;

                            var crmQuotationMapping = existing.FirstOrDefault(x =>
                                x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                                && x.EntityId == factor.PfactorID);
                            if (crmQuotationMapping != null
                                && crmQuotationMapping.ExEntityId != external.QuotationRef?.ToString())
                                throw new ArgumentException("پیش‌فاکتور مرتبط با فاکتور سپیدار، با ارتباط ثبت‌شده در CRM هم‌خوان نیست.");
                        }

                        if (!confirmedCustomerMappings.TryGetValue(customerId.Value, out var customerMaps)
                            || customerMaps.Count != 1
                            || customerMaps[0].ExEntityName != "Party"
                            || customerMaps[0].ExEntityId != partyId.ToString())
                            throw new ArgumentException("ابتدا ارتباط مشتری مربوط به فاکتور یا پیش‌فاکتور را با همین نرم‌افزار ثبت کنید.");
                    }

                    if (edited != null)
                    {
                        // Preserve the original ID, creator and creation date.
                        edited.EntityName = entityName;
                        edited.EntityId = item.EntityId;
                        edited.ExEntityName = externalName;
                        edited.ExEntityId = targetId.ToString();
                    }
                    else
                    {
                        newMappings.Add(new ExternalMapping {
                            Id = Guid.NewGuid(),
                            ExternalSoftwareId = request.ExternalSoftwareId,
                            EntityName = entityName,
                            EntityId = item.EntityId,
                            MapType = item.MapType,
                            MapTypeTxt = item.MapType.ToString(),
                            ExEntityName = externalName,
                            ExEntityId = targetId.ToString(),
                            UserCreatorID = crmUser.Id,
                            UserCreator = crmUser.FullName
                        });
                    }
                }

                db.ExternalMappings.AddRange(newMappings);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return edited != null ? 1 : newMappings.Count;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Saving selected external mappings failed.");
                throw new MappingConflictException("اطلاعات ارتباط در این فاصله تغییر کرده است؛ فهرست را دوباره دریافت کنید.");
            }
        }
    }
}
