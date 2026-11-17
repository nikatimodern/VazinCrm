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
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Sepidar.Parties.Entities;
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
using CRM.Core.Domain.Supplies.Entities;

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


                ViewBag.SubTitle = "سامانه مدیریت مشتری";
                return View();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
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

        private sealed record ExternalTransferRule(string EntityName, bool RemoveMissingMappings = true,
            string Roles = null, bool KnownDestinationsOnly = false, ExternalMappingTypeEnum? Prerequisite = null,
            string PrerequisiteMessage = null,
            Func<SepidarService, int, CancellationToken, Task<bool>> HasRelatedDocument = null);

        private static readonly IReadOnlyDictionary<ExternalMappingTypeEnum, ExternalTransferRule> ExternalTransferRules =
            new Dictionary<ExternalMappingTypeEnum, ExternalTransferRule> {
                [ExternalMappingTypeEnum.تنخواه] = new(nameof(Cash)),
                [ExternalMappingTypeEnum.پیش_فاکتور] = new(nameof(PreFactor)),
                [ExternalMappingTypeEnum.فاکتور] = new(nameof(Factor)),
                [ExternalMappingTypeEnum.فاکتور_خرید] = new(nameof(SupplyFactor), RemoveMissingMappings: false,
                    Roles: SupplyController.FinancialExportRoles, KnownDestinationsOnly: true),
                [ExternalMappingTypeEnum.رسید_انبار] = new(nameof(SupplyFactor), RemoveMissingMappings: false,
                    Roles: SupplyController.FinancialExportRoles, KnownDestinationsOnly: true,
                    Prerequisite: ExternalMappingTypeEnum.فاکتور_خرید,
                    PrerequisiteMessage: "برای ثبت رسید انبار، ابتدا فاکتور خرید را در نرم‌افزار مالی ثبت کنید.",
                    HasRelatedDocument: async (service, purchaseId, ct) =>
                        await service.FindReceiptForPurchaseAsync(purchaseId, ct) != null),
            };

        [HttpPost]
        public async Task<IActionResult> GetAllExternalSoftware([FromBody] ExternalSoftwareRequestDto request, CancellationToken ct)
        {
            if (request?.EntityIds == null || request.EntityIds.Count == 0 || request.EntityIds.Any(id => id == Guid.Empty)
                || !ExternalTransferRules.TryGetValue(request.MapType, out var rule))
                return BadRequest();
            if (rule.Roles != null && !rule.Roles.Split(',').Any(User.IsInRole))
                return Forbid();

            try
            {
                var entityIds = request.EntityIds.Distinct().ToList();
                var prerequisiteEntity = rule.Prerequisite is { } prerequisite ? ExternalTransferRules[prerequisite].EntityName : null;
                var mappings = await db.ExternalMappings.Where(x => entityIds.Contains(x.EntityId) &&
                    ((x.EntityName == rule.EntityName && x.MapType == request.MapType)
                    || (x.EntityName == prerequisiteEntity && x.MapType == rule.Prerequisite)))
                    .ToListAsync(ct);
                var existsByMapping = new Dictionary<Guid, bool?>();
                foreach (var mapping in mappings.Where(x => !x.HasPendingExternalId()))
                {
                    var service = GetSepidarService(mapping.ExternalSoftwareId);
                    existsByMapping[mapping.Id] = service == null
                        ? null : await service.EntityExistsAsync(mapping.ExEntityName, mapping.ExEntityId);
                }

                if (rule.RemoveMissingMappings)
                {
                    var missing = mappings.Where(x => x.MapType == request.MapType
                        && existsByMapping.GetValueOrDefault(x.Id) == false).ToArray();
                    if (missing.Length > 0)
                    {
                        db.ExternalMappings.RemoveRange(missing);
                        await db.SaveChangesAsync(ct);
                    }
                }

                var softwares = await db.ExternalSoftwares.AsNoTracking().OrderBy(x => x.Title)
                    .Select(x => new { x.Id, x.Title }).ToListAsync(ct);
                var options = new List<object>();
                var mappingsByDestination = mappings.ToLookup(x => x.ExternalSoftwareId);
                foreach (var software in softwares)
                {
                    var service = GetSepidarService(software.Id);
                    if (rule.KnownDestinationsOnly && service == null)
                        continue;

                    var destinationMappings = mappingsByDestination[software.Id].ToLookup(x => (x.EntityId, x.MapType));
                    var eligibleCount = 0;
                    var registeredCount = 0;
                    var missingCount = 0;
                    foreach (var entityId in entityIds)
                    {
                        ExternalMapping dependency = null;
                        if (rule.Prerequisite is { } requiredType)
                        {
                            dependency = destinationMappings[(entityId, requiredType)]
                                .FirstOrDefault(x => existsByMapping.GetValueOrDefault(x.Id) == true);
                            if (dependency == null)
                                continue;
                        }

                        eligibleCount++;
                        var completed = destinationMappings[(entityId, request.MapType)]
                            .Where(x => !x.HasPendingExternalId()).ToArray();
                        if (completed.Any(x => existsByMapping.GetValueOrDefault(x.Id) != false))
                            registeredCount++;
                        else if (completed.Length > 0 && !rule.RemoveMissingMappings)
                            missingCount++;
                        else if (rule.HasRelatedDocument != null && service != null && dependency != null
                            && !destinationMappings[(entityId, request.MapType)].Any(x => x.HasPendingExternalId())
                            && int.TryParse(dependency.ExEntityId, out var dependencyId)
                            && await rule.HasRelatedDocument(service, dependencyId, ct))
                            registeredCount++;
                    }

                    var prerequisiteMissing = rule.Prerequisite.HasValue && eligibleCount == 0;
                    options.Add(new {
                        software.Id,
                        software.Title,
                        IsDisabled = prerequisiteMissing || registeredCount + missingCount == eligibleCount,
                        IsAlreadySent = registeredCount == entityIds.Count,
                        PrerequisiteMissing = prerequisiteMissing,
                        PrerequisiteMessage = prerequisiteMissing ? rule.PrerequisiteMessage : null,
                        DisabledReason = prerequisiteMissing ? "پیش‌نیاز ثبت در مقصد موجود نیست"
                            : missingCount > 0 && registeredCount + missingCount == eligibleCount ? "سند مقصد پیدا نشد" : null,
                    });
                }
                return Ok(options);
            }
            catch (System.Data.Common.DbException)
            {
                return StatusCode(503, "بررسی وضعیت مقصدهای مالی انجام نشد.");
            }
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
