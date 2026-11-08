using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Parties.Entities;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers
{
    // Admin-only, read-only diagnostics built on the existing legacy mapping plan.
    public partial class HomeController
    {
        private sealed record MappingReportSide(
            string Title, string Id, string Number, string Date,
            decimal? Amount, string Customer, string Extra);

        private sealed record MappingReportEntry(
            string Key, string Kind, Guid EntityId, string Status,
            string Reason, string Details, string TargetEntity, int? TargetId,
            string[] Sources);

        private sealed record MappingReportRow(
            string Key, string Kind, string Status, string Reason,
            string Details, string[] Sources,
            MappingReportSide Crm, MappingReportSide Sepidar);

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        public IActionResult ExternalMappingReport() => View();

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetExternalMappingReview(CancellationToken ct)
        {
            // NEVER invoke ApplyLegacySepidarMappingPlanAsync from a report endpoint.
            var plan = await BuildLegacySepidarMappingPlanAsync(ct);
            var entries = new List<MappingReportEntry>();

            // For conflicts with multiple possible Sepidar customers, display ALL
            // candidate Party names (not just the first PartyId).
            var candidatePartyIds = plan.CustomerConflicts
                .SelectMany(x => x.SepidarPartyIds).Where(x => x > 0)
                .Distinct().ToArray();
            var partyNames = new Dictionary<int, string>();
            foreach (var batch in candidatePartyIds.Chunk(350))
            {
                var parties = await sdb.Parties.AsNoTracking()
                    .Where(x => batch.Contains(x.PartyId))
                    .Select(x => new { x.PartyId, x.Name, x.LastName }).ToListAsync(ct);
                foreach (var party in parties)
                    partyNames[party.PartyId] = $"{party.Name} {party.LastName}".Trim();
            }

            foreach (var conflict in plan.CustomerConflicts)
            {
                var partyDescriptions = conflict.SepidarPartyIds.Select(id =>
                    $"سپیدار: PartyId={id} | {partyNames.GetValueOrDefault(id, "شخص پیدا نشد")}");
                var sources = conflict.SourceDocuments.Concat(partyDescriptions).ToArray();
                entries.Add(new MappingReportEntry(
                    $"customer-conflict-{conflict.CustomerId}", nameof(Customer),
                    conflict.CustomerId, "customerConflict", conflict.Reason,
                    $"شناسه‌های احتمالی مشتری در سپیدار: {string.Join("، ", conflict.SepidarPartyIds)}",
                    nameof(Party), conflict.SepidarPartyIds.FirstOrDefault() is var id && id > 0 ? id : null,
                    sources));
            }

            foreach (var issue in plan.Issues)
            {
                var kind = issue.EntityName;
                var targetName = kind == nameof(PreFactor) ? nameof(Quotation)
                    : kind == nameof(Factor) ? nameof(Invoice) : nameof(Party);
                var status = issue.Status switch
                {
                    "Ambiguous" => "ambiguous",
                    "BlockedByCustomer" => "blocked",
                    _ => "notMatched"
                };
                var targetId = ExtractLegacyTargetId(issue.Details, targetName);
                var relatedGroup = issue.Reason == "DuplicateProposedTarget" && targetId.HasValue
                    ? plan.DuplicateTargetGroups.FirstOrDefault(x => x.ExEntityName == targetName
                        && x.ExEntityId == targetId.Value.ToString())
                    : null;
                var relatedDocuments = relatedGroup?.CrmDocuments
                    .Select(x => $"CRM {x.EntityName} شماره {x.SerialNumber} | ID={x.EntityId} | مبلغ={x.PaymentAmount}")
                    .ToArray() ?? Array.Empty<string>();
                entries.Add(new MappingReportEntry(
                    $"issue-{kind}-{issue.EntityId}-{issue.Reason}", kind,
                    issue.EntityId, status, issue.Reason,
                    issue.Details, targetName, targetId, relatedDocuments));
            }

            foreach (var mapping in plan.Pending)
            {
                int.TryParse(mapping.ExEntityId, out var targetId);
                entries.Add(new MappingReportEntry(
                    $"pending-{mapping.EntityName}-{mapping.EntityId}", mapping.EntityName,
                    mapping.EntityId, "pending", "ReadyForReview",
                    "این ارتباط فقط پیشنهاد شده و هنوز در ExternalMapping ذخیره نشده است.",
                    mapping.ExEntityName, targetId > 0 ? targetId : null, Array.Empty<string>()));
            }

            var rows = await EnrichMappingEntriesAsync(entries, ct);
            var saved = await db.ExternalMappings.AsNoTracking()
                .Where(x => x.ExternalSoftwareId == Sepidar1SoftwareId
                    && (x.MapType == ExternalMappingTypeEnum.مشتری
                        || x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                        || x.MapType == ExternalMappingTypeEnum.فاکتور))
                .CountAsync(ct);

            return Ok(new
            {
                summary = new {
                    saved,
                    pendingCustomers = plan.PendingCustomers.Count,
                    pendingDocuments = plan.PendingDocuments.Count,
                    customerConflicts = plan.CustomerConflicts.Count,
                    blockedDocuments = plan.BlockedDocumentCount,
                    unmatched = plan.NotMatched,
                    ambiguous = plan.Ambiguous,
                    duplicateGroups = plan.DuplicateTargetGroups.Count
                },
                rows
            });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetSavedExternalMappings(
            string type = "", string search = "", int page = 1, int pageSize = 12,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 30);
            search = (search ?? "").Trim();
            if (search.Length > 80) search = search[..80];

            var query = db.ExternalMappings.AsNoTracking()
                .Where(x => x.ExternalSoftwareId == Sepidar1SoftwareId
                    && (x.MapType == ExternalMappingTypeEnum.مشتری
                        || x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                        || x.MapType == ExternalMappingTypeEnum.فاکتور));

            if (type == nameof(Customer))
                query = query.Where(x => x.MapType == ExternalMappingTypeEnum.مشتری);
            else if (type == nameof(PreFactor))
                query = query.Where(x => x.MapType == ExternalMappingTypeEnum.پیش_فاکتور);
            else if (type == nameof(Factor))
                query = query.Where(x => x.MapType == ExternalMappingTypeEnum.فاکتور);

            if (search.Length > 0)
            {
                var isNumber = int.TryParse(search, out var number);
                var matchedIds = new HashSet<Guid>();

                var customers = await db.Customers.AsNoTracking()
                    .Where(x => (x.CompanyName != null && x.CompanyName.Contains(search))
                        || (x.AgentName != null && x.AgentName.Contains(search))
                        || (isNumber && x.CustomerCode == number))
                    .Select(x => x.Id).Take(300).ToListAsync(ct);
                matchedIds.UnionWith(customers);

                var preFactors = await db.PreFactors.AsNoTracking()
                    .Where(x => (x.Customer != null && x.Customer.Contains(search))
                        || (isNumber && x.SerialNumber == number))
                    .Select(x => x.Id).Take(300).ToListAsync(ct);
                matchedIds.UnionWith(preFactors);

                var factors = await db.Factors.AsNoTracking()
                    .Where(x => (x.Customer != null && x.Customer.Contains(search))
                        || (isNumber && x.SerialNumber == number))
                    .Select(x => x.Id).Take(300).ToListAsync(ct);
                matchedIds.UnionWith(factors);

                var ids = matchedIds.ToArray();
                query = query.Where(x => x.ExEntityId.Contains(search)
                    || x.EntityId.ToString().Contains(search)
                    || ids.Contains(x.EntityId));
            }

            var total = await query.CountAsync(ct);
            var mappings = await query.OrderByDescending(x => x.CreateDate)
                .ThenBy(x => x.EntityId).Skip((page - 1) * pageSize)
                .Take(pageSize).Select(x => new
                {
                    x.EntityName, x.EntityId, x.ExEntityName, x.ExEntityId
                }).ToListAsync(ct);

            var entries = mappings.Select(x => {
                int.TryParse(x.ExEntityId, out var externalId);
                return new MappingReportEntry(
                    $"saved-{x.EntityName}-{x.EntityId}", x.EntityName,
                    x.EntityId, "saved", "AlreadyMapped",
                    "این ارتباط قبلاً در ExternalMapping ثبت شده است.",
                    x.ExEntityName, externalId > 0 ? externalId : null, Array.Empty<string>());
            }).ToList();

            return Ok(new {
                total, page, pageSize,
                rows = await EnrichMappingEntriesAsync(entries, ct)
            });
        }

        private static string FormatLegacyPersianDate(DateTime date)
        {
            var calendar = new PersianCalendar();
            return $"{calendar.GetYear(date):0000}/{calendar.GetMonth(date):00}/{calendar.GetDayOfMonth(date):00}";
        }

        private static int? ExtractLegacyTargetId(string details, string targetName)
        {
            if (string.IsNullOrEmpty(details)) return null;
            var expression = targetName == nameof(Quotation)
                ? @"(?:QuotationIds?|ID)=(\d+)"
                : targetName == nameof(Invoice)
                    ? @"(?:InvoiceIds?|ID)=(\d+)"
                    : @"(?:PartyId|ID)=(\d+)";
            var match = Regex.Match(details, expression);
            return match.Success && int.TryParse(match.Groups[1].Value, out var id)
                ? id : null;
        }

        private async Task<List<MappingReportRow>> EnrichMappingEntriesAsync(
            List<MappingReportEntry> entries, CancellationToken ct)
        {
            var pfIds = entries.Where(x => x.Kind == nameof(PreFactor))
                .Select(x => x.EntityId).Distinct().ToArray();
            var factorIds = entries.Where(x => x.Kind == nameof(Factor))
                .Select(x => x.EntityId).Distinct().ToArray();
            var customerIds = entries.Where(x => x.Kind == nameof(Customer))
                .Select(x => x.EntityId).Distinct().ToArray();

            var crmSides = new Dictionary<(string, Guid), MappingReportSide>();
            foreach (var batch in pfIds.Chunk(350))
            {
                var data = await db.PreFactors.AsNoTracking()
                    .Where(x => batch.Contains(x.Id))
                    .Select(x => new { x.Id, x.SerialNumber, x.Date, x.PaymentAmount, x.Customer, x.CustomerId })
                    .ToListAsync(ct);
                foreach (var x in data)
                    crmSides[(nameof(PreFactor), x.Id)] = new MappingReportSide(
                        "پیش‌فاکتور CRM", x.Id.ToString(), x.SerialNumber.ToString(),
                        x.Date, x.PaymentAmount, x.Customer, $"CustomerId: {x.CustomerId}");
            }

            foreach (var batch in factorIds.Chunk(350))
            {
                var data = await db.Factors.AsNoTracking()
                    .Where(x => batch.Contains(x.Id))
                    .Select(x => new { x.Id, x.SerialNumber, x.Date, x.PaymentAmount,
                        x.Customer, x.CustomerId, x.SepidarId, x.SnFinancial })
                    .ToListAsync(ct);
                foreach (var x in data)
                    crmSides[(nameof(Factor), x.Id)] = new MappingReportSide(
                        "فاکتور CRM", x.Id.ToString(), x.SerialNumber.ToString(),
                        x.Date, x.PaymentAmount, x.Customer,
                        $"CustomerId: {x.CustomerId} | SepidarId: {x.SepidarId} | شماره مالی: {x.SnFinancial}");
            }

            foreach (var batch in customerIds.Chunk(350))
            {
                var data = await db.Customers.AsNoTracking()
                    .Where(x => batch.Contains(x.Id))
                    .Select(x => new { x.Id, x.CustomerCode, x.CompanyName, x.AgentName, x.FinancialCustomerId })
                    .ToListAsync(ct);
                foreach (var x in data)
                    crmSides[(nameof(Customer), x.Id)] = new MappingReportSide(
                        "مشتری CRM", x.Id.ToString(), x.CustomerCode.ToString(),
                        "", null, $"{x.CompanyName} {x.AgentName}".Trim(),
                        $"FinancialCustomerId: {x.FinancialCustomerId}");
            }

            var externalSides = new Dictionary<(string, int), MappingReportSide>();
            var quotationIds = entries.Where(x => x.TargetEntity == nameof(Quotation) && x.TargetId > 0)
                .Select(x => x.TargetId!.Value).Distinct().ToArray();
            var invoiceIds = entries.Where(x => x.TargetEntity == nameof(Invoice) && x.TargetId > 0)
                .Select(x => x.TargetId!.Value).Distinct().ToArray();
            var partyIds = entries.Where(x => x.TargetEntity == nameof(Party) && x.TargetId > 0)
                .Select(x => x.TargetId!.Value).Distinct().ToArray();

            foreach (var batch in quotationIds.Chunk(350))
            {
                var data = await sdb.Quotations.AsNoTracking()
                    .Where(x => batch.Contains(x.QuotationId))
                    .Select(x => new { x.QuotationId, x.Number, x.Date,
                        x.NetPriceInBaseCurrency, x.CustomerRealName, x.CustomerPartyRef, x.FiscalYearRef })
                    .ToListAsync(ct);
                foreach (var x in data)
                    externalSides[(nameof(Quotation), x.QuotationId)] = new MappingReportSide(
                        "پیش‌فاکتور سپیدار ۱", x.QuotationId.ToString(), x.Number.ToString(),
                        FormatLegacyPersianDate(x.Date), x.NetPriceInBaseCurrency,
                        x.CustomerRealName, $"PartyId: {x.CustomerPartyRef} | FiscalYearRef: {x.FiscalYearRef}");
            }

            foreach (var batch in invoiceIds.Chunk(350))
            {
                var data = await sdb.SalesInvoices.AsNoTracking()
                    .Where(x => batch.Contains(x.InvoiceId))
                    .Select(x => new { x.InvoiceId, x.Number, x.Date,
                        x.NetPriceInBaseCurrency, x.CustomerRealName, x.CustomerPartyRef, x.FiscalYearRef })
                    .ToListAsync(ct);
                foreach (var x in data)
                    externalSides[(nameof(Invoice), x.InvoiceId)] = new MappingReportSide(
                        "فاکتور سپیدار ۱", x.InvoiceId.ToString(), x.Number.ToString(),
                        FormatLegacyPersianDate(x.Date), x.NetPriceInBaseCurrency,
                        x.CustomerRealName, $"PartyId: {x.CustomerPartyRef} | FiscalYearRef: {x.FiscalYearRef}");
            }

            foreach (var batch in partyIds.Chunk(350))
            {
                var data = await sdb.Parties.AsNoTracking()
                    .Where(x => batch.Contains(x.PartyId))
                    .Select(x => new { x.PartyId, x.Name, x.LastName, x.IsCustomer })
                    .ToListAsync(ct);
                foreach (var x in data)
                    externalSides[(nameof(Party), x.PartyId)] = new MappingReportSide(
                        "مشتری سپیدار ۱", x.PartyId.ToString(), x.PartyId.ToString(),
                        "", null, $"{x.Name} {x.LastName}".Trim(),
                        x.IsCustomer ? "مشتری فعال در فهرست اشخاص" : "این شخص در سپیدار به‌عنوان مشتری علامت‌گذاری نشده");
            }

            return entries.Select(entry => {
                var crm = crmSides.GetValueOrDefault((entry.Kind, entry.EntityId))
                    ?? new MappingReportSide("رکورد CRM پیدا نشد", entry.EntityId.ToString(),
                        "-", "", null, "", "");
                var sepidar = entry.TargetId.HasValue
                    ? externalSides.GetValueOrDefault((entry.TargetEntity, entry.TargetId.Value))
                    : null;
                sepidar ??= new MappingReportSide(
                    entry.TargetId.HasValue ? "رکورد سپیدار پیدا نشد" : "معادل قطعی مشخص نیست",
                    entry.TargetId?.ToString() ?? "-", "-", "", null, "",
                    "برای این وضعیت یک طرف قطعی قابل نمایش نیست؛ جزئیات علت را بررسی کنید.");

                return new MappingReportRow(entry.Key, entry.Kind, entry.Status,
                    entry.Reason, entry.Details, entry.Sources, crm, sepidar);
            }).ToList();
        }
    }
}
