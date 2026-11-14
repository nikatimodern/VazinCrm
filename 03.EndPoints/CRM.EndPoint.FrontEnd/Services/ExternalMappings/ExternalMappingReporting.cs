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
        private sealed record MappingReportSide(
            string Title, string Id, string Number, string Date,
            decimal? Amount, string Customer, string Extra);

        private sealed record MappingReportEntry(
            string Key, string Kind, Guid EntityId, string Status,
            string Reason, string Details, string TargetEntity, int? TargetId,
            string[] Sources, int MapType, string TargetRaw = null,
            int[] CandidateIds = null, Guid? MappingId = null);

        private sealed record MappingReportRow(
            string Key, string Kind, string Status, string Reason,
            string Details, string[] Sources,
            MappingReportSide Crm, MappingReportSide Sepidar, int MapType,
            List<MappingReportTargetOption> Options, Guid? MappingId);

        private sealed record MappingReportTargetOption(int Id, string Title, string Description);



        public async Task<object> GetExternalMappingReview(Guid softwareId, CancellationToken ct)
        {
            // Compute a fresh preview for the chosen software. Never INSERT from report.
            var plan = await BuildLegacySepidarMappingPlanAsync(softwareId, ct);
            var sepidar = softwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb : (SepidarContextDbBase)s2Db;
            var entries = new List<MappingReportEntry>();

            // Render references in business language: which CRM customer belongs to
            // which Sepidar invoice/quotation and which Sepidar customer it uses.
            var evidence = new List<(Guid CustomerId, string Type, int ExternalId)>();
            foreach (var conflict in plan.CustomerConflicts)
            {
                foreach (var source in conflict.SourceDocuments)
                {
                    var crmMatch = Regex.Match(source, @"CustomerId=([0-9a-fA-F-]{36})");
                    var crmId = crmMatch.Success && Guid.TryParse(crmMatch.Groups[1].Value, out var parsed)
                        ? parsed : conflict.CustomerId;
                    foreach (Match m in Regex.Matches(source, @"(Quotation|Invoice):(\d+)"))
                        if (int.TryParse(m.Groups[2].Value, out var documentId))
                            evidence.Add((crmId, m.Groups[1].Value, documentId));
                }
            }

            // Resolve who already owns a conflicting Sepidar customer mapping.
            // Names/codes come from actual CRM Customers; no ownership is inferred.
            var conflictPartyReferences = plan.CustomerConflicts
                .SelectMany(x => x.SepidarPartyIds)
                .Where(x => x > 0).Distinct()
                .Select(x => x.ToString()).ToArray();
            var savedPartyOwners = new List<(int PartyId, Guid CustomerId)>();
            foreach (var batch in conflictPartyReferences.Chunk(350))
            {
                var mappings = await db.ExternalMappings.AsNoTracking()
                    .Where(x => x.ExternalSoftwareId == softwareId
                        && x.MapType == ExternalMappingTypeEnum.مشتری
                        && x.ExEntityName == nameof(Party)
                        && batch.Contains(x.ExEntityId))
                    .Select(x => new { x.ExEntityId, x.EntityId }).ToListAsync(ct);
                foreach (var mapping in mappings)
                    if (int.TryParse(mapping.ExEntityId, out var id))
                        savedPartyOwners.Add((id, mapping.EntityId));
            }

            var relatedCustomerIds = evidence.Select(x => x.CustomerId)
                .Concat(plan.CustomerConflicts.Select(x => x.CustomerId))
                .Concat(savedPartyOwners.Select(x => x.CustomerId))
                .Distinct().ToArray();
            var crmCustomerNames = new Dictionary<Guid, (string Name, int Code)>();
            foreach (var batch in relatedCustomerIds.Chunk(350))
            {
                var rows = await db.Customers.AsNoTracking()
                    .Where(x => batch.Contains(x.Id))
                    .Select(x => new { x.Id, x.CustomerCode, x.CompanyName, x.AgentName })
                    .ToListAsync(ct);
                foreach (var row in rows)
                {
                    var name = $"{row.CompanyName} {row.AgentName}".Trim();
                    crmCustomerNames[row.Id] = (string.IsNullOrWhiteSpace(name)
                        ? $"مشتری کد {row.CustomerCode}" : name, row.CustomerCode);
                }
            }

            var quoteIds = evidence.Where(x => x.Type == "Quotation")
                .Select(x => x.ExternalId).Distinct().ToArray();
            var invoiceIds = evidence.Where(x => x.Type == "Invoice")
                .Select(x => x.ExternalId).Distinct().ToArray();

            var linkedDocuments = new Dictionary<(string Type, int Id),
                (int Number, int PartyId)>();
            foreach (var batch in quoteIds.Chunk(350))
            {
                var rows = await sepidar.Quotations.AsNoTracking()
                    .Where(x => batch.Contains(x.QuotationId))
                    .Select(x => new { x.QuotationId, x.Number, x.CustomerPartyRef })
                    .ToListAsync(ct);
                foreach (var row in rows)
                    linkedDocuments[("Quotation", row.QuotationId)] = (row.Number, row.CustomerPartyRef);
            }
            foreach (var batch in invoiceIds.Chunk(350))
            {
                var rows = await sepidar.SalesInvoices.AsNoTracking()
                    .Where(x => batch.Contains(x.InvoiceId))
                    .Select(x => new {
                        x.InvoiceId, Number = (int?)x.Number,
                        PartyId = (int?)x.CustomerPartyRef
                    }).ToListAsync(ct);
                foreach (var row in rows)
                    linkedDocuments[("Invoice", row.InvoiceId)] =
                        (row.Number ?? row.InvoiceId, row.PartyId ?? 0);
            }

            var candidatePartyIds = plan.CustomerConflicts
                .SelectMany(x => x.SepidarPartyIds)
                .Concat(linkedDocuments.Values.Select(x => x.PartyId))
                .Where(x => x > 0).Distinct().ToArray();
            var partyNames = new Dictionary<int, string>();
            foreach (var batch in candidatePartyIds.Chunk(350))
            {
                var parties = await sepidar.Parties.AsNoTracking()
                    .Where(x => batch.Contains(x.PartyId))
                    .Select(x => new { x.PartyId, x.Name, x.LastName }).ToListAsync(ct);
                foreach (var party in parties)
                    partyNames[party.PartyId] = $"{party.Name} {party.LastName}".Trim();
            }

            foreach (var conflict in plan.CustomerConflicts)
            {
                var related = evidence
                    .Where(x => x.CustomerId == conflict.CustomerId
                        || conflict.SourceDocuments.Any(source =>
                            source.Contains($"CustomerId={x.CustomerId}")))
                    .Distinct()
                    .Select(x => {
                        var crmName = crmCustomerNames.GetValueOrDefault(x.CustomerId);
                        var crmTitle = !string.IsNullOrWhiteSpace(crmName.Name)
                            ? $"{crmName.Name} (کد {crmName.Code})"
                            : $"شناسه مشتری CRM: {x.CustomerId}";
                        if (!linkedDocuments.TryGetValue((x.Type, x.ExternalId), out var document))
                            return $"مشتری CRM «{crmTitle}» ← { (x.Type == "Quotation" ? "پیش‌فاکتور" : "فاکتور") } سپیدار با شناسه {x.ExternalId} (پیدا نشد)";
                        var partyTitle = partyNames.GetValueOrDefault(document.PartyId, "نام ثبت نشده");
                        var typeTitle = x.Type == "Quotation" ? "پیش‌فاکتور" : "فاکتور";
                        return $"مشتری CRM «{crmTitle}» ← {typeTitle} سپیدار شماره {document.Number} ← مشتری سپیدار «{partyTitle}» (کد {document.PartyId})";
                    }).Distinct().ToArray();

                var partyDescriptions = conflict.SepidarPartyIds.Select(id =>
                    $"گزینه مشتری سپیدار: «{partyNames.GetValueOrDefault(id, "نام ثبت نشده")}» (کد {id})");

                var owners = savedPartyOwners
                    .Where(x => conflict.SepidarPartyIds.Contains(x.PartyId)
                        && x.CustomerId != conflict.CustomerId)
                    .Distinct()
                    .Select(x => {
                        var customerInfo = crmCustomerNames.GetValueOrDefault(x.CustomerId);
                        var owner = !string.IsNullOrWhiteSpace(customerInfo.Name)
                            ? $"«{customerInfo.Name}» (کد {customerInfo.Code})"
                            : $"با شناسه داخلی {x.CustomerId} (مشتری در CRM پیدا نشد)";
                        var partyName = partyNames.GetValueOrDefault(x.PartyId, "بدون نام");
                        return $"مشتری «{partyName}» در سپیدار (کد {x.PartyId}) قبلاً به مشتری CRM {owner} نگاشت شده است.";
                    }).ToArray();

                var conflictDetails = owners.Length > 0
                    ? string.Join("\n", owners)
                    : $"برای این مشتری CRM، {conflict.SepidarPartyIds.Count} معادل احتمالی در سپیدار وجود دارد.";
                var sources = related.Concat(partyDescriptions).ToArray();
                entries.Add(new MappingReportEntry(
                    $"customer-conflict-{conflict.CustomerId}", nameof(Customer),
                    conflict.CustomerId, "customerConflict", conflict.Reason,
                    conflictDetails,
                    nameof(Party), conflict.SepidarPartyIds.Count > 0 && conflict.SepidarPartyIds[0] > 0
                        ? conflict.SepidarPartyIds[0] : (int?)null,
                    sources, (int)ExternalMappingTypeEnum.مشتری, null,
                    conflict.SepidarPartyIds.ToArray()));
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
                    .Select(x => $"{(x.EntityName == nameof(PreFactor) ? "پیش‌فاکتور" : "فاکتور")} CRM شماره {x.SerialNumber} با مبلغ {x.PaymentAmount:N0} ریال")
                    .ToArray() ?? Array.Empty<string>();
                entries.Add(new MappingReportEntry(
                    $"issue-{kind}-{issue.EntityId}-{issue.Reason}", kind,
                    issue.EntityId, status, issue.Reason,
                    issue.Details, targetName, targetId, relatedDocuments,
                    kind == nameof(PreFactor) ? (int)ExternalMappingTypeEnum.پیش_فاکتور
                        : (int)ExternalMappingTypeEnum.فاکتور,
                    null, ExtractLegacyCandidateIds(issue.Details, targetName)));
            }

            foreach (var mapping in plan.Pending)
            {
                int.TryParse(mapping.ExEntityId, out var targetId);
                entries.Add(new MappingReportEntry(
                    $"pending-{mapping.EntityName}-{mapping.EntityId}", mapping.EntityName,
                    mapping.EntityId, "pending", "ReadyForReview",
                    "",
                    mapping.ExEntityName, targetId > 0 ? targetId : null, Array.Empty<string>(),
                    (int)mapping.MapType, mapping.ExEntityId,
                    targetId > 0 ? new[] { targetId } : Array.Empty<int>()));
            }

            entries.AddRange(await BuildUserBrokerSuggestionsAsync(softwareId, ct));
            var reportRows = await EnrichMappingEntriesAsync(entries, softwareId, ct);
            var saved = await db.ExternalMappings.AsNoTracking()
                .Where(x => x.ExternalSoftwareId == softwareId
                    && (x.MapType == ExternalMappingTypeEnum.مشتری
                        || x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                        || x.MapType == ExternalMappingTypeEnum.فاکتور
                        || x.MapType == ExternalMappingTypeEnum.واسط))
                .CountAsync(ct);

            return new
            {
                summary = new {
                    saved,
                    pendingCustomers = plan.PendingCustomers.Count,
                    pendingDocuments = plan.PendingDocuments.Count,
                    pendingUsers = reportRows.Count(x => x.Status == "pending" && x.MapType == (int)ExternalMappingTypeEnum.واسط),
                    customerConflicts = plan.CustomerConflicts.Count,
                    blockedDocuments = plan.BlockedDocumentCount,
                    unmatched = reportRows.Count(x => x.Status == "notMatched"),
                    ambiguous = plan.Ambiguous,
                    duplicateGroups = plan.DuplicateTargetGroups.Count
                },
                rows = reportRows
            };
        }

        public async Task<object> GetSavedExternalMappings(
            Guid softwareId, int? mapType = null, string search = "",
            int page = 1, int pageSize = 12, CancellationToken ct = default)
        {
            if (!await db.ExternalSoftwares.AsNoTracking().AnyAsync(x => x.Id == softwareId, ct))
                throw new ArgumentException("نرم‌افزار انتخاب‌شده معتبر نیست.");

            if (mapType.HasValue && !Enum.IsDefined(typeof(ExternalMappingTypeEnum), mapType.Value))
                throw new ArgumentException("نوع نگاشت معتبر نیست.");

            page = Math.Max(1, page);
            // pageSize=0 requests all saved mappings for the selected software/filter.
            if (pageSize != 0)
                pageSize = Math.Clamp(pageSize, 5, 40);
            search = (search ?? "").Trim();
            if (search.Length > 80) search = search[..80];

            var query = db.ExternalMappings.AsNoTracking()
                .Where(x => x.ExternalSoftwareId == softwareId);
            if (mapType.HasValue)
                query = query.Where(x => x.MapType == (ExternalMappingTypeEnum)mapType.Value);

            if (search.Length > 0)
            {
                var isNumber = int.TryParse(search, out var number);
                var matchedIds = new HashSet<Guid>();

                var customers = await db.Customers.AsNoTracking()
                    .Where(x => (x.CompanyName != null && x.CompanyName.Contains(search))
                        || (x.AgentName != null && x.AgentName.Contains(search))
                        || (isNumber && x.CustomerCode == number))
                    .Select(x => x.Id).Take(500).ToListAsync(ct);
                matchedIds.UnionWith(customers);

                var preFactors = await db.PreFactors.AsNoTracking()
                    .Where(x => (x.Customer != null && x.Customer.Contains(search))
                        || (isNumber && x.SerialNumber == number))
                    .Select(x => x.Id).Take(500).ToListAsync(ct);
                matchedIds.UnionWith(preFactors);

                var factors = await db.Factors.AsNoTracking()
                    .Where(x => (x.Customer != null && x.Customer.Contains(search))
                        || (isNumber && x.SerialNumber == number))
                    .Select(x => x.Id).Take(500).ToListAsync(ct);
                matchedIds.UnionWith(factors);

                var cashes = await db.Cashes.AsNoTracking()
                    .Where(x => (x.CostDescription != null && x.CostDescription.Contains(search))
                        || (isNumber && x.Serial == number))
                    .Select(x => x.Id).Take(500).ToListAsync(ct);
                matchedIds.UnionWith(cashes);

                var centers = await db.BaseCenterCashes.AsNoTracking()
                    .Where(x => (x.Title != null && x.Title.Contains(search))
                        || (x.Code != null && x.Code.Contains(search)))
                    .Select(x => x.Id).Take(500).ToListAsync(ct);
                matchedIds.UnionWith(centers);

                // Search must also find broker mappings by the CRM employee's name.
                var employees = await userManager.Users.AsNoTracking()
                    .Where(x => (x.FirstName != null && x.FirstName.Contains(search))
                        || (x.LastName != null && x.LastName.Contains(search))
                        || (x.UserName != null && x.UserName.Contains(search)))
                    .Select(x => x.Id).Take(500).ToListAsync(ct);
                foreach (var userId in employees)
                    if (Guid.TryParse(userId, out var parsedUserId))
                        matchedIds.Add(parsedUserId);

                var ids = matchedIds.ToArray();
                query = query.Where(x => x.ExEntityId.Contains(search)
                    || x.EntityId.ToString().Contains(search)
                    || ids.Contains(x.EntityId));
            }

            var total = await query.CountAsync(ct);
            var ordered = query.OrderByDescending(x => x.CreateDate)
                .ThenBy(x => x.EntityId);
            var selected = pageSize == 0 ? ordered
                : ordered.Skip((page - 1) * pageSize).Take(pageSize);
            var mappings = await selected.Select(x => new {
                    x.Id, x.EntityName, x.EntityId, x.MapType, x.ExEntityName, x.ExEntityId
                }).ToListAsync(ct);

            var entries = mappings.Select(x => {
                int.TryParse(x.ExEntityId, out var externalId);
                return new MappingReportEntry(
                    $"saved-{x.EntityName}-{x.EntityId}-{(int)x.MapType}", x.EntityName,
                    x.EntityId, "saved", "AlreadyMapped",
                    "این نگاشت در پایگاه داده CRM ثبت شده است.",
                    x.ExEntityName, externalId > 0 ? externalId : null,
                    Array.Empty<string>(), (int)x.MapType, x.ExEntityId,
                    null, x.Id);
            }).ToList();

            return new {
                total, page, pageSize,
                rows = await EnrichMappingEntriesAsync(entries, softwareId, ct)
            };
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

        private static int[] ExtractLegacyCandidateIds(string details, string targetName)
        {
            if (string.IsNullOrWhiteSpace(details)) return Array.Empty<int>();
            var prefix = targetName == nameof(Quotation) ? "QuotationIds?"
                : targetName == nameof(Invoice) ? "InvoiceIds?" : "PartyIds?";
            var pattern = @"(?:" + prefix + @"|ID)=(\d+(?:\s*,\s*\d+)*)";
            return Regex.Matches(details, pattern).Cast<Match>()
                .SelectMany(x => Regex.Matches(x.Groups[1].Value, @"\d+").Cast<Match>())
                .Select(x => int.TryParse(x.Value, out var id) ? id : 0)
                .Where(x => x > 0).Distinct().ToArray();
        }

        private async Task<List<MappingReportRow>> EnrichMappingEntriesAsync(
            List<MappingReportEntry> entries, Guid softwareId, CancellationToken ct)
        {
            var pfIds = entries.Where(x => x.Kind == nameof(PreFactor))
                .Select(x => x.EntityId).Distinct().ToArray();
            var factorIds = entries.Where(x => x.Kind == nameof(Factor))
                .Select(x => x.EntityId).Distinct().ToArray();
            var customerIds = entries.Where(x => x.Kind == nameof(Customer))
                .Select(x => x.EntityId).Distinct().ToArray();
            var userIds = entries.Where(x => x.Kind == nameof(AppUser))
                .Select(x => x.EntityId).Distinct().ToArray();

            var crmSides = new Dictionary<(string, Guid), MappingReportSide>();
            foreach (var batch in userIds.Chunk(350))
            {
                var ids = batch.Select(x => x.ToString()).ToArray();
                var users = await userManager.Users.AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => new { x.Id, x.FirstName, x.LastName, x.UserName, x.IsEmployee })
                    .ToListAsync(ct);
                foreach (var x in users)
                    if (Guid.TryParse(x.Id, out var userId))
                    {
                        var name = (x.FirstName + " " + x.LastName).Trim();
                        if (string.IsNullOrEmpty(name)) name = x.UserName ?? "کاربر بدون نام";
                        crmSides[(nameof(AppUser), userId)] = new MappingReportSide(
                            "کاربر CRM", userId.ToString(), x.UserName ?? "—", "",
                            null, name, x.IsEmployee ? "کارمند داخلی" : "کاربر CRM");
                    }
            }
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
                {
                    var customerName = $"{x.CompanyName} {x.AgentName}".Trim();
                    if (string.IsNullOrWhiteSpace(customerName))
                        customerName = $"مشتری با کد {x.CustomerCode}";

                    crmSides[(nameof(Customer), x.Id)] = new MappingReportSide(
                        "مشتری CRM", x.Id.ToString(), x.CustomerCode.ToString(),
                        "", null, customerName,
                        $"FinancialCustomerId: {x.FinancialCustomerId}");
                }
            }

            // All map types can be viewed, including cash, accounting and cost centers.
            var cashIds = entries.Where(x => x.Kind == nameof(Cash))
                .Select(x => x.EntityId).Distinct().ToArray();
            foreach (var batch in cashIds.Chunk(350))
            {
                var rows = await db.Cashes.AsNoTracking()
                    .Where(x => batch.Contains(x.Id))
                    .Select(x => new { x.Id, x.Serial, x.CostDate, x.Amount,
                        x.CostCenterTxt, x.CostTitleTxt, x.CostDescription })
                    .ToListAsync(ct);
                foreach (var x in rows)
                    crmSides[(nameof(Cash), x.Id)] = new MappingReportSide(
                        "تنخواه CRM", x.Id.ToString(), x.Serial.ToString(), x.CostDate,
                        x.Amount, x.CostTitleTxt ?? x.CostDescription,
                        "مرکز هزینه: " + (x.CostCenterTxt ?? "ثبت نشده"));
            }

            var centerIds = entries.Where(x => x.Kind == nameof(BaseCenterCash))
                .Select(x => x.EntityId).Distinct().ToArray();
            foreach (var batch in centerIds.Chunk(350))
            {
                var rows = await db.BaseCenterCashes.AsNoTracking()
                    .Where(x => batch.Contains(x.Id))
                    .Select(x => new { x.Id, x.Code, x.Title }).ToListAsync(ct);
                foreach (var x in rows)
                    crmSides[(nameof(BaseCenterCash), x.Id)] = new MappingReportSide(
                        "مرکز هزینه CRM", x.Id.ToString(), x.Code, "",
                        null, x.Title, "");
            }

            var accountingIds = entries.Where(x => x.Kind == nameof(Accountancy))
                .Select(x => x.EntityId).Distinct().ToArray();
            foreach (var batch in accountingIds.Chunk(350))
            {
                var rows = await db.Accountancies.AsNoTracking()
                    .Where(x => batch.Contains(x.Id))
                    .Select(x => new { x.Id, x.fSerialNumber, x.pfSerialNumber,
                        x.dateSh, x.credit, x.debit, x.CustomerName })
                    .ToListAsync(ct);
                foreach (var x in rows)
                    crmSides[(nameof(Accountancy), x.Id)] = new MappingReportSide(
                        "حسابداری CRM", x.Id.ToString(),
                        (x.fSerialNumber ?? x.pfSerialNumber)?.ToString() ?? "-",
                        x.dateSh, x.credit ?? x.debit, x.CustomerName, "");
            }

            var externalSides = new Dictionary<(string, int), MappingReportSide>();
            var softwareTitle = await db.ExternalSoftwares.AsNoTracking()
                .Where(x => x.Id == softwareId)
                .Select(x => x.Title).FirstOrDefaultAsync(ct) ?? "نرم‌افزار خارجی";
            var sepidarContext = softwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb
                : softwareId == Sepidar2SoftwareId ? (SepidarContextDbBase)s2Db : null;
            var quotationIds = entries.Where(x => x.TargetEntity == nameof(Quotation))
                .SelectMany(x => x.CandidateIds ?? (x.TargetId.HasValue
                    ? new[] { x.TargetId.Value } : Array.Empty<int>()))
                .Where(x => x > 0).Distinct().ToArray();
            var invoiceIds = entries.Where(x => x.TargetEntity == nameof(Invoice))
                .SelectMany(x => x.CandidateIds ?? (x.TargetId.HasValue
                    ? new[] { x.TargetId.Value } : Array.Empty<int>()))
                .Where(x => x > 0).Distinct().ToArray();
            var partyIds = entries.Where(x => x.TargetEntity == nameof(Party))
                .SelectMany(x => x.CandidateIds ?? (x.TargetId.HasValue
                    ? new[] { x.TargetId.Value } : Array.Empty<int>()))
                .Where(x => x > 0).Distinct().ToArray();

            if (sepidarContext != null)
            {
            foreach (var batch in quotationIds.Chunk(350))
            {
                var data = await sepidarContext.Quotations.AsNoTracking()
                    .Where(x => batch.Contains(x.QuotationId))
                    .Select(x => new { x.QuotationId, x.Number, x.Date,
                        x.NetPriceInBaseCurrency, x.CustomerRealName, x.CustomerPartyRef, x.FiscalYearRef })
                    .ToListAsync(ct);
                foreach (var x in data)
                    externalSides[(nameof(Quotation), x.QuotationId)] = new MappingReportSide(
                        "پیش‌فاکتور " + softwareTitle, x.QuotationId.ToString(), x.Number.ToString(),
                        FormatLegacyPersianDate(x.Date), x.NetPriceInBaseCurrency,
                        x.CustomerRealName, $"PartyId: {x.CustomerPartyRef} | FiscalYearRef: {x.FiscalYearRef}");
            }

            foreach (var batch in invoiceIds.Chunk(350))
            {
                var data = await sepidarContext.SalesInvoices.AsNoTracking()
                    .Where(x => batch.Contains(x.InvoiceId))
                    .Select(x => new {
                        x.InvoiceId,
                        Number = (int?)x.Number,
                        Date = (DateTime?)x.Date,
                        Amount = (decimal?)x.NetPriceInBaseCurrency,
                        x.CustomerRealName,
                        PartyId = (int?)x.CustomerPartyRef,
                        FiscalYearId = (int?)x.FiscalYearRef
                    })
                    .ToListAsync(ct);
                foreach (var x in data)
                    externalSides[(nameof(Invoice), x.InvoiceId)] = new MappingReportSide(
                        "فاکتور " + softwareTitle, x.InvoiceId.ToString(), x.Number?.ToString() ?? "-",
                        x.Date.HasValue ? FormatLegacyPersianDate(x.Date.Value) : "-",
                        x.Amount, x.CustomerRealName,
                        $"PartyId: {x.PartyId} | FiscalYearRef: {x.FiscalYearId}");
            }

            foreach (var batch in partyIds.Chunk(350))
            {
                var data = await sepidarContext.Parties.AsNoTracking()
                    .Where(x => batch.Contains(x.PartyId))
                    .Select(x => new { x.PartyId, x.Name, x.LastName, x.IsCustomer,
                        x.IsBroker, x.CommissionRate, DlCode = x.Dl.Code })
                    .ToListAsync(ct);
                foreach (var x in data)
                    externalSides[(nameof(Party), x.PartyId)] = new MappingReportSide(
                        (x.IsBroker ? "واسط " : "مشتری ") + softwareTitle,
                        x.PartyId.ToString(), x.PartyId.ToString(), "",
                        null, $"{x.Name} {x.LastName}".Trim(),
                        x.IsBroker ? $"کد تفصیلی: {x.DlCode} | نرخ پورسانت: {x.CommissionRate}%"
                            : x.IsCustomer ? "مشتری فعال در فهرست اشخاص"
                            : "این شخص در سپیدار به‌عنوان مشتری علامت‌گذاری نشده");
            }

                // Enrich the other saved Sepidar mapping types when their
                // external entity names are known, without assuming custom schemas.
                var vouchers = entries.Where(x => x.TargetEntity == nameof(Voucher) && x.TargetId > 0)
                    .Select(x => x.TargetId.Value).Distinct().ToArray();
                foreach (var batch in vouchers.Chunk(350))
                {
                    var rows = await sepidarContext.Vouchers.AsNoTracking()
                        .Where(x => batch.Contains(x.VoucherId))
                        .Select(x => new { x.VoucherId, x.Number, x.Date, x.Description, x.FiscalYearRef })
                        .ToListAsync(ct);
                    foreach (var x in rows)
                        externalSides[(nameof(Voucher), x.VoucherId)] = new MappingReportSide(
                            "سند حسابداری نرم‌افزار مالی", x.VoucherId.ToString(),
                            x.Number.ToString(), FormatLegacyPersianDate(x.Date), null,
                            x.Description, $"سال مالی: {x.FiscalYearRef}");
                }

                var payments = entries.Where(x => x.TargetEntity == nameof(PaymentHeader) && x.TargetId > 0)
                    .Select(x => x.TargetId.Value).Distinct().ToArray();
                foreach (var batch in payments.Chunk(350))
                {
                    var rows = await sepidarContext.PaymentHeaders.AsNoTracking()
                        .Where(x => batch.Contains(x.PaymentHeaderId))
                        .Select(x => new { x.PaymentHeaderId, x.Number, x.Date,
                            x.AmountInBaseCurrency, x.Description, x.FiscalYearRef })
                        .ToListAsync(ct);
                    foreach (var x in rows)
                        externalSides[(nameof(PaymentHeader), x.PaymentHeaderId)] = new MappingReportSide(
                            "اعلامیه پرداخت نرم‌افزار مالی", x.PaymentHeaderId.ToString(),
                            x.Number.ToString(), FormatLegacyPersianDate(x.Date),
                            x.AmountInBaseCurrency, x.Description, $"سال مالی: {x.FiscalYearRef}");
                }

                var dlIds = entries.Where(x => x.TargetEntity == nameof(DL) && x.TargetId > 0)
                    .Select(x => x.TargetId.Value).Distinct().ToArray();
                foreach (var batch in dlIds.Chunk(350))
                {
                    var rows = await sepidarContext.DLs.AsNoTracking()
                        .Where(x => batch.Contains(x.DLId))
                        .Select(x => new { x.DLId, x.Code, x.Title }).ToListAsync(ct);
                    foreach (var x in rows)
                        externalSides[(nameof(DL), x.DLId)] = new MappingReportSide(
                            "حساب تفصیلی نرم‌افزار مالی", x.DLId.ToString(),
                            x.Code, "", null, x.Title, "");
                }

                var cashIdsInSepidar = entries.Where(x => (x.TargetEntity == "Cash"
                        || x.TargetEntity == "SepidarCash") && x.TargetId > 0)
                    .Select(x => x.TargetId.Value).Distinct().ToArray();
                foreach (var batch in cashIdsInSepidar.Chunk(350))
                {
                    var rows = await sepidarContext.SepidarCash.AsNoTracking()
                        .Where(x => batch.Contains(x.CashId))
                        .Select(x => new { x.CashId, x.Title }).ToListAsync(ct);
                    foreach (var x in rows)
                    {
                        var side = new MappingReportSide(
                            "تنخواه نرم‌افزار مالی", x.CashId.ToString(),
                            x.CashId.ToString(), "", null, x.Title, "");
                        externalSides[("Cash", x.CashId)] = side;
                        externalSides[("SepidarCash", x.CashId)] = side;
                    }
                }
            }

            return entries.Select(entry => {
                var crm = crmSides.GetValueOrDefault((entry.Kind, entry.EntityId))
                    ?? new MappingReportSide("رکورد CRM پیدا نشد", entry.EntityId.ToString(),
                        "-", "", null, "", "");
                var sepidar = entry.TargetId.HasValue
                    ? externalSides.GetValueOrDefault((entry.TargetEntity, entry.TargetId.Value))
                    : null;
                sepidar ??= new MappingReportSide(
                    entry.Status == "saved" ? "نرم‌افزار مقصد" : "معادل قطعی مشخص نیست",
                    entry.TargetRaw ?? entry.TargetId?.ToString() ?? "-",
                    entry.TargetRaw ?? entry.TargetId?.ToString() ?? "-",
                    "", null, "", entry.Status == "saved"
                        ? "شناسه ثبت‌شده در نرم‌افزار انتخاب‌شده"
                        : "برای این وضعیت معادل قطعی مشخص نیست.");

                var options = (entry.CandidateIds ?? Array.Empty<int>())
                    .Distinct().Where(id => id > 0)
                    .Select(id => {
                        var candidate = externalSides.GetValueOrDefault((entry.TargetEntity, id));
                        var itemType = entry.TargetEntity == nameof(Quotation) ? "پیش‌فاکتور"
                            : entry.TargetEntity == nameof(Invoice) ? "فاکتور"
                            : entry.MapType == (int)ExternalMappingTypeEnum.واسط ? "واسط" : "مشتری";
                        var readableTitle = candidate == null
                            ? $"{itemType} با شناسه {id}"
                            : entry.TargetEntity == nameof(Party)
                                ? candidate.Customer
                                : $"{itemType} شماره {candidate.Number} — {candidate.Customer}";
                        return new MappingReportTargetOption(id,
                            readableTitle,
                            candidate != null
                                ? $"شناسه {id} | شماره {candidate.Number}"
                                : $"شناسه {id}");
                    }).ToList();

                return new MappingReportRow(entry.Key, entry.Kind, entry.Status,
                    entry.Reason, entry.Details, entry.Sources, crm, sepidar,
                    entry.MapType, options, entry.MappingId);
            }).ToList();
        }
    }
}
