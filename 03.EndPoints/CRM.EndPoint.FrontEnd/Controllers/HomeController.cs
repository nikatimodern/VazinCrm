using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using System.Threading;
using System.Data.SqlTypes;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;
using CRM.EndPoint.FrontEnd.Services.AI;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.Carizo;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.EndPoint.FrontEnd.Services.Sms;
using CRM.Core.Domain.Deals.Enums;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class HomeController : Controller {
        private readonly IWebHostEnvironment appEnvironment;
        private readonly IConfiguration conf;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly CRMContextDb db;
        private readonly DateTimeServices dts;
        private readonly Sepidar1ContextDb sdb;
        private readonly Service genService;
        private readonly UserContextDb udb;
        private readonly Sepidar1ContextDb S1db;
        private readonly ISmsQueue smsQueue;
        private readonly IssabelService issabelService;
        private readonly AiPrompts aiPrompts;
        private readonly ViraAi viraAi;
        private readonly GapgptAi gapgptAi;
        private readonly LogSystemService logSystemService;
        private readonly TaskServices ts;
        private readonly SmsService smsService;
        private readonly Sepidar2ContextDb s2Db;
        private readonly Sepidar1Service sepidar1Service;
        private readonly Sepidar2Service sepidar2Service;
        private readonly ISmsIrService smsIrService;

        public HomeController(IWebHostEnvironment appEnvironment,
            IConfiguration conf,
            UserManager<AppUser> userManager, ILogger<HomeController> logger, CRMContextDb db, DateTimeServices dts,
            Sepidar1ContextDb sdb, Service genService, UserContextDb udb, Sepidar1ContextDb S1db, ISmsQueue smsQueue, IssabelService issabelService,
            AiPrompts aiPrompts, ViraAi viraAi, GapgptAi gapgptAi, LogSystemService logSystemService, TaskServices ts, SmsService smsService,
            Sepidar2ContextDb s2db, Sepidar1Service sepidar1Service, Sepidar2Service sepidar2Service)
        {
            this.appEnvironment = appEnvironment;
            this.conf = conf;
            this.userManager = userManager;
            _logger = logger;
            this.db = db;
            this.dts = dts;
            this.sdb = sdb;
            this.udb = udb;
            this.S1db = S1db;
            this.smsQueue = smsQueue;
            this.logSystemService = logSystemService;
            this.ts = ts;
            this.smsService = smsService;
            s2Db = s2db;
            this.sepidar1Service = sepidar1Service;
            this.sepidar2Service = sepidar2Service;
            this.smsIrService = smsIrService;
            this.issabelService = issabelService;
            this.aiPrompts = aiPrompts;
            this.viraAi = viraAi;
            this.gapgptAi = gapgptAi;
            this.genService = genService;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> Index()
        {
            try
            {

                //var de = db.Deals.ToList();
                //foreach(var item in de)
                //{
                //    item.CreateDate = DateTime.Now.AddDays(-20);
                //    item.CreateDateTxt = dts.GetDatePersian( DateTime.Now.AddDays(-20));
                //    item.Status = DealStatusEnum.باز;
                    
                //}
                //db.SaveChanges();

                /*var softs = await db.ExternalSoftwares.AsNoTracking().ToListAsync();
                var baseCenters = await db.BaseCenterCashes.AsNoTracking().ToListAsync();

                var mappings = new List<ExternalMapping>();

                foreach (var soft in softs)
                {
                    if (soft.Code == 1)
                    {
                        var dls = await S1db.DLs.AsNoTracking()
                            .Where(f => f.Type == 3)
                            .ToListAsync();

                        foreach (var center in baseCenters)
                        {
                            var dl = dls.FirstOrDefault(f => f.Code == center.Code);

                            if (dl != null)
                            {
                                mappings.Add(new ExternalMapping {
                                    EntityName = nameof(BaseCenterCash),
                                    EntityId = center.Id,
                                    MapType = ExternalMappingTypeEnum.مرکز_هزینه,
                                    MapTypeTxt = nameof(ExternalMappingTypeEnum.مرکز_هزینه),
                                    ExternalSoftwareId = soft.Id,
                                    ExEntityName = nameof(DL),
                                    ExEntityId = dl.DLId.ToString()
                                });
                            }
                        }
                    } else
                    {
                        var dls = await s2Db.DLs.AsNoTracking()
                            .Where(f => f.Type == 3)
                            .ToListAsync();

                        foreach (var center in baseCenters)
                        {
                            var code = center.Code.StartsWith('0') ? center.Code[1..] : center.Code;
                            var dl = dls.FirstOrDefault(f => f.Code == code);

                            if (dl != null)
                            {
                                mappings.Add(new ExternalMapping {
                                    EntityName = nameof(BaseCenterCash),
                                    EntityId = center.Id,
                                    MapType = ExternalMappingTypeEnum.مرکز_هزینه,
                                    MapTypeTxt = nameof(ExternalMappingTypeEnum.مرکز_هزینه),
                                    ExternalSoftwareId = soft.Id,
                                    ExEntityName = nameof(DL),
                                    ExEntityId = dl.DLId.ToString()
                                });
                            }
                        }
                    }
                }

                db.ExternalMappings.AddRange(mappings);
                await db.SaveChangesAsync();
*/


                // One-time ExternalMapping backfill (NOT EXECUTED).
                // Review the private BuildLegacySepidarMappingPlanAsync method below first.
                // Do not run database writes from GET /Home/Index.
                // After approval, expose a separate administrator-only, one-time POST
                // to review and apply the plan. No migration method is invoked here.

                ViewBag.SubTitle = "سامانه مدیریت مشتری";
                return View();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
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
            List<LegacyDuplicateTargetGroup> DuplicateTargetGroups)
        {
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
            var financialCustomerIds = await db.Customers.AsNoTracking()
                .Where(c => c.FinancialCustomerId.HasValue)
                .Select(c => new { c.Id, c.FinancialCustomerId })
                .ToDictionaryAsync(x => x.Id, x => x.FinancialCustomerId!.Value, ct);

            readStage = "CRM.ExternalMappings";
            var existing = await db.ExternalMappings.AsNoTracking()
                .Where(m => m.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                         || m.MapType == ExternalMappingTypeEnum.فاکتور)
                .ToListAsync(ct);

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
            }

            return new LegacyMappingPlan(alreadyMapped, notMatched, ambiguous, pending, issues, duplicateTargetGroups);
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
        private async Task<int> ApplyLegacySepidarMappingPlanAsync(CancellationToken ct)
        {
            var plan = await BuildLegacySepidarMappingPlanAsync(ct);
            if (plan.Pending.Count == 0) return 0;

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // Never overwrite an existing mapping or silently reuse a target record.
                var live = await db.ExternalMappings.AsNoTracking()
                    .Where(x => x.MapType == ExternalMappingTypeEnum.پیش_فاکتور
                             || x.MapType == ExternalMappingTypeEnum.فاکتور)
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

        public IActionResult sigR()
        {
            return View();
        }

        public IActionResult notif()
        {
            return View();
        }

        public IActionResult GetDate(int type)
        {
            switch (type)
            {
                case 1: //getCurrentWeek
                    return Ok(dts.GetCurrentWeek());
                case 2: //getCurrentMonth
                    return Ok(dts.GetCurrentMounth());
                case 3: //getCurrentYear
                    return Ok(dts.GetCurrentYear());
                case 4: //getToday_Date
                    return Ok(dts.GetDatePersian());
                case 5: //getToday_DateTime
                    return Ok(dts.GetDateTimePersianWithDayOfWeek());
                case 6: //getTowmarrowDate
                    return Ok(dts.GetTowmarrow());
            }

            return Ok();
        }


        public IActionResult GetTodayDateTime()
        {
            var dt = dts.GetDateTimePersianWithDayOfWeek();
            return Ok(dt);
        }

        public IActionResult GetTodayDate()
        {
            return Ok(dts.GetDatePersian());
        }

        public string GetTomorrowDate()
        {
            return dts.GetTowmarrow();
        }


        [HttpGet]
        public async Task<IActionResult> GetEntityLogSystem(string entityName, string entityId)
        {
            try
            {
                var isAdmin = User.IsAdmin();

                var logs = await logSystemService.GetEntityLogSystemAsync(entityName, entityId, isAdmin);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }


        /* ----------------------------------------------- System Warnings ----------------------------------------------- */

        [HttpGet]
        public async Task<IActionResult> GetAllSystemWarnings(bool isRefresh = false)
        {
            var warnings = await genService.GetAllWarnings(User, isRefresh);

            return Ok(warnings);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSystemWarning(Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var item = await db.SystemWarnings.Where(f => f.Id == id).FirstOrDefaultAsync();
                if (item == null)
                    return BadRequest("مقداری برای حذف یافت نشد");


                item.IsDeleted = true;
                item.ModifyDate = DateTime.Now;
                item.ModifyDateTxt = dts.GetDateTimePersian();
                item.UserModifyID = user.Id;
                item.UserModify = user.FullName;
                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        /* ----------------------------------------------- External Softwares ----------------------------------------------- */

        [HttpPost]
        public async Task<IActionResult> GetAllExternalSoftware([FromBody] ExternalSoftwareRequestDto request)
        {
            if (request.EntityIds.Count == 0 || request.EntityIds.Any(id => id == Guid.Empty))
                return BadRequest();

            var entityName = request.MapType switch {
                ExternalMappingTypeEnum.تنخواه => nameof(Cash),
                ExternalMappingTypeEnum.پیش_فاکتور => nameof(PreFactor),
                ExternalMappingTypeEnum.فاکتور => nameof(Factor),
                _ => null
            };

            if (entityName == null)
                return BadRequest();

            var entityIds = request.EntityIds.Distinct().ToList();

            await ValidateExternalMappingsAsync(entityIds, entityName, request.MapType);

            var softwares = await db.ExternalSoftwares
                .AsNoTracking()
                .OrderBy(x => x.Title)
                .Select(x => new {
                    x.Id,
                    x.Title,
                    IsDisabled = entityIds.All(entityId =>
                        db.ExternalMappings.Any(m =>
                            m.EntityName == entityName &&
                            m.EntityId == entityId &&
                            m.ExternalSoftwareId == x.Id &&
                            m.MapType == request.MapType))
                })
                .ToListAsync();

            return Ok(softwares);
        }

        private async Task ValidateExternalMappingsAsync(List<Guid> entityIds, string entityName, ExternalMappingTypeEnum mapType)
        {
            var mappings = await db.ExternalMappings
                .Where(x =>
                    entityIds.Contains(x.EntityId) &&
                    x.EntityName == entityName &&
                    x.MapType == mapType)
                .ToListAsync();

            if (mappings.Count == 0) return;

            var hasChanges = false;
            foreach (var mapping in mappings)
            {
                var exists = await CheckExternalEntityExistsAsync(mapping.ExternalSoftwareId, mapping.ExEntityId, mapping.ExEntityName);

                if (exists == false)
                {
                    db.ExternalMappings.Remove(mapping);
                    hasChanges = true;
                }
            }

            if (hasChanges)
                await db.SaveChangesAsync();
        }

        private async Task<bool?> CheckExternalEntityExistsAsync(Guid externalSoftwareId, string exEntityId, string exEntityName)
        {
            var service = GetSepidarService(externalSoftwareId);

            if (service == null)
                return null;

            return await service.EntityExistsAsync(exEntityName, exEntityId);
        }

        private static readonly Guid Sepidar1SoftwareId = Guid.Parse("2f3d8024-6c2c-491f-c921-08df26b00138");
        private static readonly Guid Sepidar2SoftwareId = Guid.Parse("d6c3e8a7-1435-47d5-0489-08df26c5e2ca");

        private SepidarService GetSepidarService(Guid softwareId)
        {
            if (softwareId == Sepidar1SoftwareId)
                return sepidar1Service;

            if (softwareId == Sepidar2SoftwareId)
                return sepidar2Service;

            return null;
        }
    }
}