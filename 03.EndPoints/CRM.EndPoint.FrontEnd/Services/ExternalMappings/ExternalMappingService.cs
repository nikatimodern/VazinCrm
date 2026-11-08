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
    public sealed class ExternalMappingService
    {
        public sealed class MappingConflictException : Exception
        {
            public MappingConflictException(string message) : base(message) { }
        }

        private static readonly Guid Sepidar1SoftwareId =
            Guid.Parse("2f3d8024-6c2c-491f-c921-08df26b00138");
        private static readonly Guid Sepidar2SoftwareId =
            Guid.Parse("d6c3e8a7-1435-47d5-0489-08df26c5e2ca");

        private readonly CRMContextDb db;
        private readonly Sepidar1ContextDb sdb;
        private readonly Sepidar2ContextDb s2Db;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<ExternalMappingService> _logger;

        public ExternalMappingService(
            CRMContextDb db,
            Sepidar1ContextDb sdb,
            Sepidar2ContextDb s2Db,
            UserManager<AppUser> userManager,
            ILogger<ExternalMappingService> logger)
        {
            this.db = db;
            this.sdb = sdb;
            this.s2Db = s2Db;
            this.userManager = userManager;
            _logger = logger;
        }

        // One-off historical mapping migration draft. Private and intentionally uncalled.
        // Review first. No SQL reads or writes happen just by visiting Home/Index.
        private sealed record LegacyPreFactorRow(
            Guid Id, int SerialNumber, string Date, DateTime? DateMiladi,
            decimal SumTotalAmount, decimal PaymentAmount, Guid? CustomerId);

        private sealed record LegacyFactorRow(
            Guid Id, int SerialNumber, Guid PfactorId, int? SepidarId, string SnFinancial,
            string Date, DateTime? DateMiladi, decimal PaymentAmount, Guid? CustomerId);

        private sealed record LegacyQuotationRow(
            int Id, int Number, int FiscalYearRef, decimal Price,
            decimal NetPrice, int CustomerPartyRef);

        private sealed record LegacyInvoiceRow(
            int Id, int? Number, int? QuotationRef, int FiscalYearRef,
            decimal? NetPrice, int? CustomerPartyRef);

        private sealed record LegacyFiscalYearRow(
            int Year, DateTime StartDate, DateTime EndDate);

        private sealed record LegacyCustomerRow(
            Guid Id, string CompanyName, int? FinancialCustomerId);

        private sealed record LegacyCustomerEvidence(
            Guid CustomerId, int SepidarPartyId, string SourceDocument);

        private sealed record LegacyCustomerConflict(
            Guid CustomerId, string CompanyName, string Reason,
            List<int> SepidarPartyIds, List<string> SourceDocuments);

        private static int? GetLegacyFiscalYear(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 4)
                return null;

            Span<char> digits = stackalloc char[4];
            for (var i = 0; i < digits.Length; i++)
            {
                var number = char.GetNumericValue(value[i]);
                if (number < 0 || number > 9 || number != Math.Floor(number))
                    return null;

                digits[i] = (char)('0' + (int)number);
            }

            return int.TryParse(digits, out var year) ? year : null;
        }

        private static bool IsInLegacyFiscalYear(
            string crmPersianDate, DateTime? crmMiladiDate, LegacyFiscalYearRow fiscalYear)
        {
            // Prefer Persian financial year (e.g. 1405); do NOT compare exact dates.
            var crmYear = GetLegacyFiscalYear(crmPersianDate);
            if (crmYear.HasValue)
                return crmYear.Value == fiscalYear.Year;

            // Fallback only when the CRM Persian date/year is unavailable.
            return crmMiladiDate.HasValue
                && crmMiladiDate.Value.Date >= fiscalYear.StartDate.Date
                && crmMiladiDate.Value.Date <= fiscalYear.EndDate.Date;
        }

        // Read-only diagnostics for failed historical matches. Expand plan.Issues in the debugger.
        private sealed record LegacyMappingIssue(
            string EntityName, Guid EntityId, string CrmNumber,
            string Status, string Reason, string Details);

        private sealed record LegacyDuplicateCrmDocument(
            string EntityName, Guid EntityId, int SerialNumber,
            string Date, decimal PaymentAmount);

        private sealed record LegacyDuplicateTargetGroup(
            Guid ExternalSoftwareId, string ExEntityName, string ExEntityId,
            ExternalMappingTypeEnum MapType, List<LegacyDuplicateCrmDocument> CrmDocuments);

        private sealed record LegacyMappingPlan(
            int AlreadyMapped, int NotMatched, int Ambiguous,
            List<ExternalMapping> Pending, List<LegacyMappingIssue> Issues,
            List<LegacyDuplicateTargetGroup> DuplicateTargetGroups,
            List<LegacyCustomerConflict> CustomerConflicts,
            int AlreadyMappedCustomers)
        {
            public List<ExternalMapping> PendingCustomers => Pending
                .Where(x => x.MapType == ExternalMappingTypeEnum.مشتری).ToList();

            public List<ExternalMapping> PendingDocuments => Pending
                .Where(x => x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                    || x.MapType == ExternalMappingTypeEnum.فاکتور).ToList();

            public List<LegacyMappingIssue> BlockedDocuments => Issues
                .Where(x => x.Status == "BlockedByCustomer").ToList();

            public int BlockedDocumentCount => BlockedDocuments.Count;

            public Dictionary<string, int> CustomerConflictSummary => CustomerConflicts
                .GroupBy(x => x.Reason)
                .ToDictionary(x => x.Key, x => x.Count());

            public Dictionary<string, int> ReasonsSummary => Issues
                .GroupBy(x => x.Reason)
                .ToDictionary(x => x.Key, x => x.Count());

            public int PreFactorsWithoutMatch => Issues.Count(x =>
                x.EntityName == nameof(PreFactor) && x.Status == "NotMatched");
            public int FactorsWithoutMatch => Issues.Count(x =>
                x.EntityName == nameof(Factor) && x.Status == "NotMatched");
        }

        private async Task<LegacyMappingPlan> BuildLegacySepidarMappingPlanAsync(CancellationToken ct)
        {
            // This method only READS data. It never calls Add/Update/SaveChanges.
            var readStage = "CRM.PreFactors";
            try
            {
            var preFactors = await db.PreFactors.AsNoTracking()
                .Select(p => new LegacyPreFactorRow(
                    p.Id, p.SerialNumber, p.Date, p.DateMiladi,
                    p.SumTotalAmount, p.PaymentAmount, p.CustomerId))
                .ToListAsync(ct);

            readStage = "CRM.Factors";
            var factors = await db.Factors.AsNoTracking()
                .Where(f => f.status == FactorStatus.تایید_شده && f.ParentReturnedId == null)
                .Select(f => new LegacyFactorRow(
                    f.Id, f.SerialNumber, f.PfactorID, f.SepidarId, f.SnFinancial,
                    f.Date, f.DateMiladi, f.PaymentAmount, f.CustomerId))
                .ToListAsync(ct);

            readStage = "CRM.Customers";
            var customers = await db.Customers.AsNoTracking()
                .Select(c => new LegacyCustomerRow(c.Id, c.CompanyName, c.FinancialCustomerId))
                .ToListAsync(ct);
            var customersById = customers.ToDictionary(x => x.Id);
            var financialCustomerIds = customers
                .Where(x => x.FinancialCustomerId.HasValue)
                .ToDictionary(x => x.Id, x => x.FinancialCustomerId!.Value);

            readStage = "CRM.ExternalMappings";
            var existing = await db.ExternalMappings.AsNoTracking()
                .Where(m => m.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                         || m.MapType == ExternalMappingTypeEnum.فاکتور
                         || m.MapType == ExternalMappingTypeEnum.مشتری)
                .ToListAsync(ct);

            // A customer mapping explicitly confirmed in ExternalMapping takes priority
            // over the old FinancialCustomerId for matching Sepidar 1 records.
            foreach (var mappedCustomer in existing.Where(x =>
                x.ExternalSoftwareId == Sepidar1SoftwareId
                && x.MapType == ExternalMappingTypeEnum.مشتری
                && x.EntityName == nameof(Customer)
                && x.ExEntityName == nameof(Party)))
            {
                if (int.TryParse(mappedCustomer.ExEntityId, out var partyId) && partyId > 0)
                    financialCustomerIds[mappedCustomer.EntityId] = partyId;
            }

            var existingSources = existing
                .Select(m => (m.ExternalSoftwareId, m.EntityName, m.EntityId, m.MapType))
                .ToHashSet();
            var existingTargets = existing
                .Select(m => (m.ExternalSoftwareId, m.ExEntityName, m.ExEntityId))
                .ToHashSet();

            // Historical backfill is restricted to Sepidar 1 only.
            readStage = "CRM.ExternalSoftwares";
            var configuredSoftwareIds = await db.ExternalSoftwares.AsNoTracking()
                .Where(x => x.Id == Sepidar1SoftwareId)
                .Select(x => x.Id)
                .ToListAsync(ct);

            readStage = "Sepidar1.FMK.FiscalYear";
            var financialYears = await sdb.FiscalYears.AsNoTracking()
                .Select(x => new { x.FiscalYearId, x.Title, x.StartDate, x.EndDate })
                .ToListAsync(ct);

            var fiscalYears = financialYears
                .Where(x => GetLegacyFiscalYear(x.Title).HasValue)
                .ToDictionary(
                    x => x.FiscalYearId,
                    x => new LegacyFiscalYearRow(
                        GetLegacyFiscalYear(x.Title)!.Value, x.StartDate, x.EndDate));

            var pending = new List<ExternalMapping>();
            var issues = new List<LegacyMappingIssue>();
            // Groups only the CRM documents competing for the same Sepidar target.
            var duplicateTargetGroups = new List<LegacyDuplicateTargetGroup>();
            var customerConflicts = new List<LegacyCustomerConflict>();
            void AddIssue(string entityName, Guid id, string crmNumber,
                string status, string reason, string details)
            {
                issues.Add(new LegacyMappingIssue(entityName, id, crmNumber,
                    status, reason, details));
            }

            var alreadyMapped = 0;
            var notMatched = 0;
            var ambiguous = 0;

            void Propose(Guid softwareId, string entityName, Guid entityId,
                string crmNumber, ExternalMappingTypeEnum mapType, string externalName, int externalId)
            {
                if (existingSources.Contains((softwareId, entityName, entityId, mapType)))
                {
                    alreadyMapped++;
                    return;
                }

                if (existingTargets.Contains((softwareId, externalName, externalId.ToString())))
                {
                    ambiguous++;
                    AddIssue(entityName, entityId, crmNumber, "Ambiguous",
                        "ExternalTargetAlreadyMapped",
                        $"سپیدار {externalName} ID={externalId} قبلاً به سند دیگری متصل شده است.");
                    return;
                }

                pending.Add(new ExternalMapping {
                    Id = Guid.NewGuid(),
                    ExternalSoftwareId = softwareId,
                    EntityName = entityName,
                    EntityId = entityId,
                    MapType = mapType,
                    MapTypeTxt = mapType.ToString(),
                    ExEntityName = externalName,
                    ExEntityId = externalId.ToString(),
                    UserCreatorID = "legacy-migration",
                    UserCreator = "Legacy Sepidar mapping backfill"
                });
            }

            // Keep the conflicting CRM documents together for inspection in Debug.
            void RemoveAmbiguousProposals(ExternalMappingTypeEnum mapType)
            {
                var duplicateGroups = pending.Where(x => x.MapType == mapType)
                    .GroupBy(x => (x.ExternalSoftwareId, x.ExEntityName, x.ExEntityId))
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var group in duplicateGroups)
                {
                    var documents = group.Select(mapping => {
                        var preFactor = mapping.EntityName == nameof(PreFactor)
                            ? preFactors.FirstOrDefault(x => x.Id == mapping.EntityId) : null;
                        var factor = mapping.EntityName == nameof(Factor)
                            ? factors.FirstOrDefault(x => x.Id == mapping.EntityId) : null;

                        return new LegacyDuplicateCrmDocument(
                            mapping.EntityName, mapping.EntityId,
                            preFactor?.SerialNumber ?? factor?.SerialNumber ?? 0,
                            preFactor?.Date ?? factor?.Date,
                            preFactor?.PaymentAmount ?? factor?.PaymentAmount ?? 0);
                    }).ToList();

                    duplicateTargetGroups.Add(new LegacyDuplicateTargetGroup(
                        group.Key.ExternalSoftwareId, group.Key.ExEntityName,
                        group.Key.ExEntityId, mapType, documents));

                    foreach (var mapping in group)
                        AddIssue(mapping.EntityName, mapping.EntityId,
                            documents.First(x => x.EntityId == mapping.EntityId).SerialNumber.ToString(),
                            "Ambiguous", "DuplicateProposedTarget",
                            $"سپیدار: {mapping.ExEntityName} ID={mapping.ExEntityId}، تعداد اسناد متعارض CRM={documents.Count}");
                }

                var duplicateIds = duplicateGroups
                    .SelectMany(x => x.Select(mapping => mapping.Id))
                    .ToHashSet();

                ambiguous += duplicateIds.Count;
                pending.RemoveAll(x => duplicateIds.Contains(x.Id));
            }

            foreach (var softwareId in configuredSoftwareIds)
            {
                var sepidar = (SepidarContextDbBase)sdb;

                var numbers = preFactors
                    .Where(x => x.SerialNumber > 0)
                    .Select(x => x.SerialNumber).Distinct().ToArray();

                var quotations = new List<LegacyQuotationRow>();
                foreach (var batch in numbers.Chunk(400))
                {
                    readStage = "Sepidar1.SLS.Quotation";
                    quotations.AddRange(await sepidar.Quotations.AsNoTracking()
                        .Where(x => batch.Contains(x.Number))
                        .Select(x => new LegacyQuotationRow(
                            x.QuotationId, x.Number, x.FiscalYearRef,
                            x.Price, x.NetPriceInBaseCurrency, x.CustomerPartyRef))
                        .ToListAsync(ct));
                }

                var quoteByNumber = quotations.GroupBy(q => q.Number)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var pf in preFactors)
                {
                    if (existingSources.Contains((softwareId, nameof(PreFactor),
                            pf.Id, ExternalMappingTypeEnum.پیش_فاکتور)))
                    {
                        alreadyMapped++;
                        continue;
                    }

                    if (!quoteByNumber.TryGetValue(pf.SerialNumber, out var options))
                    {
                        notMatched++;
                        AddIssue(nameof(PreFactor), pf.Id, pf.SerialNumber.ToString(),
                            "NotMatched", "QuotationNumberNotFound",
                            $"هیچ پیش‌فاکتوری در سپیدار ۱ با Number={pf.SerialNumber} پیدا نشد.");
                        continue;
                    }

                    // Match fiscal year, serial number, amounts, and known customer.
                    // The registration day can differ between CRM and Sepidar.
                    var matches = options.Where(q =>
                        fiscalYears.TryGetValue(q.FiscalYearRef, out var financialYear)
                        && IsInLegacyFiscalYear(pf.Date, pf.DateMiladi, financialYear)
                        && q.Price == pf.SumTotalAmount
                        && q.NetPrice == pf.PaymentAmount
                        && (!pf.CustomerId.HasValue
                            || !financialCustomerIds.TryGetValue(pf.CustomerId.Value, out var partyId)
                            || q.CustomerPartyRef == partyId)).ToList();

                    if (matches.Count == 1)
                    {
                        Propose(softwareId, nameof(PreFactor), pf.Id,
                            pf.SerialNumber.ToString(), ExternalMappingTypeEnum.پیش_فاکتور,
                            nameof(Quotation), matches[0].Id);
                    }
                    else if (matches.Count == 0)
                    {
                        notMatched++;
                        var candidates = options.Select(q => {
                            var differences = new List<string>();
                            if (!fiscalYears.TryGetValue(q.FiscalYearRef, out var fiscalYear))
                                differences.Add($"سال مالی سپیدار با Ref={q.FiscalYearRef} شناخته نشده");
                            else if (!IsInLegacyFiscalYear(pf.Date, pf.DateMiladi, fiscalYear))
                                differences.Add($"سال مالی متفاوت: CRM={pf.Date}, Sepidar={fiscalYear.Year}");

                            if (q.Price != pf.SumTotalAmount)
                                differences.Add($"مبلغ ناخالص: CRM={pf.SumTotalAmount}, Sepidar={q.Price}");
                            if (q.NetPrice != pf.PaymentAmount)
                                differences.Add($"مبلغ نهایی: CRM={pf.PaymentAmount}, Sepidar={q.NetPrice}");
                            if (pf.CustomerId.HasValue
                                && financialCustomerIds.TryGetValue(pf.CustomerId.Value, out var partyId)
                                && q.CustomerPartyRef != partyId)
                                differences.Add($"مشتری: CRM SepidarPartyId={partyId}, Sepidar={q.CustomerPartyRef}");

                            return new { q.Id, Differences = differences };
                        }).ToList();

                        var reason = candidates.All(x => x.Differences.Any(d => d.StartsWith("سال مالی")))
                            ? "QuotationFiscalYearMismatch"
                            : candidates.All(x => x.Differences.Any(d => d.StartsWith("مبلغ")))
                                ? "QuotationAmountMismatch"
                                : candidates.All(x => x.Differences.Any(d => d.StartsWith("مشتری")))
                                    ? "QuotationCustomerMismatch"
                                    : "QuotationMultipleFieldMismatch";

                        AddIssue(nameof(PreFactor), pf.Id, pf.SerialNumber.ToString(),
                            "NotMatched", reason,
                            string.Join(" | ", candidates.Take(5).Select(x =>
                                $"QuotationId={x.Id}: {string.Join("; ", x.Differences)}")));
                    }
                    else
                    {
                        ambiguous++;
                        AddIssue(nameof(PreFactor), pf.Id, pf.SerialNumber.ToString(),
                            "Ambiguous", "MultipleMatchingQuotations",
                            $"چند پیش‌فاکتور کاملاً مطابق: QuotationIds={string.Join(", ", matches.Select(x => x.Id))}");
                    }
                }

                RemoveAmbiguousProposals(ExternalMappingTypeEnum.پیش_فاکتور);

                // Reuse existing quotation mappings as well as unambiguous new proposals.
                var quotationByPreFactor = new Dictionary<Guid, int>();
                foreach (var map in existing.Where(x =>
                             x.ExternalSoftwareId == softwareId
                             && x.EntityName == nameof(PreFactor)
                             && x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                             && x.ExEntityName == nameof(Quotation)))
                {
                    if (int.TryParse(map.ExEntityId, out var id) && id > 0)
                        quotationByPreFactor.TryAdd(map.EntityId, id);
                }

                foreach (var map in pending.Where(x =>
                             x.ExternalSoftwareId == softwareId
                             && x.MapType == ExternalMappingTypeEnum.پیش_فاکتور))
                    quotationByPreFactor[map.EntityId] = int.Parse(map.ExEntityId);

                var quotationIds = quotationByPreFactor.Values.Distinct().ToArray();
                var legacyInvoiceIds = factors.Where(f => f.SepidarId > 0)
                    .Select(f => f.SepidarId!.Value).Distinct().ToArray();

                var invoices = new Dictionary<int, LegacyInvoiceRow>();
                foreach (var batch in quotationIds.Chunk(400))
                {
                    readStage = "Sepidar1.SLS.Invoice";
                    var rows = await sepidar.SalesInvoices.AsNoTracking()
                        .Where(x => x.QuotationRef.HasValue && batch.Contains(x.QuotationRef.Value))
                        .Select(x => new LegacyInvoiceRow(
                            x.InvoiceId, x.Number, x.QuotationRef,
                            x.FiscalYearRef, x.NetPriceInBaseCurrency, x.CustomerPartyRef))
                        .ToListAsync(ct);

                    foreach (var row in rows)
                        invoices.TryAdd(row.Id, row);
                }

                foreach (var batch in legacyInvoiceIds.Chunk(400))
                {
                    readStage = "Sepidar1.SLS.Invoice";
                    var rows = await sepidar.SalesInvoices.AsNoTracking()
                        .Where(x => batch.Contains(x.InvoiceId))
                        .Select(x => new LegacyInvoiceRow(
                            x.InvoiceId, x.Number, x.QuotationRef,
                            x.FiscalYearRef, x.NetPriceInBaseCurrency, x.CustomerPartyRef))
                        .ToListAsync(ct);

                    foreach (var row in rows)
                        invoices.TryAdd(row.Id, row);
                }

                var invoicesByQuotation = invoices.Values
                    .Where(x => x.QuotationRef.HasValue)
                    .GroupBy(x => x.QuotationRef!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var factor in factors)
                {
                    if (existingSources.Contains((softwareId, nameof(Factor),
                            factor.Id, ExternalMappingTypeEnum.فاکتور)))
                    {
                        alreadyMapped++;
                        continue;
                    }

                    quotationByPreFactor.TryGetValue(factor.PfactorId, out var quotationId);
                    List<LegacyInvoiceRow> matches;

                    if (factor.SepidarId > 0)
                    {
                        // Old Factor.SepidarId points to Sepidar1; it is not a Sepidar2 reference.
                        matches = invoices.TryGetValue(factor.SepidarId.Value, out var invoice)
                            ? new List<LegacyInvoiceRow> { invoice } : new List<LegacyInvoiceRow>();
                    }
                    else
                    {
                        matches = quotationId > 0 && invoicesByQuotation.TryGetValue(
                                quotationId, out var quotationInvoices)
                            ? quotationInvoices : new List<LegacyInvoiceRow>();
                    }

                    var candidateInvoices = matches.ToList();
                    matches = matches.Where(invoice =>
                        fiscalYears.TryGetValue(invoice.FiscalYearRef, out var financialYear)
                        && IsInLegacyFiscalYear(factor.Date, factor.DateMiladi, financialYear)
                        && invoice.NetPrice == factor.PaymentAmount
                        && (quotationId <= 0 || invoice.QuotationRef == quotationId)
                        && (string.IsNullOrWhiteSpace(factor.SnFinancial)
                            || (int.TryParse(factor.SnFinancial, out var financialNumber)
                                && invoice.Number == financialNumber))
                        && (!factor.CustomerId.HasValue
                            || !financialCustomerIds.TryGetValue(factor.CustomerId.Value, out var partyId)
                            || invoice.CustomerPartyRef == partyId))
                        .ToList();

                    if (matches.Count == 1)
                    {
                        Propose(softwareId, nameof(Factor), factor.Id,
                            factor.SnFinancial ?? factor.SepidarId?.ToString() ?? factor.Id.ToString(),
                            ExternalMappingTypeEnum.فاکتور, nameof(Invoice), matches[0].Id);
                    }
                    else if (matches.Count == 0)
                    {
                        notMatched++;
                        var crmNumber = factor.SnFinancial ?? factor.SepidarId?.ToString() ?? "-";
                        if (candidateInvoices.Count == 0)
                        {
                            var reason = factor.SepidarId > 0 ? "InvoiceIdNotFound"
                                : quotationId == 0 ? "RelatedQuotationNotMapped"
                                : "InvoiceForQuotationNotFound";
                            AddIssue(nameof(Factor), factor.Id, crmNumber,
                                "NotMatched", reason,
                                $"CRM PrefactorId={factor.PfactorId}, SepidarId={factor.SepidarId}, QuotationId={quotationId}");
                        }
                        else
                        {
                            var candidates = candidateInvoices.Select(invoice => {
                                var differences = new List<string>();
                                if (!fiscalYears.TryGetValue(invoice.FiscalYearRef, out var fiscalYear))
                                    differences.Add($"سال مالی سپیدار Ref={invoice.FiscalYearRef} ناشناخته");
                                else if (!IsInLegacyFiscalYear(factor.Date, factor.DateMiladi, fiscalYear))
                                    differences.Add($"سال مالی: CRM={factor.Date}, Sepidar={fiscalYear.Year}");

                                if (invoice.NetPrice != factor.PaymentAmount)
                                    differences.Add($"مبلغ نهایی: CRM={factor.PaymentAmount}, Sepidar={invoice.NetPrice}");
                                if (quotationId > 0 && invoice.QuotationRef != quotationId)
                                    differences.Add($"QuotationRef: expected={quotationId}, actual={invoice.QuotationRef}");
                                if (!string.IsNullOrWhiteSpace(factor.SnFinancial)
                                    && (!int.TryParse(factor.SnFinancial, out var number)
                                        || invoice.Number != number))
                                    differences.Add($"شماره مالی: CRM={factor.SnFinancial}, Sepidar={invoice.Number}");
                                if (factor.CustomerId.HasValue
                                    && financialCustomerIds.TryGetValue(factor.CustomerId.Value, out var partyId)
                                    && invoice.CustomerPartyRef != partyId)
                                    differences.Add($"مشتری: CRM SepidarPartyId={partyId}, Sepidar={invoice.CustomerPartyRef}");

                                return new { invoice.Id, Differences = differences };
                            }).ToList();
                            var reason = candidates.All(x => x.Differences.Any(d => d.StartsWith("سال مالی")))
                                ? "InvoiceFiscalYearMismatch"
                                : candidates.All(x => x.Differences.Any(d => d.StartsWith("مبلغ")))
                                    ? "InvoiceAmountMismatch"
                                    : candidates.All(x => x.Differences.Any(d => d.StartsWith("شماره مالی")))
                                        ? "InvoiceFinancialNumberMismatch"
                                        : candidates.All(x => x.Differences.Any(d => d.StartsWith("مشتری")))
                                            ? "InvoiceCustomerMismatch"
                                            : candidates.All(x => x.Differences.Any(d => d.StartsWith("QuotationRef")))
                                                ? "InvoiceQuotationMismatch"
                                                : "InvoiceMultipleFieldMismatch";
                            AddIssue(nameof(Factor), factor.Id, crmNumber,
                                "NotMatched", reason,
                                string.Join(" | ", candidates.Take(5).Select(x =>
                                    $"InvoiceId={x.Id}: {string.Join("; ", x.Differences)}")));
                        }
                    }
                    else
                    {
                        ambiguous++;
                        AddIssue(nameof(Factor), factor.Id,
                            factor.SnFinancial ?? factor.SepidarId?.ToString() ?? "-",
                            "Ambiguous", "MultipleMatchingInvoices",
                            $"چند فاکتور مطابق: InvoiceIds={string.Join(", ", matches.Select(x => x.Id))}");
                    }
                }

                RemoveAmbiguousProposals(ExternalMappingTypeEnum.فاکتور);

                // Map CRM customers from the Sepidar documents that are already linked
                // or have a unique, conflict-free mapping proposed by this run.
                // The existing Customer.ExternalMapping is never overwritten.
                var documentMappings = existing.Concat(pending)
                    .Where(x => x.ExternalSoftwareId == softwareId &&
                        ((x.EntityName == nameof(PreFactor)
                          && x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                          && x.ExEntityName == nameof(Quotation))
                         || (x.EntityName == nameof(Factor)
                          && x.MapType == ExternalMappingTypeEnum.فاکتور
                          && x.ExEntityName == nameof(Invoice))))
                    .ToList();

                var crmPreFactors = preFactors.ToDictionary(x => x.Id);
                var crmFactors = factors.ToDictionary(x => x.Id);

                var quotationIdsForCustomers = documentMappings
                    .Where(x => x.ExEntityName == nameof(Quotation))
                    .Select(x => int.TryParse(x.ExEntityId, out var id) ? id : 0)
                    .Where(x => x > 0).Distinct().ToArray();
                var invoiceIdsForCustomers = documentMappings
                    .Where(x => x.ExEntityName == nameof(Invoice))
                    .Select(x => int.TryParse(x.ExEntityId, out var id) ? id : 0)
                    .Where(x => x > 0).Distinct().ToArray();

                readStage = "Sepidar1.SLS.Quotation customer references";
                var quotationParties = new Dictionary<int, int?>();
                foreach (var batch in quotationIdsForCustomers.Chunk(400))
                {
                    var rows = await sepidar.Quotations.AsNoTracking()
                        .Where(x => batch.Contains(x.QuotationId))
                        .Select(x => new {
                            x.QuotationId,
                            PartyId = (int?)x.CustomerPartyRef
                        })
                        .ToListAsync(ct);

                    foreach (var row in rows)
                        quotationParties[row.QuotationId] = row.PartyId;
                }

                readStage = "Sepidar1.SLS.Invoice customer references";
                var invoiceParties = new Dictionary<int, int?>();
                foreach (var batch in invoiceIdsForCustomers.Chunk(400))
                {
                    var rows = await sepidar.SalesInvoices.AsNoTracking()
                        .Where(x => batch.Contains(x.InvoiceId))
                        .Select(x => new {
                            x.InvoiceId,
                            PartyId = (int?)x.CustomerPartyRef
                        })
                        .ToListAsync(ct);

                    foreach (var row in rows)
                        invoiceParties[row.InvoiceId] = row.PartyId;
                }

                var evidence = new List<LegacyCustomerEvidence>();
                foreach (var mapping in documentMappings)
                {
                    Guid? crmCustomerId = null;
                    if (mapping.EntityName == nameof(PreFactor)
                        && crmPreFactors.TryGetValue(mapping.EntityId, out var sourcePreFactor))
                        crmCustomerId = sourcePreFactor.CustomerId;
                    else if (mapping.EntityName == nameof(Factor)
                        && crmFactors.TryGetValue(mapping.EntityId, out var sourceFactor))
                    {
                        var relatedPreFactorCustomerId =
                            crmPreFactors.GetValueOrDefault(sourceFactor.PfactorId)?.CustomerId;

                        // Do not derive a customer mapping from internally inconsistent CRM documents.
                        if (sourceFactor.CustomerId.HasValue
                            && relatedPreFactorCustomerId.HasValue
                            && sourceFactor.CustomerId.Value != relatedPreFactorCustomerId.Value)
                            continue;

                        crmCustomerId = sourceFactor.CustomerId ?? relatedPreFactorCustomerId;
                    }

                    if (!crmCustomerId.HasValue
                        || !customersById.ContainsKey(crmCustomerId.Value)
                        || !int.TryParse(mapping.ExEntityId, out var externalDocId))
                        continue;

                    int? sepidarPartyId = null;
                    if (mapping.ExEntityName == nameof(Quotation))
                        quotationParties.TryGetValue(externalDocId, out sepidarPartyId);
                    else if (mapping.ExEntityName == nameof(Invoice))
                        invoiceParties.TryGetValue(externalDocId, out sepidarPartyId);

                    if (sepidarPartyId > 0)
                        evidence.Add(new LegacyCustomerEvidence(
                            crmCustomerId.Value, sepidarPartyId.Value,
                            $"{mapping.ExEntityName}:{externalDocId}"));
                }

                // Check actual GNR.Party rows; don't use a document's PartyRef blindly.
                var partyIds = evidence.Select(x => x.SepidarPartyId).Distinct().ToArray();
                readStage = "Sepidar1.GNR.Party customer validation";
                var validSepidarCustomers = new HashSet<int>();
                foreach (var batch in partyIds.Chunk(400))
                {
                    var validIds = await sepidar.Parties.AsNoTracking()
                        .Where(x => batch.Contains(x.PartyId) && x.IsCustomer)
                        .Select(x => x.PartyId).ToListAsync(ct);
                    validSepidarCustomers.UnionWith(validIds);
                }

                // Validate historical customer mappings too. A customer already mapped to
                // a different Party, or with contradictory document evidence, is conflicted.
                var existingCustomerMappings = existing
                    .Where(x => x.ExternalSoftwareId == softwareId
                        && x.MapType == ExternalMappingTypeEnum.مشتری
                        && x.EntityName == nameof(Customer))
                    .GroupBy(x => x.EntityId)
                    .ToDictionary(x => x.Key, x => x.ToList());

                var customerCandidates = new List<LegacyCustomerEvidence>();
                foreach (var customerEvidence in evidence.GroupBy(x => x.CustomerId))
                {
                    var customer = customersById[customerEvidence.Key];
                    var possibleParties = customerEvidence.Select(x => x.SepidarPartyId)
                        .Distinct().OrderBy(x => x).ToList();
                    var sources = customerEvidence.Select(x => x.SourceDocument)
                        .Distinct().OrderBy(x => x).ToList();

                    existingCustomerMappings.TryGetValue(customer.Id, out var savedMappings);
                    var savedPartyIds = savedMappings?
                        .Select(x => int.TryParse(x.ExEntityId, out var id) ? id : 0)
                        .Distinct().ToList() ?? new List<int>();

                    // An existing, valid customer mapping resolves the CRM customer choice.
                    // Individual quotations/invoices referencing another Party are still
                    // rejected separately by DocumentSepidarCustomerMismatch below.
                    var reason = savedMappings != null
                        ? savedPartyIds.Count != 1 || savedPartyIds[0] <= 0
                          || savedMappings.Any(x => x.ExEntityName != nameof(Party))
                            ? "ExistingCustomerMappingMismatch"
                            : !validSepidarCustomers.Contains(savedPartyIds[0])
                                && !await sdb.Parties.AsNoTracking()
                                    .AnyAsync(x => x.PartyId == savedPartyIds[0] && x.IsCustomer, ct)
                                    ? "PartyNotFoundOrNotCustomer"
                            : existing.Any(x => x.ExternalSoftwareId == softwareId
                                && x.MapType == ExternalMappingTypeEnum.مشتری
                                && x.ExEntityName == nameof(Party)
                                && x.EntityId != customer.Id
                                && x.ExEntityId == savedPartyIds[0].ToString())
                                ? "ExistingDuplicateCustomerParty"
                            : null
                        : possibleParties.Count > 1 ? "MultipleSepidarParties"
                        : !validSepidarCustomers.Contains(possibleParties[0])
                            ? "PartyNotFoundOrNotCustomer"
                        : customer.FinancialCustomerId.HasValue
                          && customer.FinancialCustomerId != possibleParties[0]
                            ? "LegacyFinancialCustomerIdMismatch"
                        : existing.Any(x => x.ExternalSoftwareId == softwareId
                          && x.MapType == ExternalMappingTypeEnum.مشتری
                          && x.ExEntityName == nameof(Party)
                          && x.ExEntityId == possibleParties[0].ToString()
                          && x.EntityId != customer.Id)
                            ? "PartyAlreadyMappedToAnotherCustomer"
                        : null;

                    if (reason != null)
                    {
                        customerConflicts.Add(new LegacyCustomerConflict(
                            customer.Id, customer.CompanyName, reason, possibleParties, sources));
                        continue;
                    }

                    // Already mapped and consistent with the matched Sepidar documents.
                    if (savedMappings != null)
                        continue;

                    customerCandidates.Add(new LegacyCustomerEvidence(
                        customer.Id, possibleParties[0], string.Join(", ", sources)));
                }

                // Existing duplicate customer mappings also block new documents.
                foreach (var duplicatedParty in existingCustomerMappings
                             .SelectMany(g => g.Value
                                 .Where(x => x.ExEntityName == nameof(Party)
                                     && int.TryParse(x.ExEntityId, out _))
                                 .Select(x => new { g.Key, PartyId = int.Parse(x.ExEntityId) }))
                             .GroupBy(x => x.PartyId)
                             .Where(x => x.Select(y => y.Key).Distinct().Count() > 1))
                {
                    foreach (var customerId in duplicatedParty.Select(x => x.Key).Distinct())
                    {
                        if (!customersById.TryGetValue(customerId, out var customer)
                            || customerConflicts.Any(x => x.CustomerId == customerId))
                            continue;

                        customerConflicts.Add(new LegacyCustomerConflict(
                            customerId, customer.CompanyName, "ExistingDuplicateCustomerParty",
                            new List<int> { duplicatedParty.Key },
                            duplicatedParty.Select(x => $"CRM CustomerId={x.Key}").Distinct().ToList()));
                    }
                }

                // Two different CRM customers must not silently map to the same Party.
                var repeatedParties = customerCandidates.GroupBy(x => x.SepidarPartyId)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key).ToHashSet();

                foreach (var candidate in customerCandidates)
                {
                    if (repeatedParties.Contains(candidate.SepidarPartyId))
                    {
                        var customer = customersById[candidate.CustomerId];
                        customerConflicts.Add(new LegacyCustomerConflict(
                            candidate.CustomerId, customer.CompanyName,
                            "DuplicateCrmCustomersForParty",
                            new List<int> { candidate.SepidarPartyId },
                            customerCandidates.Where(x => x.SepidarPartyId == candidate.SepidarPartyId)
                                .Select(x => {
                                    var name = customersById.GetValueOrDefault(x.CustomerId)?.CompanyName ?? "-";
                                    return $"مشتری CRM: {name} | CustomerId={x.CustomerId} | {x.SourceDocument}";
                                }).ToList()));
                        continue;
                    }

                    pending.Add(new ExternalMapping {
                        Id = Guid.NewGuid(),
                        EntityName = nameof(Customer),
                        EntityId = candidate.CustomerId,
                        ExternalSoftwareId = softwareId,
                        MapType = ExternalMappingTypeEnum.مشتری,
                        MapTypeTxt = nameof(ExternalMappingTypeEnum.مشتری),
                        ExEntityName = nameof(Party),
                        ExEntityId = candidate.SepidarPartyId.ToString(),
                        UserCreatorID = "legacy-migration",
                        UserCreator = "Legacy Sepidar mapping backfill"
                    });
                }

                // Stage 2: an invoice/quotation can only be proposed after its CRM
                // customer is ALREADY mapped in ExternalMapping, with a matching Party.
                // A customer mapping merely proposed in this run does not unlock documents.
                // Approve/save customer mappings first, then re-run the preview.
                var conflictedCustomerIds = customerConflicts
                    .Select(x => x.CustomerId).ToHashSet();
                var proposedCustomerIds = pending
                    .Where(x => x.MapType == ExternalMappingTypeEnum.مشتری
                        && x.ExternalSoftwareId == softwareId)
                    .Select(x => x.EntityId).ToHashSet();
                var blockedIds = new HashSet<Guid>();

                foreach (var document in pending.Where(x => x.ExternalSoftwareId == softwareId
                             && (x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                                 || x.MapType == ExternalMappingTypeEnum.فاکتور)).ToList())
                {
                    Guid? customerId = null;
                    string crmNumber;
                    if (document.MapType == ExternalMappingTypeEnum.پیش_فاکتور)
                    {
                        var source = crmPreFactors.GetValueOrDefault(document.EntityId);
                        customerId = source?.CustomerId;
                        crmNumber = source?.SerialNumber.ToString() ?? "-";
                    }
                    else
                    {
                        var source = crmFactors.GetValueOrDefault(document.EntityId);
                        crmNumber = source?.SerialNumber.ToString() ?? "-";
                        customerId = source?.CustomerId;
                        // A factor without an explicit CustomerId may inherit the
                        // customer of its CRM quotation, but never if the IDs disagree.
                        var quotationCustomerId = source != null
                            ? crmPreFactors.GetValueOrDefault(source.PfactorId)?.CustomerId
                            : null;
                        if (customerId.HasValue && quotationCustomerId.HasValue
                            && customerId.Value != quotationCustomerId.Value)
                        {
                            blockedIds.Add(document.Id);
                            AddIssue(document.EntityName, document.EntityId, crmNumber,
                                "BlockedByCustomer", "DocumentCrmCustomerMismatch",
                                $"Factor.CustomerId={customerId}, PreFactor.CustomerId={quotationCustomerId} | {document.ExEntityName} ID={document.ExEntityId}");
                            continue;
                        }

                        customerId ??= quotationCustomerId;
                    }

                    if (!customerId.HasValue || !customersById.ContainsKey(customerId.Value))
                    {
                        blockedIds.Add(document.Id);
                        AddIssue(document.EntityName, document.EntityId, crmNumber,
                            "BlockedByCustomer", "DocumentCustomerMissing",
                            $"مشتری CRM این سند مشخص نیست یا در جدول Customers وجود ندارد. | {document.ExEntityName} ID={document.ExEntityId}");
                        continue;
                    }

                    if (conflictedCustomerIds.Contains(customerId.Value))
                    {
                        blockedIds.Add(document.Id);
                        AddIssue(document.EntityName, document.EntityId, crmNumber,
                            "BlockedByCustomer", "CustomerHasConflict",
                            $"CustomerId={customerId.Value} دارای تعارض مپینگ است. | {document.ExEntityName} ID={document.ExEntityId}");
                        continue;
                    }

                    if (!existingCustomerMappings.TryGetValue(customerId.Value, out var confirmed)
                        || confirmed.Count != 1 || confirmed[0].ExEntityName != nameof(Party)
                        || !int.TryParse(confirmed[0].ExEntityId, out var mappedPartyId)
                        || mappedPartyId <= 0)
                    {
                        blockedIds.Add(document.Id);
                        var reason = proposedCustomerIds.Contains(customerId.Value)
                            ? "CustomerMappingPendingApproval" : "CustomerNotMapped";
                        AddIssue(document.EntityName, document.EntityId, crmNumber,
                            "BlockedByCustomer", reason,
                            $"CustomerId={customerId.Value}; ابتدا مپینگ مشتری را ثبت یا اصلاح کنید. | {document.ExEntityName} ID={document.ExEntityId}");
                        continue;
                    }

                    int? documentPartyId = null;
                    if (document.ExEntityName == nameof(Quotation)
                        && int.TryParse(document.ExEntityId, out var quotationDocId))
                        quotationParties.TryGetValue(quotationDocId, out documentPartyId);
                    else if (document.ExEntityName == nameof(Invoice)
                             && int.TryParse(document.ExEntityId, out var invoiceDocId))
                        invoiceParties.TryGetValue(invoiceDocId, out documentPartyId);

                    if (documentPartyId != mappedPartyId)
                    {
                        blockedIds.Add(document.Id);
                        AddIssue(document.EntityName, document.EntityId, crmNumber,
                            "BlockedByCustomer", "DocumentSepidarCustomerMismatch",
                            $"CustomerId={customerId.Value}, mapped PartyId={mappedPartyId}, document PartyId={documentPartyId} | {document.ExEntityName} ID={document.ExEntityId}");
                    }
                }

                pending.RemoveAll(x => blockedIds.Contains(x.Id));

            }

            return new LegacyMappingPlan(
                alreadyMapped, notMatched, ambiguous, pending, issues, duplicateTargetGroups,
                customerConflicts,
                existing.Count(x => x.ExternalSoftwareId == Sepidar1SoftwareId
                    && x.MapType == ExternalMappingTypeEnum.مشتری
                    && x.EntityName == nameof(Customer)));
            }
            catch (SqlNullValueException ex)
            {
                throw new InvalidOperationException(
                    $"A NULL database column was encountered while reading {readStage}. Check EF Core nullability for this query.",
                    ex);
            }
        }

        // NOT CALLED from Index or any public route.
        // This write method must only be wired to an administrator-only one-time POST
        // AFTER the user has reviewed the candidate counts and approved the migration.
        private async Task<int> ApplyLegacySepidarMappingPlanAsync(CancellationToken ct = default)
        {
            var plan = await BuildLegacySepidarMappingPlanAsync(ct);
            if (plan.Pending.Count == 0) return 0;

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // Never overwrite an existing mapping or silently reuse a target record.
                var live = await db.ExternalMappings.AsNoTracking()
                    .Where(x => x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                             || x.MapType == ExternalMappingTypeEnum.فاکتور
                             || x.MapType == ExternalMappingTypeEnum.مشتری)
                    .ToListAsync(ct);

                foreach (var candidate in plan.Pending)
                {
                    if (live.Any(x => x.ExternalSoftwareId == candidate.ExternalSoftwareId
                        && ((x.EntityName == candidate.EntityName
                             && x.EntityId == candidate.EntityId
                             && x.MapType == candidate.MapType)
                            || (x.ExEntityName == candidate.ExEntityName
                                && x.ExEntityId == candidate.ExEntityId))))
                        throw new InvalidOperationException(
                            "نگاشت‌های مالی هنگام آماده‌سازی تغییر کرده‌اند؛ پیش‌نمایش را دوباره تهیه کنید.");
                }

                db.ExternalMappings.AddRange(plan.Pending);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return plan.Pending.Count;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        private sealed record MappingReportSide(
            string Title, string Id, string Number, string Date,
            decimal? Amount, string Customer, string Extra);

        private sealed record MappingReportEntry(
            string Key, string Kind, Guid EntityId, string Status,
            string Reason, string Details, string TargetEntity, int? TargetId,
            string[] Sources, int MapType, string TargetRaw = null, int[] CandidateIds = null);

        private sealed record MappingReportRow(
            string Key, string Kind, string Status, string Reason,
            string Details, string[] Sources,
            MappingReportSide Crm, MappingReportSide Sepidar, int MapType,
            List<MappingReportTargetOption> Options);

        private sealed record MappingReportTargetOption(int Id, string Title, string Description);



        public async Task<object> GetExternalMappingReview(CancellationToken ct)
        {
            // NEVER invoke ApplyLegacySepidarMappingPlanAsync from a report endpoint.
            var plan = await BuildLegacySepidarMappingPlanAsync(ct);
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

            var relatedCustomerIds = evidence.Select(x => x.CustomerId)
                .Concat(plan.CustomerConflicts.Select(x => x.CustomerId))
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
                var rows = await sdb.Quotations.AsNoTracking()
                    .Where(x => batch.Contains(x.QuotationId))
                    .Select(x => new { x.QuotationId, x.Number, x.CustomerPartyRef })
                    .ToListAsync(ct);
                foreach (var row in rows)
                    linkedDocuments[("Quotation", row.QuotationId)] = (row.Number, row.CustomerPartyRef);
            }
            foreach (var batch in invoiceIds.Chunk(350))
            {
                var rows = await sdb.SalesInvoices.AsNoTracking()
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
                var parties = await sdb.Parties.AsNoTracking()
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

                var sources = related.Concat(partyDescriptions).ToArray();
                entries.Add(new MappingReportEntry(
                    $"customer-conflict-{conflict.CustomerId}", nameof(Customer),
                    conflict.CustomerId, "customerConflict", conflict.Reason,
                    $"این مشتری CRM به {conflict.SepidarPartyIds.Count} مشتری احتمالی در سپیدار مربوط شده است. ردیف‌های مرتبط را باز کنید تا ارتباط هر پیش‌فاکتور یا فاکتور روشن شود.",
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
                    "این ارتباط فقط پیشنهاد شده و هنوز در ExternalMapping ذخیره نشده است.",
                    mapping.ExEntityName, targetId > 0 ? targetId : null, Array.Empty<string>(),
                    (int)mapping.MapType, mapping.ExEntityId,
                    targetId > 0 ? new[] { targetId } : Array.Empty<int>()));
            }

            var rows = await EnrichMappingEntriesAsync(entries, Sepidar1SoftwareId, ct);
            var saved = await db.ExternalMappings.AsNoTracking()
                .Where(x => x.ExternalSoftwareId == Sepidar1SoftwareId
                    && (x.MapType == ExternalMappingTypeEnum.مشتری
                        || x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                        || x.MapType == ExternalMappingTypeEnum.فاکتور))
                .CountAsync(ct);

            return new
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

                var ids = matchedIds.ToArray();
                query = query.Where(x => x.ExEntityId.Contains(search)
                    || x.EntityId.ToString().Contains(search)
                    || ids.Contains(x.EntityId));
            }

            var total = await query.CountAsync(ct);
            var mappings = await query.OrderByDescending(x => x.CreateDate)
                .ThenBy(x => x.EntityId).Skip((page - 1) * pageSize)
                .Take(pageSize).Select(x => new {
                    x.EntityName, x.EntityId, x.MapType, x.ExEntityName, x.ExEntityId
                }).ToListAsync(ct);

            var entries = mappings.Select(x => {
                int.TryParse(x.ExEntityId, out var externalId);
                return new MappingReportEntry(
                    $"saved-{x.EntityName}-{x.EntityId}-{(int)x.MapType}", x.EntityName,
                    x.EntityId, "saved", "AlreadyMapped",
                    "این نگاشت در پایگاه داده CRM ثبت شده است.",
                    x.ExEntityName, externalId > 0 ? externalId : null,
                    Array.Empty<string>(), (int)x.MapType, x.ExEntityId);
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
                        "پیش‌فاکتور سپیدار ۱", x.QuotationId.ToString(), x.Number.ToString(),
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
                        "فاکتور سپیدار ۱", x.InvoiceId.ToString(), x.Number?.ToString() ?? "-",
                        x.Date.HasValue ? FormatLegacyPersianDate(x.Date.Value) : "-",
                        x.Amount, x.CustomerRealName,
                        $"PartyId: {x.PartyId} | FiscalYearRef: {x.FiscalYearId}");
            }

            foreach (var batch in partyIds.Chunk(350))
            {
                var data = await sepidarContext.Parties.AsNoTracking()
                    .Where(x => batch.Contains(x.PartyId))
                    .Select(x => new { x.PartyId, x.Name, x.LastName, x.IsCustomer })
                    .ToListAsync(ct);
                foreach (var x in data)
                    externalSides[(nameof(Party), x.PartyId)] = new MappingReportSide(
                        "مشتری سپیدار ۱", x.PartyId.ToString(), x.PartyId.ToString(),
                        "", null, $"{x.Name} {x.LastName}".Trim(),
                        x.IsCustomer ? "مشتری فعال در فهرست اشخاص" : "این شخص در سپیدار به‌عنوان مشتری علامت‌گذاری نشده");
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
                            : entry.TargetEntity == nameof(Invoice) ? "فاکتور" : "مشتری";
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
                    entry.MapType, options);
            }).ToList();
        }

        public sealed class SelectedExternalMapping
        {
            public Guid EntityId { get; set; }
            public ExternalMappingTypeEnum MapType { get; set; }
            public string ExEntityId { get; set; }
        }

        public sealed class SaveExternalMappingsRequest
        {
            public Guid ExternalSoftwareId { get; set; }
            public List<SelectedExternalMapping> Items { get; set; } = new();
        }

        public async Task<object> GetExternalMappingWorkspaceOptions(CancellationToken ct)
        {
            var software = await db.ExternalSoftwares.AsNoTracking()
                .OrderBy(x => x.Title)
                .Select(x => new { id = x.Id, title = x.Title })
                .ToListAsync(ct);

            var counts = await db.ExternalMappings.AsNoTracking()
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
                automaticMatchingSoftwareId = Sepidar1SoftwareId,
                supportedManualSoftwareIds = new[] { Sepidar1SoftwareId, Sepidar2SoftwareId }
            };
        }

        public async Task<object> GetExternalMappingTargetDetails(
            Guid softwareId, int mapType, int targetId, CancellationToken ct)
        {
            if (targetId <= 0 || !Enum.IsDefined(typeof(ExternalMappingTypeEnum), mapType)
                || softwareId != Sepidar1SoftwareId && softwareId != Sepidar2SoftwareId
                || !await db.ExternalSoftwares.AsNoTracking().AnyAsync(x => x.Id == softwareId, ct))
                throw new ArgumentException("نرم‌افزار، نوع نگاشت یا شناسه معتبر نیست.");

            var sepidar = softwareId == Sepidar1SoftwareId
                ? (SepidarContextDbBase)sdb : s2Db;
            var entityName = mapType == (int)ExternalMappingTypeEnum.مشتری ? nameof(Customer)
                : mapType == (int)ExternalMappingTypeEnum.پیش_فاکتور ? nameof(PreFactor)
                : mapType == (int)ExternalMappingTypeEnum.فاکتور ? nameof(Factor) : null;
            if (entityName == null)
                throw new ArgumentException("نمایش معادل برای این نوع نگاشت هنوز پشتیبانی نمی‌شود.");

            var externalName = entityName == nameof(Customer) ? "Party"
                : entityName == nameof(PreFactor) ? "Quotation" : "Invoice";
            var exists = entityName == nameof(Customer)
                ? await sepidar.Parties.AsNoTracking().AnyAsync(x => x.PartyId == targetId && x.IsCustomer, ct)
                : entityName == nameof(PreFactor)
                    ? await sepidar.Quotations.AsNoTracking().AnyAsync(x => x.QuotationId == targetId, ct)
                    : await sepidar.SalesInvoices.AsNoTracking().AnyAsync(x => x.InvoiceId == targetId, ct);
            if (!exists) throw new KeyNotFoundException("معادل انتخاب‌شده در نرم‌افزار خارجی پیدا نشد.");

            var entry = new MappingReportEntry(
                "manual-preview", entityName, Guid.Empty, "pending", "ReadyForReview",
                "", externalName, targetId, Array.Empty<string>(), mapType,
                targetId.ToString(), new[] { targetId });
            var rows = await EnrichMappingEntriesAsync(new List<MappingReportEntry> { entry }, softwareId, ct);
            return new { target = rows[0].Sepidar };
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

        // Only selected rows are submitted; no automatic migration runs from page visits.
        // On success each record becomes a persisted ExternalMapping, never an external INSERT.


        public async Task<int> SaveSelectedExternalMappings(
            SaveExternalMappingsRequest request, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct)
        {
            if (request == null || request.Items == null || request.Items.Count == 0 || request.Items.Count > 100)
                throw new ArgumentException("بین ۱ تا ۱۰۰ نگاشت را انتخاب کنید.");

            if (request.ExternalSoftwareId != Sepidar1SoftwareId
                && request.ExternalSoftwareId != Sepidar2SoftwareId)
                throw new ArgumentException("برای این نرم‌افزار، ثبت دستی هنوز پشتیبانی نمی‌شود.");

            if (!await db.ExternalSoftwares.AsNoTracking()
                    .AnyAsync(x => x.Id == request.ExternalSoftwareId, ct))
                throw new ArgumentException("نرم‌افزار انتخاب‌شده ثبت نشده است.");

            var repeatedSources = request.Items.GroupBy(x => (x.MapType, x.EntityId))
                .Any(x => x.Count() > 1);
            if (repeatedSources)
                throw new ArgumentException("برای هر رکورد CRM فقط یک معادل انتخاب کنید.");

            var allowed = new[] {
                ExternalMappingTypeEnum.مشتری,
                ExternalMappingTypeEnum.پیش_فاکتور,
                ExternalMappingTypeEnum.فاکتور
            };
            if (request.Items.Any(x => !allowed.Contains(x.MapType)
                || x.EntityId == Guid.Empty
                || !int.TryParse(x.ExEntityId, out var id) || id <= 0))
                throw new ArgumentException("نوع نگاشت یا شناسه انتخاب‌شده معتبر نیست.");

            var hasCustomer = request.Items.Any(x => x.MapType == ExternalMappingTypeEnum.مشتری);
            var hasOther = request.Items.Any(x => x.MapType != ExternalMappingTypeEnum.مشتری);
            if (hasCustomer && hasOther)
                throw new ArgumentException("ابتدا نگاشت مشتریان را ثبت کنید؛ سپس پیش‌فاکتور یا فاکتور را انتخاب کنید.");

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

                var duplicatesInRequest = request.Items.GroupBy(x =>
                    (x.MapType, x.ExEntityId.Trim())).Any(x => x.Count() > 1);
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
                    var entityName = item.MapType switch {
                        ExternalMappingTypeEnum.مشتری => nameof(Customer),
                        ExternalMappingTypeEnum.پیش_فاکتور => nameof(PreFactor),
                        _ => nameof(Factor)
                    };
                    var externalName = item.MapType switch {
                        ExternalMappingTypeEnum.مشتری => "Party",
                        ExternalMappingTypeEnum.پیش_فاکتور => "Quotation",
                        _ => "Invoice"
                    };

                    if (existing.Any(x => x.EntityName == entityName
                        && x.EntityId == item.EntityId && x.MapType == item.MapType))
                        throw new MappingConflictException($"این {item.MapType} در نرم‌افزار انتخاب‌شده قبلاً نگاشت شده است.");

                    if (existing.Any(x => x.ExEntityName == externalName
                        && x.ExEntityId == targetId.ToString()
                        && x.EntityId != item.EntityId))
                        throw new MappingConflictException($"معادل خارجی شماره {targetId} قبلاً به رکورد CRM دیگری وصل شده است.");

                    if (item.MapType == ExternalMappingTypeEnum.مشتری)
                    {
                        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == item.EntityId, ct))
                            throw new ArgumentException("مشتری انتخاب‌شده در CRM یافت نشد.");
                        if (!await targetDb.Parties.AsNoTracking()
                                .AnyAsync(x => x.PartyId == targetId && x.IsCustomer, ct))
                            throw new ArgumentException($"مشتری با شناسه {targetId} در نرم‌افزار مقصد معتبر نیست.");
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
                                throw new ArgumentException("فقط فاکتور تأییدشده و غیربرگشتی قابل نگاشت است.");

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
                                throw new ArgumentException("پیش‌فاکتور مرتبط با فاکتور سپیدار، با نگاشت CRM هم‌خوان نیست.");
                        }

                        if (!confirmedCustomerMappings.TryGetValue(customerId.Value, out var customerMaps)
                            || customerMaps.Count != 1
                            || customerMaps[0].ExEntityName != "Party"
                            || customerMaps[0].ExEntityId != partyId.ToString())
                            throw new ArgumentException("ابتدا مشتری مربوط به فاکتور یا پیش‌فاکتور را در همین نرم‌افزار به‌درستی نگاشت کنید.");
                    }

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

                db.ExternalMappings.AddRange(newMappings);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return newMappings.Count;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Saving selected external mappings failed.");
                throw new MappingConflictException("اطلاعات نگاشت در این فاصله تغییر کرده است؛ فهرست را دوباره دریافت کنید.");
            }
        }
    }
}
